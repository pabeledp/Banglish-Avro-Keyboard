using System;
using System.Collections.Generic;
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
        private List<string> _currentCandidates = new List<string>();
        private int _selectedCandidateIndex = 0;

        public event Action<string, List<string>, int> CandidatesChanged;
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
            IntPtr hMod = IntPtr.Zero;
            try
            {
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    hMod = GetModuleHandle(curModule.ModuleName);
                }
            }
            catch {}

            if (hMod == IntPtr.Zero)
            {
                hMod = GetModuleHandle(null);
            }

            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, hMod, 0);
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

        public void CommitSelectedCandidate(string candidate)
        {
            int prevCount = _lastComposed.Length;
            ReplaceComposedText(prevCount, candidate);
            Buffer.Clear();
            _lastComposed = "";
            _currentCandidates.Clear();
            _selectedCandidateIndex = 0;
            TriggerCandidatesUpdate();
        }

        public void ToggleMode()
        {
            IsEnabled = !IsEnabled;
            Buffer.Clear();
            _lastComposed = "";
            _currentCandidates.Clear();
            _selectedCandidateIndex = 0;
            TriggerCandidatesUpdate();
            if (ModeToggled != null) ModeToggled(IsEnabled);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                KBDLLHOOKSTRUCT hookStruct = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));

                // If key is injected by our own SendInput, pass it through immediately
                if ((hookStruct.flags & 0x10) != 0 || hookStruct.dwExtraInfo == (UIntPtr)0xBA991158 || (long)hookStruct.dwExtraInfo == 0xBA991158L)
                {
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                System.Windows.Forms.Keys key = (System.Windows.Forms.Keys)hookStruct.vkCode;

                // Toggle Key: F12
                if (key == System.Windows.Forms.Keys.F12)
                {
                    ToggleMode();
                    return (IntPtr)1;
                }

                if (!IsEnabled)
                {
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                // If candidate window is active and user presses number key 1-9
                if (Buffer.Length > 0 && key >= System.Windows.Forms.Keys.D1 && key <= System.Windows.Forms.Keys.D9)
                {
                    bool shift = (System.Windows.Forms.Control.ModifierKeys & System.Windows.Forms.Keys.Shift) != 0;
                    if (!shift)
                    {
                        int choiceIdx = (key - System.Windows.Forms.Keys.D1);
                        if (choiceIdx < _currentCandidates.Count)
                        {
                            CommitSelectedCandidate(_currentCandidates[choiceIdx]);
                            return (IntPtr)1;
                        }
                    }
                }

                // Navigate candidates using Down arrow or Tab
                if (Buffer.Length > 0 && (key == System.Windows.Forms.Keys.Down || key == System.Windows.Forms.Keys.Tab))
                {
                    if (_currentCandidates.Count > 1)
                    {
                        _selectedCandidateIndex = (_selectedCandidateIndex + 1) % _currentCandidates.Count;
                        string chosen = _currentCandidates[_selectedCandidateIndex];
                        int prevCount = _lastComposed.Length;
                        ReplaceComposedText(prevCount, chosen);
                        _lastComposed = chosen;
                        TriggerCandidatesUpdate();
                        return (IntPtr)1;
                    }
                }

                // Navigate candidates using Up arrow
                if (Buffer.Length > 0 && key == System.Windows.Forms.Keys.Up)
                {
                    if (_currentCandidates.Count > 1)
                    {
                        _selectedCandidateIndex = (_selectedCandidateIndex - 1 + _currentCandidates.Count) % _currentCandidates.Count;
                        string chosen = _currentCandidates[_selectedCandidateIndex];
                        int prevCount = _lastComposed.Length;
                        ReplaceComposedText(prevCount, chosen);
                        _lastComposed = chosen;
                        TriggerCandidatesUpdate();
                        return (IntPtr)1;
                    }
                }

                // If user clicks Space: commit current word and append space reliably
                if (key == System.Windows.Forms.Keys.Space)
                {
                    if (Buffer.Length > 0)
                    {
                        Buffer.Clear();
                        _lastComposed = "";
                        _currentCandidates.Clear();
                        _selectedCandidateIndex = 0;
                        TriggerCandidatesUpdate();

                        // Dispatch space via SendInput sequentially in same stream
                        SendSingleChar(' ');
                        return (IntPtr)1;
                    }
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                // If user clicks Enter/Return: finalize word and allow Enter to pass through
                if (key == System.Windows.Forms.Keys.Return)
                {
                    if (Buffer.Length > 0)
                    {
                        Buffer.Clear();
                        _lastComposed = "";
                        _currentCandidates.Clear();
                        _selectedCandidateIndex = 0;
                        TriggerCandidatesUpdate();
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
                            _currentCandidates = BanglishDictionary.Shared.GetCandidates(Buffer.ToString());
                            _selectedCandidateIndex = 0;
                            string newBangla = _currentCandidates.Count > 0 ? _currentCandidates[0] : "";

                            ReplaceComposedText(prevCount, newBangla);
                            _lastComposed = newBangla;
                        }
                        else
                        {
                            ReplaceComposedText(prevCount, "");
                            _lastComposed = "";
                            _currentCandidates.Clear();
                            _selectedCandidateIndex = 0;
                        }

                        TriggerCandidatesUpdate();
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
                        _currentCandidates.Clear();
                        _selectedCandidateIndex = 0;
                        TriggerCandidatesUpdate();
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

                    _currentCandidates = BanglishDictionary.Shared.GetCandidates(Buffer.ToString());
                    _selectedCandidateIndex = 0;
                    string newBangla = _currentCandidates.Count > 0 ? _currentCandidates[0] : "";

                    ReplaceComposedText(prevCount, newBangla);
                    _lastComposed = newBangla;

                    TriggerCandidatesUpdate();
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

        private void TriggerCandidatesUpdate()
        {
            string raw = Buffer.ToString();
            if (CandidatesChanged != null)
            {
                CandidatesChanged(raw, _currentCandidates, _selectedCandidateIndex);
            }
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
                            wVk = 0x08, // VK_BACK
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

        public static void SendSingleChar(char c)
        {
            INPUT[] inputs = new INPUT[2];
            inputs[0] = new INPUT
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
                        dwExtraInfo = MAGIC_COOKIE
                    }
                }
            };
            uint sent = SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
            if (sent == 0)
            {
                keybd_event(0, (byte)c, 0x0004, MAGIC_COOKIE);
                keybd_event(0, (byte)c, 0x0004 | 0x0002, MAGIC_COOKIE);
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
