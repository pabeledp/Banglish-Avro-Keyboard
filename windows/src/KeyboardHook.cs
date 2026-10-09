using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Banglish.Core
{
    public class KeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;

        public static readonly IntPtr MAGIC_COOKIE = new IntPtr(0xBA991158);

        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        private HookProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        public bool IsEnabled { get; set; }
        public StringBuilder Buffer { get; private set; }
        private string _lastComposed = "";

        public event Action<string, string> BufferChanged;
        public event Action<bool> ModeToggled;

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

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                KBDLLHOOKSTRUCT hookStruct = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));

                // If key is injected by our own SendInput, pass it through immediately!
                if (hookStruct.dwExtraInfo == (UIntPtr)0xBA991158 || (hookStruct.flags & 0x10) != 0)
                {
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                System.Windows.Forms.Keys key = (System.Windows.Forms.Keys)hookStruct.vkCode;

                // Toggle Key: F12
                if (key == System.Windows.Forms.Keys.F12)
                {
                    IsEnabled = !IsEnabled;
                    Buffer.Clear();
                    _lastComposed = "";
                    TriggerBufferUpdate();
                    if (ModeToggled != null) ModeToggled(IsEnabled);
                    return (IntPtr)1;
                }

                if (!IsEnabled)
                {
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                // If user clicks Space or Enter: finalize the word
                if (key == System.Windows.Forms.Keys.Space || key == System.Windows.Forms.Keys.Return)
                {
                    if (Buffer.Length > 0)
                    {
                        Buffer.Clear();
                        _lastComposed = "";
                        TriggerBufferUpdate();
                    }
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                // Handle Backspace: remove last character and re-transliterate live
                if (key == System.Windows.Forms.Keys.Back)
                {
                    if (Buffer.Length > 0)
                    {
                        int prevCount = _lastComposed.Length;
                        Buffer.Remove(Buffer.Length - 1, 1);

                        if (Buffer.Length > 0)
                        {
                            string newBangla = BanglishEngine.Shared.Transliterate(Buffer.ToString());
                            ReplaceComposedText(prevCount, newBangla);
                            _lastComposed = newBangla;
                        }
                        else
                        {
                            ReplaceComposedText(prevCount, "");
                            _lastComposed = "";
                        }

                        TriggerBufferUpdate();
                        return (IntPtr)1;
                    }
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                // Handle Escape: clear active word
                if (key == System.Windows.Forms.Keys.Escape)
                {
                    if (Buffer.Length > 0)
                    {
                        ReplaceComposedText(_lastComposed.Length, "");
                        Buffer.Clear();
                        _lastComposed = "";
                        TriggerBufferUpdate();
                        return (IntPtr)1;
                    }
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                // Convert Key to Character
                char ch = GetCharFromKey(key);
                if (ch != '\0')
                {
                    int prevCount = _lastComposed.Length;
                    Buffer.Append(ch);
                    string newBangla = BanglishEngine.Shared.Transliterate(Buffer.ToString());

                    ReplaceComposedText(prevCount, newBangla);
                    _lastComposed = newBangla;

                    TriggerBufferUpdate();
                    return (IntPtr)1; // Swallowed, because Bangla was typed live into the active app!
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
            string bangla = _lastComposed;
            if (BufferChanged != null) BufferChanged(raw, bangla);
        }

        public static void ReplaceComposedText(int backspaceCount, string newText)
        {
            int totalInputs = (backspaceCount * 2) + (newText.Length * 2);
            if (totalInputs == 0) return;

            INPUT[] inputs = new INPUT[totalInputs];
            int idx = 0;

            // 1. Send Backspaces
            for (int i = 0; i < backspaceCount; i++)
            {
                inputs[idx++] = new INPUT
                {
                    type = 1,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = 0x08, // VK_BACK
                            wScan = 0,
                            dwFlags = 0,
                            time = 0,
                            dwExtraInfo = MAGIC_COOKIE
                        }
                    }
                };
                inputs[idx++] = new INPUT
                {
                    type = 1,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = 0x08,
                            wScan = 0,
                            dwFlags = 0x0002, // KEYEVENTF_KEYUP
                            time = 0,
                            dwExtraInfo = MAGIC_COOKIE
                        }
                    }
                };
            }

            // 2. Send New Transliterated Unicode Characters
            for (int i = 0; i < newText.Length; i++)
            {
                char c = newText[i];
                inputs[idx++] = new INPUT
                {
                    type = 1,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = 0,
                            wScan = (ushort)c,
                            dwFlags = 0x0004, // KEYEVENTF_UNICODE
                            time = 0,
                            dwExtraInfo = MAGIC_COOKIE
                        }
                    }
                };
                inputs[idx++] = new INPUT
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
                            dwExtraInfo = MAGIC_COOKIE
                        }
                    }
                };
            }

            uint sent = SendInput((uint)totalInputs, inputs, Marshal.SizeOf(typeof(INPUT)));
            if (sent == 0)
            {
                // Fallback via keybd_event if SendInput is blocked
                for (int i = 0; i < backspaceCount; i++)
                {
                    keybd_event(0x08, 0, 0, MAGIC_COOKIE);
                    keybd_event(0x08, 0, 2, MAGIC_COOKIE);
                }
                foreach (char c in newText)
                {
                    keybd_event(0, (byte)c, 0x0004, MAGIC_COOKIE);
                    keybd_event(0, (byte)c, 0x0004 | 0x0002, MAGIC_COOKIE);
                }
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

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        #endregion
    }
}
