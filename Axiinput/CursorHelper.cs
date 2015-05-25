using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Axiinput
{
    public static class CursorHelper
    {
        [DllImport("user32.dll", EntryPoint = "SystemParametersInfo")]
        static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);
        [DllImport("user32.dll")]
        static extern IntPtr LoadCursor(IntPtr hInstance, uint lpCursorName);
        [DllImport("user32.dll")]
        static extern IntPtr CreateIconIndirect(ref IconInfo icon);
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetSystemCursor(IntPtr hcur, uint id);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr CopyImage(IntPtr hImage, uint uType, int cxDesired, int cyDesired, uint fuFlags);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetIconInfo(IntPtr hIcon, ref IconInfo pIconInfo);
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DeleteObject([In] IntPtr hObject);

        private const uint SPI_SETCURSORS = 0x0057;
        public static void CursorVisibility(bool pVisible)
        {
            Array pCursors = Enum.GetValues(typeof(IDC_STANDARD_CURSORS));
            if (!pVisible)
            {
                for (int i = 0; i < pCursors.Length; i++)
                {
                    IDC_STANDARD_CURSORS curType = (IDC_STANDARD_CURSORS)pCursors.GetValue(i);
                    System.Drawing.Bitmap EmptyBitmap = new System.Drawing.Bitmap(32, 32);
                    System.Windows.Forms.Cursor returnCursor = CreateCursorNoResize(EmptyBitmap, 0, 0);
                    SetSystemCursor(returnCursor.Handle, (uint)curType);
                }
            }
            else
            {
                SystemParametersInfo(SPI_SETCURSORS, 0, IntPtr.Zero, 0);
            }
        }
     
        private static System.Windows.Forms.Cursor CreateCursorNoResize(Bitmap bmp, int xHotSpot, int yHotSpot)
        {
            IntPtr ptr = IntPtr.Zero;
            for (int i = 5; i > 0; i--)
            {
                try
                {
                    ptr = bmp.GetHicon();
                    IconInfo tmp = new IconInfo();
                    GetIconInfo(ptr, ref tmp);
                    tmp.xHotspot = xHotSpot;
                    tmp.yHotspot = yHotSpot;
                    tmp.fIcon = false;
                    ptr = CreateIconIndirect(ref tmp);

                    if (ptr == IntPtr.Zero)
                    {
                        if (i == 1)
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return new System.Windows.Forms.Cursor(ptr);
                    }
                }
                catch (ExternalException)
                {
                }
                finally
                {
                    if (ptr != null)
                    {
                        DeleteObject(ptr);
                    }
                }
            }
            return null;
        }
    }
}
