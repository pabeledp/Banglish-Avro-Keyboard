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
        public event Action VoiceHotkeyPressed;

        private long _lastToggleTick = 0;

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
            ReplaceComposedText(_lastComposed, candidate);
            Buffer.Clear();
            _lastComposed = "";
            _currentCandidates.Clear();
            _selectedCandidateIndex = 0;
            TriggerCandidatesUpdate();
        }

        public void CommitComposition()
        {
            if (Buffer.Length > 0)
            {
                Buffer.Clear();
                _lastComposed = "";
                _currentCandidates.Clear();
                _selectedCandidateIndex = 0;
                TriggerCandidatesUpdate();
            }
        }

        public void ToggleMode()
        {
            long now = Environment.TickCount;
            if (now - _lastToggleTick < 250)
            {
                // Prevent duplicate rapid triggers (e.g. from simultaneous hook & hotkey)
                return;
            }
            _lastToggleTick = now;

            // Finalize any in-progress composed word so the typed Bengali word stays cleanly in the editor
            CommitComposition();

            IsEnabled = !IsEnabled;
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

                // Check Modifier Keys
                bool ctrlPressed = (GetKeyState(0x11 /*VK_CONTROL*/) & 0x8000) != 0;
                bool altPressed = (GetKeyState(0x12 /*VK_MENU*/) & 0x8000) != 0;
                bool shiftPressed = (GetKeyState(0x10 /*VK_SHIFT*/) & 0x8000) != 0;

                // 1. Function Keys (Section 1) - Hotkeys
                if (key == System.Windows.Forms.Keys.F12)
                {
                    if (ctrlPressed && !altPressed && !shiftPressed)
                    {
                        // Ctrl + F12: Voice Typing Toggle
                        if (VoiceHotkeyPressed != null)
                        {
                            VoiceHotkeyPressed();
                        }
                        return (IntPtr)1;
                    }
                    else if (!ctrlPressed && !altPressed)
                    {
                        // Plain F12 or Shift + F12: Language Mode Toggle (even in the middle of words!)
                        ToggleMode();
                        return (IntPtr)1;
                    }
                }

                // Universal Alternate Language Toggle: Ctrl + Space
                if (ctrlPressed && !altPressed && !shiftPressed && key == System.Windows.Forms.Keys.Space)
                {
                    ToggleMode();
                    return (IntPtr)1;
                }

                if (!IsEnabled)
                {
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                // 2. Standalone Modifier Keys (Pass through so holding Shift/Ctrl/Alt/Win doesn't clear composition)
                bool isStandaloneModifier = key == System.Windows.Forms.Keys.ShiftKey ||
                                            key == System.Windows.Forms.Keys.LShiftKey ||
                                            key == System.Windows.Forms.Keys.RShiftKey ||
                                            key == System.Windows.Forms.Keys.ControlKey ||
                                            key == System.Windows.Forms.Keys.LControlKey ||
                                            key == System.Windows.Forms.Keys.RControlKey ||
                                            key == System.Windows.Forms.Keys.Menu ||
                                            key == System.Windows.Forms.Keys.LMenu ||
                                            key == System.Windows.Forms.Keys.RMenu ||
                                            key == System.Windows.Forms.Keys.LWin ||
                                            key == System.Windows.Forms.Keys.RWin ||
                                            key == System.Windows.Forms.Keys.Capital ||
                                            key == System.Windows.Forms.Keys.NumLock;
                if (isStandaloneModifier)
                {
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                // 3. System Shortcut Combinations (Ctrl+C, Ctrl+V, Alt+Tab, Win+D, etc.)
                bool winPressed = (GetKeyState(0x5B /*VK_LWIN*/) & 0x8000) != 0 || (GetKeyState(0x5C /*VK_RWIN*/) & 0x8000) != 0;
                if (ctrlPressed || altPressed || winPressed)
                {
                    CommitComposition();
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                // 4. Candidate Selection (Keys 1-9 on BOTH Top Number Row AND Numeric Keypad)
                bool isPureDigits = true;
                for (int bi = 0; bi < Buffer.Length; bi++)
                {
                    if (!char.IsDigit(Buffer[bi])) { isPureDigits = false; break; }
                }

                bool isNumRowChoice = key >= System.Windows.Forms.Keys.D1 && key <= System.Windows.Forms.Keys.D9;
                bool isNumPadChoice = key >= System.Windows.Forms.Keys.NumPad1 && key <= System.Windows.Forms.Keys.NumPad9;

                if (Buffer.Length > 0 && !isPureDigits && _currentCandidates.Count > 1 && (isNumRowChoice || isNumPadChoice))
                {
                    if (!shiftPressed)
                    {
                        int choiceIdx = isNumPadChoice 
                            ? (key - System.Windows.Forms.Keys.NumPad1) 
                            : (key - System.Windows.Forms.Keys.D1);
                        if (choiceIdx < _currentCandidates.Count)
                        {
                            CommitSelectedCandidate(_currentCandidates[choiceIdx]);
                            return (IntPtr)1;
                        }
                    }
                }

                // 5. Candidate Navigation (Down Arrow / Tab: next candidate; Up Arrow: previous candidate)
                if (Buffer.Length > 0 && (key == System.Windows.Forms.Keys.Down || key == System.Windows.Forms.Keys.Tab))
                {
                    if (_currentCandidates.Count > 1)
                    {
                        _selectedCandidateIndex = (_selectedCandidateIndex + 1) % _currentCandidates.Count;
                        string chosen = _currentCandidates[_selectedCandidateIndex];
                        ReplaceComposedText(_lastComposed, chosen);
                        _lastComposed = chosen;
                        TriggerCandidatesUpdate();
                        return (IntPtr)1;
                    }
                }

                if (Buffer.Length > 0 && key == System.Windows.Forms.Keys.Up)
                {
                    if (_currentCandidates.Count > 1)
                    {
                        _selectedCandidateIndex = (_selectedCandidateIndex - 1 + _currentCandidates.Count) % _currentCandidates.Count;
                        string chosen = _currentCandidates[_selectedCandidateIndex];
                        ReplaceComposedText(_lastComposed, chosen);
                        _lastComposed = chosen;
                        TriggerCandidatesUpdate();
                        return (IntPtr)1;
                    }
                }

                // 6. Navigation Keys (Left, Right, Home, End, PageUp, PageDown, Delete, Insert) & Non-candidate Arrows/Tab
                bool isNavKey = key == System.Windows.Forms.Keys.Left ||
                                key == System.Windows.Forms.Keys.Right ||
                                key == System.Windows.Forms.Keys.Up ||
                                key == System.Windows.Forms.Keys.Down ||
                                key == System.Windows.Forms.Keys.Home ||
                                key == System.Windows.Forms.Keys.End ||
                                key == System.Windows.Forms.Keys.PageUp ||
                                key == System.Windows.Forms.Keys.PageDown ||
                                key == System.Windows.Forms.Keys.Insert ||
                                key == System.Windows.Forms.Keys.Delete ||
                                key == System.Windows.Forms.Keys.Tab;
                if (isNavKey)
                {
                    CommitComposition();
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                // 7. Spacebar: commit composed word cleanly and dispatch single space
                if (key == System.Windows.Forms.Keys.Space)
                {
                    if (Buffer.Length > 0)
                    {
                        CommitComposition();
                        SendSingleChar(' ');
                        return (IntPtr)1;
                    }
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                // 8. Enter / Return (Both Main Enter and Keypad Enter)
                if (key == System.Windows.Forms.Keys.Return)
                {
                    CommitComposition();
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                // 9. Backspace: remove last character and re-transliterate live
                if (key == System.Windows.Forms.Keys.Back)
                {
                    if (Buffer.Length > 0)
                    {
                        Buffer.Remove(Buffer.Length - 1, 1);

                        if (Buffer.Length > 0)
                        {
                            _currentCandidates = BanglishDictionary.Shared.GetCandidates(Buffer.ToString());
                            _selectedCandidateIndex = 0;
                            string newBangla = _currentCandidates.Count > 0 ? _currentCandidates[0] : BanglishEngine.Shared.Transliterate(Buffer.ToString());

                            ReplaceComposedText(_lastComposed, newBangla);
                            _lastComposed = newBangla;
                        }
                        else
                        {
                            ReplaceComposedText(_lastComposed, "");
                            _lastComposed = "";
                            _currentCandidates.Clear();
                            _selectedCandidateIndex = 0;
                        }

                        TriggerCandidatesUpdate();
                        return (IntPtr)1;
                    }
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                // 10. Escape: discard composition
                if (key == System.Windows.Forms.Keys.Escape)
                {
                    if (Buffer.Length > 0)
                    {
                        ReplaceComposedText(_lastComposed, "");
                        CommitComposition();
                        return (IntPtr)1;
                    }
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                // 11. Function Keys (F1-F11, F13-F24) & System Keys (PrintScreen, Scroll, Pause, Menu/Apps)
                bool isFunctionKey = (key >= System.Windows.Forms.Keys.F1 && key <= System.Windows.Forms.Keys.F11) ||
                                     (key >= System.Windows.Forms.Keys.F13 && key <= System.Windows.Forms.Keys.F24);
                bool isSystemKey = key == System.Windows.Forms.Keys.PrintScreen ||
                                   key == System.Windows.Forms.Keys.Scroll ||
                                   key == System.Windows.Forms.Keys.Pause ||
                                   key == System.Windows.Forms.Keys.Apps;
                if (isFunctionKey || isSystemKey)
                {
                    CommitComposition();
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                // 12. Numeric Keypad (Section 5)
                // 12a. Numpad Operators: Multiply (*), Add (+), Subtract (-), Divide (/)
                bool isNumpadOperator = key == System.Windows.Forms.Keys.Multiply ||
                                        key == System.Windows.Forms.Keys.Add ||
                                        key == System.Windows.Forms.Keys.Subtract ||
                                        key == System.Windows.Forms.Keys.Divide;
                if (isNumpadOperator)
                {
                    CommitComposition();
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                // 12b. Numpad Decimal (.)
                if (key == System.Windows.Forms.Keys.Decimal)
                {
                    CommitComposition();
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                // 12c. Numpad Digits (NumPad0 to NumPad9)
                if (key >= System.Windows.Forms.Keys.NumPad0 && key <= System.Windows.Forms.Keys.NumPad9)
                {
                    CommitComposition();
                    bool numLockOn = System.Windows.Forms.Control.IsKeyLocked(System.Windows.Forms.Keys.NumLock);
                    if (numLockOn)
                    {
                        int digitVal = (key - System.Windows.Forms.Keys.NumPad0);
                        char banglaDigit = (char)('০' + digitVal);
                        SendSingleChar(banglaDigit);
                        return (IntPtr)1;
                    }
                    else
                    {
                        // NumLock OFF: Numpad acts as navigation (Home, Up, PgUp, Left, Right, End, Down, PgDn, Ins, Del)
                        return CallNextHookEx(_hookID, nCode, wParam, lParam);
                    }
                }

                // 13. Top Number Row Digits (D0 to D9 without Shift)
                if (key >= System.Windows.Forms.Keys.D0 && key <= System.Windows.Forms.Keys.D9)
                {
                    if (!shiftPressed)
                    {
                        CommitComposition();
                        int digitVal = (key - System.Windows.Forms.Keys.D0);
                        char banglaDigit = (char)('০' + digitVal);
                        SendSingleChar(banglaDigit);
                        return (IntPtr)1;
                    }
                }

                // 14. Convert Key to Character
                char ch = GetCharFromKey(key);
                if (ch != '\0')
                {
                    Buffer.Append(ch);

                    _currentCandidates = BanglishDictionary.Shared.GetCandidates(Buffer.ToString());
                    _selectedCandidateIndex = 0;
                    string newBangla = _currentCandidates.Count > 0 ? _currentCandidates[0] : BanglishEngine.Shared.Transliterate(Buffer.ToString());

                    ReplaceComposedText(_lastComposed, newBangla);
                    _lastComposed = newBangla;

                    TriggerCandidatesUpdate();
                    return (IntPtr)1; // Swallowed, because Bangla was typed live into the active app!
                }
                else
                {
                    // Non-phonetic key (e.g. punctuation !, @, #, $, %, &, *, (, ), -, =, [, ], \, ;, ', ", /, ?, etc.)
                    // Finalize any open composition cleanly so symbol is inserted right after without breaking text!
                    CommitComposition();
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
                if (shift)
                {
                    switch (key)
                    {
                        case System.Windows.Forms.Keys.D6: return '^'; // Chandrabindu (ঁ)
                        case System.Windows.Forms.Keys.D4: return '$'; // Taka (৳)
                    }
                }
                return '\0';
            }

            switch (key)
            {
                case System.Windows.Forms.Keys.OemPeriod: return shift ? '\0' : '.'; // Daari (।)
                case System.Windows.Forms.Keys.Oemcomma: return shift ? '\0' : ','; // Comma (,) / Hasanta (,, -> ্‌)
                case System.Windows.Forms.Keys.Oem1: return shift ? ':' : '\0'; // Bisorgo (ঃ)
                case System.Windows.Forms.Keys.Oemtilde: return shift ? '\0' : '`'; // Avro joiner / escape (`)
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

        public static void ReplaceComposedText(string oldText, string newText)
        {
            if (oldText == null) oldText = "";
            if (newText == null) newText = "";
            if (oldText == newText) return;

            // Differential replacement: compute common prefix so unchanged characters are never deleted/re-rendered!
            int commonPrefixLen = 0;
            int minLen = Math.Min(oldText.Length, newText.Length);
            while (commonPrefixLen < minLen && oldText[commonPrefixLen] == newText[commonPrefixLen])
            {
                commonPrefixLen++;
            }

            int backspaceCount = oldText.Length - commonPrefixLen;
            string textToInsert = newText.Substring(commonPrefixLen);

            if (backspaceCount == 0 && string.IsNullOrEmpty(textToInsert)) return;

            // Pack both backspaces AND new Unicode characters into a SINGLE atomic SendInput batch!
            // No sleep in between! The OS kernel delivers the batch atomically, completely eliminating flicker/blink!
            int totalInputs = (backspaceCount * 2) + (textToInsert.Length * 2);
            INPUT[] inputs = new INPUT[totalInputs];
            int idx = 0;

            // 1. Backspaces for the suffix that changed
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
                            wScan = 0x0E, // Hardware scan code for Backspace
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
                            wScan = 0x0E,
                            dwFlags = 0x0002, // KEYEVENTF_KEYUP
                            time = 0,
                            dwExtraInfo = MAGIC_COOKIE
                        }
                    }
                };
            }

            // 2. New Unicode characters to insert
            for (int i = 0; i < textToInsert.Length; i++)
            {
                char c = textToInsert[i];
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

            uint sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            if (sent == 0)
            {
                for (int i = 0; i < backspaceCount; i++)
                {
                    keybd_event(0x08, 0x0E, 0, MAGIC_COOKIE);
                    keybd_event(0x08, 0x0E, 2, MAGIC_COOKIE);
                }
                foreach (char c in textToInsert)
                {
                    keybd_event(0, (byte)c, 0x0004, MAGIC_COOKIE);
                    keybd_event(0, (byte)c, 0x0004 | 0x0002, MAGIC_COOKIE);
                }
            }
        }

        public static void ReplaceComposedText(int backspaceCount, string newText)
        {
            if (backspaceCount == 0 && string.IsNullOrEmpty(newText)) return;
            int totalInputs = (backspaceCount * 2) + ((newText != null ? newText.Length : 0) * 2);
            INPUT[] inputs = new INPUT[totalInputs];
            int idx = 0;

            for (int i = 0; i < backspaceCount; i++)
            {
                inputs[idx++] = new INPUT
                {
                    type = 1,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = 0x08,
                            wScan = 0x0E,
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
                            wScan = 0x0E,
                            dwFlags = 0x0002,
                            time = 0,
                            dwExtraInfo = MAGIC_COOKIE
                        }
                    }
                };
            }

            if (!string.IsNullOrEmpty(newText))
            {
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
                                dwFlags = 0x0004,
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
                                dwFlags = 0x0004 | 0x0002,
                                time = 0,
                                dwExtraInfo = MAGIC_COOKIE
                            }
                        }
                    };
                }
            }

            uint sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            if (sent == 0)
            {
                for (int i = 0; i < backspaceCount; i++)
                {
                    keybd_event(0x08, 0x0E, 0, MAGIC_COOKIE);
                    keybd_event(0x08, 0x0E, 2, MAGIC_COOKIE);
                }
                if (!string.IsNullOrEmpty(newText))
                {
                    foreach (char c in newText)
                    {
                        keybd_event(0, (byte)c, 0x0004, MAGIC_COOKIE);
                        keybd_event(0, (byte)c, 0x0004 | 0x0002, MAGIC_COOKIE);
                    }
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

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

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
