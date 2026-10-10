using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Banglish.Voice
{
    public class AudioRecorder : IDisposable
    {
        #region Win32 API Imports
        private const int WAVE_MAPPER = -1;
        private const int CALLBACK_FUNCTION = 0x00030000;
        private const int WIM_DATA = 0x3C0;
        private const int WIM_OPEN = 0x3BE;
        private const int WIM_CLOSE = 0x3BF;

        [StructLayout(LayoutKind.Sequential)]
        public struct WAVEFORMATEX
        {
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WAVEHDR
        {
            public IntPtr lpData;
            public uint dwBufferLength;
            public uint dwBytesRecorded;
            public IntPtr dwUser;
            public uint dwFlags;
            public uint dwLoops;
            public IntPtr lpNext;
            public IntPtr reserved;
        }

        public delegate void WaveInProc(IntPtr hwi, uint uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2);

        [DllImport("winmm.dll")]
        private static extern int waveInOpen(out IntPtr phwi, int uDeviceID, ref WAVEFORMATEX lpFormat, WaveInProc dwCallback, IntPtr dwInstance, int fdwOpen);

        [DllImport("winmm.dll")]
        private static extern int waveInPrepareHeader(IntPtr hwi, ref WAVEHDR pwh, int cbwh);

        [DllImport("winmm.dll")]
        private static extern int waveInUnprepareHeader(IntPtr hwi, ref WAVEHDR pwh, int cbwh);

        [DllImport("winmm.dll")]
        private static extern int waveInAddBuffer(IntPtr hwi, ref WAVEHDR pwh, int cbwh);

        [DllImport("winmm.dll")]
        private static extern int waveInStart(IntPtr hwi);

        [DllImport("winmm.dll")]
        private static extern int waveInStop(IntPtr hwi);

        [DllImport("winmm.dll")]
        private static extern int waveInReset(IntPtr hwi);

        [DllImport("winmm.dll")]
        private static extern int waveInClose(IntPtr hwi);
        #endregion

        private IntPtr _hWaveIn = IntPtr.Zero;
        private WaveInProc _callback;
        private MemoryStream _audioStream;
        private const int BUFFER_COUNT = 4;
        private const int BUFFER_SIZE = 3200; // 100ms at 16kHz 16-bit Mono (1600 samples * 2 bytes)
        private WAVEHDR[] _headers;
        private GCHandle[] _bufferHandles;
        private byte[][] _buffers;

        public bool IsRecording { get; private set; }
        public event Action<float> AudioLevelChanged;
        public event Action<byte[], float> AudioChunkReceived;

        public AudioRecorder()
        {
            _callback = WaveInCallback;

            // Allocate pinned buffers once for the entire application session
            _buffers = new byte[BUFFER_COUNT][];
            _bufferHandles = new GCHandle[BUFFER_COUNT];
            _headers = new WAVEHDR[BUFFER_COUNT];

            for (int i = 0; i < BUFFER_COUNT; i++)
            {
                _buffers[i] = new byte[BUFFER_SIZE];
                _bufferHandles[i] = GCHandle.Alloc(_buffers[i], GCHandleType.Pinned);
            }
        }

        public bool StartRecording()
        {
            if (IsRecording) return true;

            _audioStream = new MemoryStream();

            WAVEFORMATEX format = new WAVEFORMATEX
            {
                wFormatTag = 1, // WAVE_FORMAT_PCM
                nChannels = 1,  // Mono
                nSamplesPerSec = 16000,
                nAvgBytesPerSec = 32000,
                nBlockAlign = 2,
                wBitsPerSample = 16,
                cbSize = 0
            };

            int res = waveInOpen(out _hWaveIn, WAVE_MAPPER, ref format, _callback, IntPtr.Zero, CALLBACK_FUNCTION);
            if (res != 0)
            {
                _hWaveIn = IntPtr.Zero;
                return false;
            }

            for (int i = 0; i < BUFFER_COUNT; i++)
            {
                _headers[i] = new WAVEHDR
                {
                    lpData = _bufferHandles[i].AddrOfPinnedObject(),
                    dwBufferLength = BUFFER_SIZE,
                    dwBytesRecorded = 0,
                    dwUser = (IntPtr)i,
                    dwFlags = 0
                };

                waveInPrepareHeader(_hWaveIn, ref _headers[i], Marshal.SizeOf(typeof(WAVEHDR)));
                waveInAddBuffer(_hWaveIn, ref _headers[i], Marshal.SizeOf(typeof(WAVEHDR)));
            }

            res = waveInStart(_hWaveIn);
            if (res != 0)
            {
                StopRecording();
                return false;
            }

            IsRecording = true;
            return true;
        }

        public byte[] StopRecording()
        {
            if (!IsRecording && _audioStream == null) return new byte[0];

            IsRecording = false;

            if (_hWaveIn != IntPtr.Zero)
            {
                try
                {
                    waveInStop(_hWaveIn);
                    waveInReset(_hWaveIn);

                    // Wait briefly for native audio driver thread to finish in-flight callbacks
                    Thread.Sleep(40);

                    if (_headers != null)
                    {
                        for (int i = 0; i < BUFFER_COUNT; i++)
                        {
                            try
                            {
                                waveInUnprepareHeader(_hWaveIn, ref _headers[i], Marshal.SizeOf(typeof(WAVEHDR)));
                            }
                            catch {}
                        }
                    }

                    waveInClose(_hWaveIn);
                }
                catch {}
                _hWaveIn = IntPtr.Zero;
            }

            byte[] pcmData = _audioStream != null ? _audioStream.ToArray() : new byte[0];
            return pcmData;
        }

        private void WaveInCallback(IntPtr hwi, uint uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2)
        {
            try
            {
                if (uMsg == WIM_DATA && IsRecording)
                {
                    WAVEHDR header = (WAVEHDR)Marshal.PtrToStructure(dwParam1, typeof(WAVEHDR));
                    int bufIndex = header.dwUser.ToInt32();

                    if (header.dwBytesRecorded > 0 && _buffers != null && bufIndex >= 0 && bufIndex < BUFFER_COUNT)
                    {
                        byte[] data = _buffers[bufIndex];
                        int bytesRead = (int)header.dwBytesRecorded;

                        if (_audioStream != null)
                        {
                            lock (_audioStream)
                            {
                                _audioStream.Write(data, 0, bytesRead);
                            }
                        }

                        // Calculate peak level for audio visualizer (safe from Math.Abs overflow on -32768)
                        float peak = 0f;
                        for (int i = 0; i < bytesRead - 1; i += 2)
                        {
                            int sample = (short)(data[i] | (data[i + 1] << 8));
                            float abs = Math.Abs(sample) / 32768f;
                            if (abs > peak) peak = abs;
                        }

                        if (AudioLevelChanged != null)
                        {
                            AudioLevelChanged(peak);
                        }

                        if (AudioChunkReceived != null)
                        {
                            byte[] chunk = new byte[bytesRead];
                            Buffer.BlockCopy(data, 0, chunk, 0, bytesRead);
                            AudioChunkReceived(chunk, peak);
                        }

                        // Re-add buffer for continuous streaming
                        if (IsRecording && _hWaveIn != IntPtr.Zero)
                        {
                            waveInAddBuffer(_hWaveIn, ref _headers[bufIndex], Marshal.SizeOf(typeof(WAVEHDR)));
                        }
                    }
                }
            }
            catch {}
        }

        public void Dispose()
        {
            StopRecording();

            if (_bufferHandles != null)
            {
                for (int i = 0; i < _bufferHandles.Length; i++)
                {
                    try
                    {
                        if (_bufferHandles[i].IsAllocated)
                        {
                            _bufferHandles[i].Free();
                        }
                    }
                    catch {}
                }
            }
        }
    }
}
