using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Banglish.Core
{
    public class KeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;

        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        private HookProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        public bool IsEnabled { get; set; }
        public StringBuilder Buffer { get; private set; }

        public event Action<string, string> BufferChanged; // (rawBuffer, transliterated)
        public event Action<bool> ModeToggled;             // (isEnabled)
        public event Action<string> TextCommitted;         // (transliteratedText)

        public KeyboardHook()
        {
            IsEnabled = true;
            Buffer = new StringBuilder();
            _proc = HookCallback;
        }

        public void Start()
        {
            _hookID = SetHook(_proc);
        }

        public void Stop()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }

        private IntPtr SetHook(HookProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                System.Windows.Forms.Keys key = (System.Windows.Forms.Keys)vkCode;

                // F12 or Ctrl+Space to toggle Mode
                if (key == System.Windows.Forms.Keys.F12)
                {
                    IsEnabled = !IsEnabled;
                    Buffer.Clear();
                    if (ModeToggled != null) ModeToggled(IsEnabled);
                    return (IntPtr)1;
                }

                if (!IsEnabled)
                {
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                // Handle Backspace
                if (key == System.Windows.Forms.Keys.Back)
                {
                    if (Buffer.Length > 0)
                    {
                        Buffer.Remove(Buffer.Length - 1, 1);
                        TriggerBufferUpdate();
                        return (IntPtr)1;
                    }
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                // Handle Escape (Cancel composition)
                if (key == System.Windows.Forms.Keys.Escape)
                {
                    if (Buffer.Length > 0)
                    {
                        Buffer.Clear();
                        TriggerBufferUpdate();
                        return (IntPtr)1;
                    }
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                // Handle Space / Enter (Commit transliterated word into active app)
                if (key == System.Windows.Forms.Keys.Space || key == System.Windows.Forms.Keys.Return)
                {
                    if (Buffer.Length > 0)
                    {
                        string raw = Buffer.ToString();
                        string bangla = BanglishEngine.Shared.Transliterate(raw);
                        Buffer.Clear();
                        TriggerBufferUpdate();

                        string suffix = (key == System.Windows.Forms.Keys.Space) ? " " : "\n";
                        SendUnicodeStringAsync(bangla + suffix);

                        if (TextCommitted != null) TextCommitted(bangla);
                        return (IntPtr)1;
                    }
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                // Convert Key to Character
                char ch = GetCharFromKey(key);
                if (ch != '\0')
                {
                    Buffer.Append(ch);
                    TriggerBufferUpdate();
                    return (IntPtr)1;
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private char GetCharFromKey(System.Windows.Forms.Keys key)
        {
            bool shift = (System.Windows.Forms.Control.ModifierKeys & System.Windows.Forms.Keys.Shift) != 0;
            bool capsLock = System.Windows.Forms.Control.IsKeyLocked(System.Windows.Forms.Keys.CapsLock);

            if (key >= System.Windows.Forms.Keys.A && key <= System.Windows.Forms.Keys.Z)
            {
                bool upper = shift ^ capsLock;
                char baseChar = (char)('a' + (key - System.Windows.Forms.Keys.A));
                return upper ? char.ToUpperInvariant(baseChar) : baseChar;
            }

            if (key >= System.Windows.Forms.Keys.D0 && key <= System.Windows.Forms.Keys.D9)
            {
                if (!shift) return (char)('0' + (key - System.Windows.Forms.Keys.D0));
                switch (key)
                {
                    case System.Windows.Forms.Keys.D6: return '^';
                    case System.Windows.Forms.Keys.D4: return '$';
                }
            }

            switch (key)
            {
                case System.Windows.Forms.Keys.OemPeriod: return shift ? '>' : '.';
                case System.Windows.Forms.Keys.Oemcomma: return shift ? '<' : ',';
                case System.Windows.Forms.Keys.Oem1: return shift ? ':' : ';';
                case System.Windows.Forms.Keys.Oemtilde: return shift ? '~' : '`';
            }

            return '\0';
        }

        private void TriggerBufferUpdate()
        {
            string raw = Buffer.ToString();
            string bangla = BanglishEngine.Shared.Transliterate(raw);
            if (BufferChanged != null) BufferChanged(raw, bangla);
        }

        public static void SendUnicodeStringAsync(string str)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(15); // Pause 15ms to allow keyboard hook callback thread to exit
                SendUnicodeString(str);
            });
        }

        public static void SendUnicodeString(string str)
        {
            foreach (char c in str)
            {
                INPUT[] inputs = new INPUT[2];
                inputs[0] = new INPUT
                {
                    type = 1, // INPUT_KEYBOARD
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = 0,
                            wScan = (ushort)c,
                            dwFlags = 0x0004, // KEYEVENTF_UNICODE
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                };
                inputs[1] = new INPUT
                {
                    type = 1,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = 0,
                            wScan = (ushort)c,
                            dwFlags = 0x0004 | 0x0002, // KEYEVENTF_UNICODE | KEYEVENTF_KEYUP
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                };
                SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
            }
        }

        public void Dispose()
        {
            Stop();
        }

        #region Win32 API Imports
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        #endregion
    }
}
