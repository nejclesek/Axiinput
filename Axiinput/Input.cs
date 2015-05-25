using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Axiinput
{
    public static class Input
    {
        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint p_Code, uint p_MapType);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern UInt32 SendInput(UInt32 numberOfInputs, INPUT[] inputs, Int32 sizeOfInputStructure);
        public static void EnterKeyDown(VirtualKeyCode pKeyCode, uint pScanCode, KBDLLHOOKSTRUCTFlags pFlags, short pAttemptNumber = 0)
        {
            if (pScanCode == 0)
            {
                pScanCode = MapVirtualKey((uint)pKeyCode, (uint)MapVirtualKeyEnum.MAPVK_VK_TO_VSC);
            }
            INPUT down;
            if (pScanCode == 0)
            {
                down = KeyDownVirtualCode(pKeyCode);
            }
            else
            {
                down = KeyDownScanCode(pScanCode, pFlags);
            }
            INPUT[] inputList = new INPUT[1];
            inputList[0] = down;
            uint numberOfSuccessfulSimulatedInputs = SendInput(1, inputList, Marshal.SizeOf(typeof(INPUT)));
            if (numberOfSuccessfulSimulatedInputs == 0)
            {
                if (pAttemptNumber < 5)
                {
                    EnterKeyDown(pKeyCode, pScanCode, pFlags, ++pAttemptNumber);
                }
            }
        }

        public static void EnterKeyUp(VirtualKeyCode pKeyCode, uint pScanCode, KBDLLHOOKSTRUCTFlags p_Flags, short pAttemptNumber = 0)
        {
            if (pScanCode == 0)
            {
                pScanCode = MapVirtualKey((uint)pKeyCode, (uint)MapVirtualKeyEnum.MAPVK_VK_TO_VSC);
            }
            INPUT up;
            if (pScanCode == 0)
            {
                up = KeyUpVirtualCode(pKeyCode);
            }
            else
            {
                up = KeyUpScanCode(pScanCode, p_Flags);
            }
            INPUT[] inputList = new INPUT[1];
            inputList[0] = up;

            uint numberOfSuccessfulSimulatedInputs = SendInput(1, inputList, Marshal.SizeOf(typeof(INPUT)));
            if (numberOfSuccessfulSimulatedInputs == 0)
            {
                if (pAttemptNumber < 5)
                {
                    EnterKeyUp(pKeyCode, pScanCode, p_Flags, ++pAttemptNumber);
                }
            }
        }
        public static void InputMouseRelative(short x, short y, MOUSEEVENTF pStateFlags, short pWheelDelta, short pAttemptNumber = 0)
        {
            INPUT pMouseData = new INPUT { Type = (UInt32)InputType.MOUSE };
            pMouseData.Data.Mouse.Time = 0;
            pMouseData.Data.Mouse.Flags = (UInt32)pStateFlags;
            pMouseData.Data.Mouse.X = (int)x;
            pMouseData.Data.Mouse.Y = (int)y;
            pMouseData.Data.Mouse.MouseData = (uint)pWheelDelta;
           
            INPUT[] inputList = new INPUT[1];
            inputList[0] = pMouseData;
            uint numberOfSuccessfulSimulatedInputs = SendInput(1, inputList, Marshal.SizeOf(typeof(INPUT)));
            if (numberOfSuccessfulSimulatedInputs == 0)
            {
                if (pAttemptNumber < 5)
                {
                    InputMouseRelative(x, y, pStateFlags, pWheelDelta, ++pAttemptNumber);
                }
            }
        }
        public static void InputMouseAbsolute(int x, int y, MOUSEEVENTF pStateFlags = MOUSEEVENTF.ABSOLUTE | MOUSEEVENTF.MOVE, short pWheelDelta = 0, short pAttemptNumber = 0)
        {
            INPUT pMouseData = new INPUT { Type = (UInt32)InputType.MOUSE };
            pMouseData.Data.Mouse.Time = 0;
            pMouseData.Data.Mouse.Flags = (UInt32)(pStateFlags | MOUSEEVENTF.ABSOLUTE | MOUSEEVENTF.MOVE);
            pMouseData.Data.Mouse.X = x;
            pMouseData.Data.Mouse.Y = y;
            pMouseData.Data.Mouse.MouseData = (uint)pWheelDelta;
            INPUT[] inputList = new INPUT[1];
            inputList[0] = pMouseData;
            uint numberOfSuccessfulSimulatedInputs = SendInput(1, inputList, Marshal.SizeOf(typeof(INPUT)));
            if (numberOfSuccessfulSimulatedInputs == 0)
            {
                if (pAttemptNumber < 5)
                {
                    InputMouseAbsolute(x, y, pStateFlags, pWheelDelta, ++pAttemptNumber);
                }
            }
        }
        public static void InputMouseAbsoluteNormalized(float x, float y, MOUSEEVENTF pStateFlags = MOUSEEVENTF.ABSOLUTE | MOUSEEVENTF.MOVE, short pWheelDelta = 0)
        {
            int pX = (int)(65535.0f * x);
            int pY = (int)(65535.0f * y);
            InputMouseAbsolute(pX, pY, pStateFlags, pWheelDelta);
        }
        private static INPUT KeyUpVirtualCode(VirtualKeyCode p_KeyCode)
        {
            var up = new INPUT
            {
                Type = (UInt32)InputType.KEYBOARD,
                Data =
                {
                    Keyboard = new KEYBDINPUT
                    {
                        Vk = (UInt16)p_KeyCode,
                        Scan = 0,
                        Flags = (uint)KeyboardFlag.KEYUP,
                        Time = 0,
                        ExtraInfo = IntPtr.Zero
                    }
                }
            };
            return up;
        }

        private static INPUT KeyUpScanCode(uint pScanCode, KBDLLHOOKSTRUCTFlags pFlags)
        {
            var up = new INPUT
            {
                Type = (UInt32)InputType.KEYBOARD,
                Data =
                {
                    Keyboard = new KEYBDINPUT
                    {
                        Scan = (ushort)pScanCode,
                        Flags = (uint)KeyboardFlag.KEYUP | (uint)KeyboardFlag.SCANCODE,
                        Time = 0,
                        ExtraInfo = IntPtr.Zero
                    }
                }
            };
            if ((pFlags & KBDLLHOOKSTRUCTFlags.LLKHF_EXTENDED) == KBDLLHOOKSTRUCTFlags.LLKHF_EXTENDED)
            {
                up.Data.Keyboard.Flags |= (uint)KeyboardFlag.EXTENDEDKEY;
            }
            return up;
        }
        private static INPUT KeyDownVirtualCode(VirtualKeyCode pKeyCode)
        {
            var down = new INPUT
            {
                Type = (UInt32)InputType.KEYBOARD,
                Data =
                {
                    Keyboard = new KEYBDINPUT { Vk = (UInt16)pKeyCode, Scan = 0, Flags = 0, Time = 0, ExtraInfo = IntPtr.Zero }
                }
            };
            return down;
        }

        private static INPUT KeyDownScanCode(uint pScanCode, KBDLLHOOKSTRUCTFlags pFlags)
        {
            var down = new INPUT
            {
                Type = (UInt32)InputType.KEYBOARD,
                Data =
                {
                    Keyboard = new KEYBDINPUT
                      {
                          Scan = (ushort)pScanCode,
                          Flags = (uint)KeyboardFlag.SCANCODE,
                          Time = 0,
                          ExtraInfo = IntPtr.Zero
                      }
                }
            };
            if ((pFlags & KBDLLHOOKSTRUCTFlags.LLKHF_EXTENDED) == KBDLLHOOKSTRUCTFlags.LLKHF_EXTENDED)
            {
                down.Data.Keyboard.Flags |= (uint)KeyboardFlag.EXTENDEDKEY;
            }
            return down;
        }
    }
}
