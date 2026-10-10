using System;
using System.IO;
using System.Media;
using System.Threading;
using System.Windows.Forms;
using Banglish.Core;

namespace Banglish.Voice
{
    public class VoiceTypingManager : IDisposable
    {
        private static VoiceTypingManager _instance;
        public static VoiceTypingManager Shared
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new VoiceTypingManager();
                }
                return _instance;
            }
        }

        private readonly AudioRecorder _recorder;
        private readonly VoiceIndicatorForm _indicatorForm;

        private MemoryStream _currentPhraseStream;
        private bool _isSpeaking = false;
        private int _silenceDurationMs = 0;
        private int _totalSilenceDurationMs = 0;
        private const float SILENCE_THRESHOLD = 0.040f;

        private System.Windows.Forms.Timer _dismissTimer;

        public bool IsRecording
        {
            get { return _recorder != null && _recorder.IsRecording; }
        }

        public VoiceTypingManager()
        {
            _recorder = new AudioRecorder();
            _indicatorForm = new VoiceIndicatorForm();
            _currentPhraseStream = new MemoryStream();

            _recorder.AudioChunkReceived += OnAudioChunk;

            _indicatorForm.OnClickStop += () =>
            {
                if (IsRecording)
                {
                    StopSession();
                }
            };
        }

        public void ToggleVoiceTyping()
        {
            if (IsRecording)
            {
                StopSession();
            }
            else
            {
                StartSession();
            }
        }

        public void StartSession()
        {
            if (IsRecording) return;

            if (_dismissTimer != null)
            {
                _dismissTimer.Stop();
                _dismissTimer.Dispose();
                _dismissTimer = null;
            }

            lock (_currentPhraseStream)
            {
                _currentPhraseStream = new MemoryStream();
            }
            lock (_seqLock)
            {
                _chunkSeqCounter = 0;
                _nextTypeSeq = 0;
                _pendingTranscripts.Clear();
            }
            _isSpeaking = false;
            _silenceDurationMs = 0;
            _totalSilenceDurationMs = 0;

            bool ok = _recorder.StartRecording();
            if (ok)
            {
                try { SystemSounds.Asterisk.Play(); } catch {}
                _indicatorForm.SetState(VoiceState.Listening, "কথা বলুন... (লাইভ টাইপিং)");
            }
            else
            {
                _indicatorForm.SetState(VoiceState.Error, "মাইক্রোফোন চালু করা যায়নি");
                ScheduleDismiss(2500);
            }
        }

        public void StopSession()
        {
            if (!IsRecording) return;

            _recorder.StopRecording();

            // Dispatch any final remaining speech chunk
            byte[] finalChunk = null;
            lock (_currentPhraseStream)
            {
                if (_currentPhraseStream.Length >= 4800) // At least 150ms of audio
                {
                    finalChunk = _currentPhraseStream.ToArray();
                }
                _currentPhraseStream = new MemoryStream();
            }

            if (finalChunk != null)
            {
                _indicatorForm.SetState(VoiceState.Processing, "শেষ অংশ প্রসেসিং হচ্ছে...");
                DispatchPhrase(finalChunk, true);
            }
            else
            {
                _indicatorForm.SetState(VoiceState.Success, "ভয়েস টাইপিং সমাপ্ত");
                ScheduleDismiss(1000);
            }
        }

        private long _chunkSeqCounter = 0;
        private long _nextTypeSeq = 0;
        private readonly System.Collections.Generic.SortedDictionary<long, string> _pendingTranscripts = new System.Collections.Generic.SortedDictionary<long, string>();
        private readonly object _seqLock = new object();

        private void OnAudioChunk(byte[] chunk, float peak)
        {
            if (!IsRecording) return;

            _indicatorForm.UpdateAudioLevel(peak);

            byte[] chunkToDispatch = null;

            lock (_currentPhraseStream)
            {
                if (peak >= SILENCE_THRESHOLD)
                {
                    _isSpeaking = true;
                    _silenceDurationMs = 0;
                    _totalSilenceDurationMs = 0;
                    _currentPhraseStream.Write(chunk, 0, chunk.Length);
                }
                else
                {
                    _silenceDurationMs += 100;
                    _totalSilenceDurationMs += 100;

                    // Preserve natural speech tail (up to 150ms)
                    if (_isSpeaking || _silenceDurationMs <= 150)
                    {
                        _currentPhraseStream.Write(chunk, 0, chunk.Length);
                    }
                }

                // Condition 1: Natural quick pause after speech (>= 200ms pause with at least 0.5s audio)
                bool pauseDetected = _isSpeaking && _silenceDurationMs >= 200 && _currentPhraseStream.Length >= 16000;

                // Condition 2: Continuous speaking slice threshold (1.2 seconds = 38400 bytes)
                bool maxChunkReached = _currentPhraseStream.Length >= 38400;

                if (pauseDetected || maxChunkReached)
                {
                    chunkToDispatch = _currentPhraseStream.ToArray();
                    _currentPhraseStream = new MemoryStream();
                    _isSpeaking = false;
                    _silenceDurationMs = 0;
                }
            }

            if (chunkToDispatch != null)
            {
                DispatchPhrase(chunkToDispatch, false);
            }

            // Auto-stop if silent for more than 10 seconds
            if (_totalSilenceDurationMs >= 10000)
            {
                StopSession();
            }
        }

        private void DispatchPhrase(byte[] pcmData, bool isFinal)
        {
            long seq;
            lock (_seqLock)
            {
                seq = ++_chunkSeqCounter;
            }

            GoogleSpeechClient.RecognizeAsync(pcmData, (transcript, error) =>
            {
                lock (_seqLock)
                {
                    _pendingTranscripts[seq] = transcript ?? "";

                    while (_pendingTranscripts.ContainsKey(_nextTypeSeq + 1))
                    {
                        _nextTypeSeq++;
                        string text = _pendingTranscripts[_nextTypeSeq];
                        _pendingTranscripts.Remove(_nextTypeSeq);

                        if (!string.IsNullOrEmpty(text))
                        {
                            // Live injection into active typing window as words are spoken!
                            string textToInsert = text + " ";
                            KeyboardHook.ReplaceComposedText(0, textToInsert);

                            if (IsRecording)
                            {
                                _indicatorForm.SetState(VoiceState.Listening, "টাইপ করা হয়েছে: " + text);
                            }
                            else if (isFinal)
                            {
                                _indicatorForm.SetState(VoiceState.Success, text);
                                ScheduleDismiss(1000);
                            }
                        }
                    }
                }

                if (isFinal && !IsRecording)
                {
                    _indicatorForm.SetState(VoiceState.Success, "ভয়েস টাইপিং সমাপ্ত");
                    ScheduleDismiss(1000);
                }
            });
        }

        private void ScheduleDismiss(int delayMs)
        {
            if (_indicatorForm.InvokeRequired)
            {
                _indicatorForm.BeginInvoke(new Action(() => ScheduleDismiss(delayMs)));
                return;
            }

            if (_dismissTimer != null)
            {
                _dismissTimer.Stop();
                _dismissTimer.Dispose();
            }

            _dismissTimer = new System.Windows.Forms.Timer();
            _dismissTimer.Interval = delayMs;
            _dismissTimer.Tick += (s, e) =>
            {
                _dismissTimer.Stop();
                _indicatorForm.SetState(VoiceState.Idle);
            };
            _dismissTimer.Start();
        }

        public void Dispose()
        {
            if (_recorder != null) _recorder.Dispose();
            if (_indicatorForm != null && !_indicatorForm.IsDisposed) _indicatorForm.Dispose();
            if (_dismissTimer != null) _dismissTimer.Dispose();
        }
    }
}
