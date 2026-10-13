using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Banglish.Core
{
    public static class Win32Caret
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct GUITHREADINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public RECT rcCaret;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        private static Rectangle _lastKnownCaretRect = Rectangle.Empty;
        private static long _lastCaretTick = 0;

        public static Rectangle GetCaretRect()
        {
            IntPtr fgHwnd = GetForegroundWindow();
            if (fgHwnd == IntPtr.Zero)
            {
                if (!_lastKnownCaretRect.IsEmpty) return _lastKnownCaretRect;
                return new Rectangle(100, 100, 2, 22);
            }

            uint fgPid;
            uint fgThreadId = GetWindowThreadProcessId(fgHwnd, out fgPid);

            // If the foreground window belongs to Banglish itself (e.g. user clicked candidate or toggle),
            // keep the last known typing position
            uint currentPid = (uint)Process.GetCurrentProcess().Id;
            if (fgPid == currentPid && !_lastKnownCaretRect.IsEmpty)
            {
                return _lastKnownCaretRect;
            }

            RECT fgRect;
            if (!GetWindowRect(fgHwnd, out fgRect))
            {
                fgRect = new RECT { Left = 0, Top = 0, Right = 1920, Bottom = 1080 };
            }

            // 1. Check Win32 Caret via GetGUIThreadInfo on foreground thread
            GUITHREADINFO gui = new GUITHREADINFO();
            gui.cbSize = Marshal.SizeOf(gui);

            if (GetGUIThreadInfo(fgThreadId, ref gui))
            {
                if (gui.hwndCaret != IntPtr.Zero)
                {
                    int caretH = gui.rcCaret.Bottom - gui.rcCaret.Top;
                    if (caretH <= 0 || caretH > 80) caretH = 22;

                    POINT pt = new POINT { x = gui.rcCaret.Left, y = gui.rcCaret.Top };
                    if (ClientToScreen(gui.hwndCaret, ref pt))
                    {
                        // Ensure it is inside or near the active foreground window
                        if (pt.x >= fgRect.Left - 50 && pt.x <= fgRect.Right + 50 &&
                            pt.y >= fgRect.Top - 50 && pt.y <= fgRect.Bottom + 50)
                        {
                            Rectangle rect = new Rectangle(pt.x, pt.y, 2, caretH);
                            _lastKnownCaretRect = rect;
                            _lastCaretTick = Environment.TickCount;
                            return rect;
                        }
                    }
                }
            }

            // 2. Fallback: Check system-wide active thread caret without queue attachment
            GUITHREADINFO globalGui = new GUITHREADINFO();
            globalGui.cbSize = Marshal.SizeOf(globalGui);
            if (GetGUIThreadInfo(0, ref globalGui) && globalGui.hwndCaret != IntPtr.Zero)
            {
                int caretH = globalGui.rcCaret.Bottom - globalGui.rcCaret.Top;
                if (caretH <= 0 || caretH > 80) caretH = 22;

                POINT p = new POINT { x = globalGui.rcCaret.Left, y = globalGui.rcCaret.Top };
                if (ClientToScreen(globalGui.hwndCaret, ref p))
                {
                    if (p.x >= fgRect.Left - 50 && p.x <= fgRect.Right + 50 &&
                        p.y >= fgRect.Top - 50 && p.y <= fgRect.Bottom + 50)
                    {
                        Rectangle rect = new Rectangle(p.x, p.y, 2, caretH);
                        _lastKnownCaretRect = rect;
                        _lastCaretTick = Environment.TickCount;
                        return rect;
                    }
                }
            }

            // 3. Check if focused control is a specific input control (e.g. search bar, address bar)
            if (gui.hwndFocus != IntPtr.Zero)
            {
                RECT fRect;
                if (GetWindowRect(gui.hwndFocus, out fRect))
                {
                    int w = fRect.Right - fRect.Left;
                    int h = fRect.Bottom - fRect.Top;
                    if (w > 20 && w < 900 && h > 15 && h < 180)
                    {
                        Rectangle rect = new Rectangle(fRect.Left + 12, fRect.Top + 6, 2, Math.Min(22, h - 8));
                        _lastKnownCaretRect = rect;
                        _lastCaretTick = Environment.TickCount;
                        return rect;
                    }
                }
            }

            // 4. Stable typing memory: hold last known caret briefly (800ms) to prevent jitter without freezing across multi-line text
            if (!_lastKnownCaretRect.IsEmpty && (Environment.TickCount - _lastCaretTick) < 800)
            {
                if (_lastKnownCaretRect.Left >= fgRect.Left && _lastKnownCaretRect.Left <= fgRect.Right &&
                    _lastKnownCaretRect.Top >= fgRect.Top && _lastKnownCaretRect.Top <= fgRect.Bottom)
                {
                    return _lastKnownCaretRect;
                }
            }

            // 5. Mouse position - ONLY IF the mouse is genuinely INSIDE the active foreground window
            POINT mousePt;
            GetCursorPos(out mousePt);
            if (mousePt.x >= fgRect.Left + 15 && mousePt.x <= fgRect.Right - 15 &&
                mousePt.y >= fgRect.Top + 30 && mousePt.y <= fgRect.Bottom - 15)
            {
                Rectangle rect = new Rectangle(mousePt.x, mousePt.y, 2, 22);
                _lastKnownCaretRect = rect;
                _lastCaretTick = Environment.TickCount;
                return rect;
            }

            // 6. Final safe fallback: near the top-third typing area of the foreground window (never in the corner of the screen!)
            int safeX = fgRect.Left + Math.Min(120, (fgRect.Right - fgRect.Left) / 4);
            int safeY = fgRect.Top + Math.Min(100, (fgRect.Bottom - fgRect.Top) / 3);
            return new Rectangle(safeX, safeY, 2, 22);
        }

        public static Point GetCaretScreenPosition()
        {
            Rectangle r = GetCaretRect();
            return new Point(r.Left, r.Bottom);
        }
    }
}
