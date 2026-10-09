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

        public static Point GetCaretScreenPosition()
        {
            GUITHREADINFO gui = new GUITHREADINFO();
            gui.cbSize = Marshal.SizeOf(gui);

            if (GetGUIThreadInfo(0, ref gui) && gui.hwndCaret != IntPtr.Zero)
            {
                POINT pt = new POINT { x = gui.rcCaret.Left, y = gui.rcCaret.Bottom };
                if (ClientToScreen(gui.hwndCaret, ref pt))
                {
                    if (pt.x > 0 && pt.y > 0)
                    {
                        return new Point(pt.x, pt.y);
                    }
                }
            }

            // Fallback: mouse cursor position
            POINT mousePt;
            GetCursorPos(out mousePt);
            return new Point(mousePt.x + 5, mousePt.y + 15);
        }
    }
}
