using System;
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
        private System.Windows.Forms.Timer _dismissTimer;

        public bool IsRecording
        {
            get { return _recorder != null && _recorder.IsRecording; }
        }

        public VoiceTypingManager()
        {
            _recorder = new AudioRecorder();
            _indicatorForm = new VoiceIndicatorForm();

            _recorder.AudioLevelChanged += (level) =>
            {
                _indicatorForm.UpdateAudioLevel(level);
            };

            _indicatorForm.OnClickStop += () =>
            {
                if (IsRecording)
                {
                    StopAndTranscribe();
                }
            };
        }

        public void ToggleVoiceTyping()
        {
            if (IsRecording)
            {
                StopAndTranscribe();
            }
            else
            {
                StartRecording();
            }
        }

        public void StartRecording()
        {
            if (IsRecording) return;

            if (_dismissTimer != null)
            {
                _dismissTimer.Stop();
                _dismissTimer.Dispose();
                _dismissTimer = null;
            }

            bool ok = _recorder.StartRecording();
            if (ok)
            {
                try { SystemSounds.Asterisk.Play(); } catch {}
                _indicatorForm.SetState(VoiceState.Listening, "কথা বলুন... (Ctrl+F12)");
            }
            else
            {
                _indicatorForm.SetState(VoiceState.Error, "মাইক্রোফোন চালু করা যায়নি");
                ScheduleDismiss(2500);
            }
        }

        public void StopAndTranscribe()
        {
            if (!IsRecording) return;

            byte[] pcmData = _recorder.StopRecording();

            if (pcmData == null || pcmData.Length < 3200)
            {
                _indicatorForm.SetState(VoiceState.Error, "কোনো কথা রেকর্ড হয়নি");
                ScheduleDismiss(1500);
                return;
            }

            _indicatorForm.SetState(VoiceState.Processing, "প্রসেসিং হচ্ছে...");

            GoogleSpeechClient.RecognizeAsync(pcmData, (transcript, error) =>
            {
                if (!string.IsNullOrEmpty(transcript))
                {
                    _indicatorForm.SetState(VoiceState.Success, transcript);

                    // Insert text directly into the active typing cursor
                    string textToInsert = transcript + " ";
                    KeyboardHook.ReplaceComposedText(0, textToInsert);

                    ScheduleDismiss(1200);
                }
                else
                {
                    string msg = !string.IsNullOrEmpty(error) ? error : "কোনো কথা শনাক্ত হয়নি";
                    _indicatorForm.SetState(VoiceState.Error, msg);
                    ScheduleDismiss(2200);
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
