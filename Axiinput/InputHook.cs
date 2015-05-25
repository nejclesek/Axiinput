using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Axiinput
{
    public class InputHook
    {
        private Thread HookThread = null;
        public event DKeyboardInput EKeyboardInput = null;
        public event DMouseInput EMouseInput = null;
        public delegate void DKeyboardInput(bool KeyUp, KBDLLHOOKSTRUCTFlags pFlags, byte ScanCode);
        public delegate void DMouseInput(MOUSEEVENTF State, short xChange, short yChange, short wheelDelta);
        private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelProc MouseCallbackD = null;
        private LowLevelProc KeyboardCallbackD = null;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
          LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
          IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string p_LpModuleName);

        private System.Windows.Forms.Form MessageForm = null;

        IntPtr MouseHookId = IntPtr.Zero;
        IntPtr KeyboardHookId = IntPtr.Zero;
        POINT pLastPos = new POINT();

        private const int WH_MOUSE_LL = 14;
        protected const int WH_KEYBOARD_LL = 13;
       
        bool pFirstMouseHookCall = true;
        bool MouseHooked = false;
        bool KeyboardHooked = false;
        public bool ConsumeKeyboard = false;
        public bool ConsumeMouse = false;
        public class StartHookParams
        {
            public bool StartKeyboardHook = true;
            public bool StartMouseHook = true;
        }
        public InputHook(bool pStartKeyboardHook = true, bool pStartMouseHook = true, bool pConsumeKeyboard = false, bool pConsumeMouse = false)
        {
            ConsumeKeyboard = pConsumeKeyboard;
            ConsumeMouse = pConsumeMouse;
            HookThread = new Thread(new ParameterizedThreadStart(StartHook));
            HookThread.SetApartmentState(ApartmentState.STA);
            HookThread.Start(new StartHookParams() { StartKeyboardHook = pStartKeyboardHook, StartMouseHook = pStartMouseHook } );            
        }
        public void DisposeHook()
        {
            StopMouseHook();
            StopKeyboardHook();
            HookThread = null;
            MessageForm.Invoke(new System.Windows.Forms.MethodInvoker(MessageForm.Close));
        }
        private void StartHook(object pStartHookParams)
        {
            StartHookParams pParams = (StartHookParams)pStartHookParams;
            if (pParams.StartMouseHook)
            {
                StartMouseHook();
            }
            if(pParams.StartKeyboardHook)
            {
                StartKeyboardHook();
            }
            MessageForm = new System.Windows.Forms.Form();
            MessageForm.ShowInTaskbar = false;
            MessageForm.Size = new System.Drawing.Size(0, 0);
            MessageForm.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            MessageForm.Opacity = 0;
            MessageForm.Load += MessageForm_Load;
            System.Windows.Forms.Application.Run(MessageForm);
        }

        void MessageForm_Load(object sender, EventArgs e)
        {
            MessageForm.Visible = false;
        }
        public void StartMouseHook()
        {
            if(MouseHookId == IntPtr.Zero)
            {
                MouseCallbackD = MouseHookCallback;
                MouseHookId = SetMouseHook(MouseCallbackD);
            }
        }
        public void StartKeyboardHook()
        {
            if(KeyboardHookId == IntPtr.Zero)
            {
                KeyboardCallbackD = KeyboardHookCallback;
                KeyboardHookId = SetKeyboardHook(KeyboardCallbackD);     
            }
        }
        public void StopMouseHook()
        {
            if (MouseHookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(MouseHookId);
            }
        }
        public void StopKeyboardHook()
        {
            if (KeyboardHookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(KeyboardHookId);
            }
        }
        private IntPtr SetMouseHook(LowLevelProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            {
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    MouseHookId = SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                    MouseHooked = (MouseHookId != IntPtr.Zero);
                    return MouseHookId;
                }
            }
        }
        private IntPtr SetKeyboardHook(LowLevelProc proc)
        {
            using (Process processTmp = Process.GetCurrentProcess())
            {
                using (ProcessModule curModule = processTmp.MainModule)
                {
                    KeyboardHookId = SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                    KeyboardHooked = (KeyboardHookId != null);
                    return KeyboardHookId;
                }
            }
        }
        protected IntPtr KeyboardHookCallback(int p_Code, IntPtr p_WParam, IntPtr p_LParam)
        {
            if (EKeyboardInput != null)
            {
                KBDLLHOOKSTRUCT kblStruct = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(p_LParam, typeof(KBDLLHOOKSTRUCT));
                if (p_Code < 0 || (kblStruct.flags & KBDLLHOOKSTRUCTFlags.LLKHF_INJECTED) == KBDLLHOOKSTRUCTFlags.LLKHF_INJECTED) // 0x14 - CAPITAL
                {
                    if (!ConsumeKeyboard)
                    {
                        return CallNextHookEx(IntPtr.Zero, p_Code, p_WParam, p_LParam);
                    }
                }
                else
                {
                    EKeyboardInput((kblStruct.flags & KBDLLHOOKSTRUCTFlags.LLKHF_UP) == KBDLLHOOKSTRUCTFlags.LLKHF_UP, kblStruct.flags, BitConverter.GetBytes(kblStruct.scanCode)[0]);
                }
            }
            if (!ConsumeKeyboard)
            {
                return CallNextHookEx(IntPtr.Zero, p_Code, p_WParam, p_LParam);
            }
            else
            {
                return (IntPtr)1;
            }
        }
        
        public void SetLastPos(int pX, int pY)
        {
            pLastPos.x = pX;
            pLastPos.y = pY;
        }
        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
            if (EMouseInput != null)
            {                
                if (!pFirstMouseHookCall)
                {
                    short pX = Convert.ToInt16(hookStruct.pt.x - pLastPos.x);
                    short pY = Convert.ToInt16(hookStruct.pt.y - pLastPos.y);
                    if((MOUSEHOOKFLAGS)wParam == MOUSEHOOKFLAGS.WM_MOUSEMOVE && (pX != 0 || pY != 0) || (MOUSEHOOKFLAGS)wParam != MOUSEHOOKFLAGS.WM_MOUSEMOVE)
                    {
                        EMouseInput(Common.HookFlagsToInputFlags((MOUSEHOOKFLAGS)wParam), pX, pY, (short)(hookStruct.mouseData >> 16));
                    }
                }
                else
                {
                    pFirstMouseHookCall = false;
                }
                pLastPos.x = hookStruct.pt.x;
                pLastPos.y = hookStruct.pt.y;
            }
            if (!ConsumeMouse || (MOUSEHOOKFLAGS)wParam == MOUSEHOOKFLAGS.WM_MOUSEMOVE)
            {
                return CallNextHookEx(MouseHookId, nCode, wParam, lParam);
            }
            else
            {
                return (IntPtr)1;
            }
        }
    }
}
