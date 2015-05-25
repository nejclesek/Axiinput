using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Axiinput
{
    class InputSender : IDisposable
    {
        Thread SenderThread = null;
        public delegate bool DProcessShortcuts(bool pKeyUp, byte pScanCode);
        public event DProcessShortcuts EProcessShortcutsSend = null;
        private object pSenderLock = new object();
        private List<InputData> pInputQue = new List<InputData>();
        private bool pShouldRunSender = true;
        public bool InputKeyboard = true;
        public bool InputMouse = true;
        public class InputData
        {
            public bool IsKeyboard;

            public KBDLLHOOKSTRUCTFlags KeyboardState;
            public byte ScanCode;

            public short PosX;
            public short PosY;
            public MOUSEEVENTF KeyState;
            public short WheelDelta;
        }
        public void Dispose()
        {
            pShouldRunSender = false;
            List<InputData> pDummyInput = new List<InputData>();
            pDummyInput.Add(new InputData());
            InputNow(pDummyInput);
            SenderThread = null;
        }
        public InputSender()
        {
            SenderThread = new Thread(new ThreadStart(RunInputSender));
            SenderThread.Start();
        }
        private void RunInputSender()
        {
            try
            {
                while (true)
                {
                    lock (pSenderLock)
                    {
                        if (pInputQue.Count == 0)
                        {
                            Monitor.Wait(pSenderLock);
                        }
                        if (!pShouldRunSender)
                        {
                            break;
                        }
                        for (short x = 0; x < pInputQue.Count; x++)
                        {
                            if (pInputQue[x].IsKeyboard)
                            {
                                bool pKeyUp = (pInputQue[x].KeyboardState & KBDLLHOOKSTRUCTFlags.LLKHF_UP) == KBDLLHOOKSTRUCTFlags.LLKHF_UP;
                                if (EProcessShortcutsSend == null || EProcessShortcutsSend(pKeyUp, pInputQue[x].ScanCode))
                                {
                                    if (pKeyUp)
                                    {
                                        Input.EnterKeyUp(VirtualKeyCode.NONAME, pInputQue[x].ScanCode, pInputQue[x].KeyboardState | KBDLLHOOKSTRUCTFlags.LLKHF_INJECTED);
                                    }
                                    else
                                    {
                                        Input.EnterKeyDown(VirtualKeyCode.NONAME, pInputQue[x].ScanCode, pInputQue[x].KeyboardState | KBDLLHOOKSTRUCTFlags.LLKHF_INJECTED);
                                    }
                                }
                            }
                            else
                            {
                                Input.InputMouseRelative(pInputQue[x].PosX, pInputQue[x].PosY, pInputQue[x].KeyState, pInputQue[x].WheelDelta);
                            }
                        }
                        pInputQue.Clear();
                    }
                }
            }
            catch
            {
                if (pShouldRunSender)
                {
                    RunInputSender();
                }
            }
        }
        public void InputNow(List<InputData> pToInput)
        {
            lock (pSenderLock)
            {
                pInputQue.AddRange(pToInput);
                Monitor.PulseAll(pSenderLock);
            }
        }
    }
}
