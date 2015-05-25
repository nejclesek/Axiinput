using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Axiinput
{
    public static class Common
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        [DllImport("user32.dll")]
        public static extern int EnumWindows(EnumWindowsCallback lpEnumFunc, int lParam);
        public delegate bool EnumWindowsCallback(IntPtr hwnd, int lParam);

        [DllImport("user32.dll")]
        static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        public static ushort DefaultPort
        {
            get
            {
                return Properties.Settings.Default.DefaultPort;
            }
        }
        public static ushort PoolingInterval
        {
            get
            {
                return Properties.Settings.Default.PoolingInterval;
            }
        }
        public static bool UseKeyboard
        {
            get
            {
                return Properties.Settings.Default.UseKeyboard;
            }
        }
        public static bool UseMouse
        {
            get
            {
                return Properties.Settings.Default.UseMouse;
            }
        }
        public static bool ConsumeLocalKeyboardInput = true;
        public static bool ConsumeLocalMouseInput = true;
        public delegate void DGetMyIp(string pIp);
        public static string GetMyIp()
        {
            WebClient pWeb = new WebClient();
            byte[] pData = pWeb.DownloadData("https://api.ipify.org?format=json");
            string pClearedData = Encoding.UTF8.GetString(pData);
            int pStartIndex = pClearedData.IndexOf("ip\":\"");
            if(pStartIndex > -1)
            {
                pStartIndex += 5;
                int pEndIndex = pClearedData.IndexOf('"', pStartIndex);
                if(pEndIndex > -1)
                {
                    return pClearedData.Substring(pStartIndex, pEndIndex - pStartIndex);
                }
            }
            return string.Empty;
        }
        public static void ManageVisibleWindows()
        {
            EnumWindows(EnumWinCallback, 0);         
        }
      
        private static bool EnumWinCallback(IntPtr hwnd, int lParam)
        {
            StringBuilder pClassNameBuilder = new StringBuilder(80);
            int pRet = GetClassName(hwnd, pClassNameBuilder, pClassNameBuilder.Capacity);
            if (pRet != 0)
            {
                string pClassName = pClassNameBuilder.ToString();
                if (pClassName.StartsWith("ad_arrow#15") || pClassName.StartsWith("ad_arrow_beacon#16"))
                {
                    ShowWindowAsync(hwnd, (int)ShowWindowCommands.Hide);
                }
            }
            return true;
        }
        public static void ResetMouseToCenter()
        {
            int xMiddle = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width / 2;
            int yMiddle = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height / 2;
            if(MainWindow.Instance().MainHook != null)
            {
                MainWindow.Instance().MainHook.SetLastPos(xMiddle, yMiddle);
            }
            Input.InputMouseAbsolute(32768, 32768);
        }
        public static void GetMyIpAsync(DGetMyIp pCallback)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(GetMyUpAsynWorker), pCallback);
        }
        private static void GetMyUpAsynWorker(object pCallbackObj)
        {
            DGetMyIp pCallback = (DGetMyIp)pCallbackObj;
            pCallback(GetMyIp());
        }
       
        private static TimeSpan UnixTimeSpan()
        {
            DateTime CetTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "UTC");
            DateTime CetTimeStart = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "UTC");
            return (CetTime - CetTimeStart);
        }
        public static double UnixNowMilis()
        {
            TimeSpan span = UnixTimeSpan();
            return Math.Floor(span.TotalMilliseconds);
        }
        public static void OpenWebSite(string pUrl)
        {
            bool pStarted = false;
            try
            {
                Process pWebSite = new Process();
                pWebSite.StartInfo = new ProcessStartInfo(pUrl);
                pStarted = pWebSite.Start();
            }
            catch
            {
                try
                {
                    if (pStarted == false)
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo("explorer.exe", pUrl);
                        Process.Start(startInfo);
                    }
                }
                catch { }
            }
        }
        public static MOUSEEVENTF HookFlagsToInputFlags(MOUSEHOOKFLAGS pHookflags)
        {
            switch(pHookflags)
            {
                case MOUSEHOOKFLAGS.WM_LBUTTONDOWN:
                    return MOUSEEVENTF.LEFTDOWN;

                case MOUSEHOOKFLAGS.WM_LBUTTONUP:
                    return MOUSEEVENTF.LEFTUP;

                case MOUSEHOOKFLAGS.WM_RBUTTONDOWN:
                    return MOUSEEVENTF.RIGHTDOWN;

                case MOUSEHOOKFLAGS.WM_RBUTTONUP:
                    return MOUSEEVENTF.RIGHTUP;

                case MOUSEHOOKFLAGS.WM_MOUSEMOVE:
                    return MOUSEEVENTF.MOVE;

                case MOUSEHOOKFLAGS.WM_XBUTTONDOWN:
                    return MOUSEEVENTF.XDOWN;

                case MOUSEHOOKFLAGS.WM_XBUTTONUP:
                    return MOUSEEVENTF.XUP;

                case MOUSEHOOKFLAGS.WM_MBUTTONDOWN:
                    return MOUSEEVENTF.MIDDLEDOWN;

                case MOUSEHOOKFLAGS.WM_MBUTTONUP:
                    return MOUSEEVENTF.MIDDLEUP;

                case MOUSEHOOKFLAGS.WM_MOUSEWHEEL:
                    return MOUSEEVENTF.WHEEL;
            }
            return MOUSEEVENTF.MOVE;
        }
    }
}
