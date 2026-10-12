using System;
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

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool GetCaretPos(out POINT lpPoint);

        private static Rectangle _lastKnownCaretRect = Rectangle.Empty;
        private static long _lastCaretTick = 0;

        public static Rectangle GetCaretRect()
        {
            GUITHREADINFO gui = new GUITHREADINFO();
            gui.cbSize = Marshal.SizeOf(gui);

            if (GetGUIThreadInfo(0, ref gui))
            {
                IntPtr targetHwnd = gui.hwndCaret != IntPtr.Zero ? gui.hwndCaret : gui.hwndFocus;
                if (targetHwnd != IntPtr.Zero)
                {
                    int caretH = gui.rcCaret.Bottom - gui.rcCaret.Top;
                    if (caretH <= 0 || caretH > 80) caretH = 22; // standard text line height

                    POINT ptTop = new POINT { x = gui.rcCaret.Left, y = gui.rcCaret.Top };
                    if (ClientToScreen(targetHwnd, ref ptTop) && ptTop.x > 0 && ptTop.y > 0)
                    {
                        Rectangle rect = new Rectangle(ptTop.x, ptTop.y, 2, caretH);
                        _lastKnownCaretRect = rect;
                        _lastCaretTick = Environment.TickCount;
                        return rect;
                    }

                    // Fallback using GetCaretPos on focused handle
                    POINT localCaret;
                    if (GetCaretPos(out localCaret) && (localCaret.x > 0 || localCaret.y > 0))
                    {
                        if (ClientToScreen(targetHwnd, ref localCaret) && localCaret.x > 0 && localCaret.y > 0)
                        {
                            Rectangle rect = new Rectangle(localCaret.x, localCaret.y, 2, 22);
                            _lastKnownCaretRect = rect;
                            _lastCaretTick = Environment.TickCount;
                            return rect;
                        }
                    }
                }
            }

            // If we recently had a valid caret position (within last 3 seconds of typing), keep using it to prevent jumping
            if (!_lastKnownCaretRect.IsEmpty && (Environment.TickCount - _lastCaretTick) < 3000)
            {
                return _lastKnownCaretRect;
            }

            // Fallback: mouse cursor position
            POINT mousePt;
            GetCursorPos(out mousePt);
            Rectangle fallbackRect = new Rectangle(mousePt.x, mousePt.y, 2, 22);
            _lastKnownCaretRect = fallbackRect;
            _lastCaretTick = Environment.TickCount;
            return fallbackRect;
        }

        public static Point GetCaretScreenPosition()
        {
            Rectangle r = GetCaretRect();
            return new Point(r.Left, r.Bottom);
        }
    }
}
