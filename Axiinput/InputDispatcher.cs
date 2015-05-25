using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Axiinput
{
    class InputDispatcher : IDisposable
    {
        Thread DispatcherThread = null;
        UDPClient ClientToSendTo = null;
        List<InputSender.InputData> pInputQue = new List<InputSender.InputData>();
        object pInputLock = new object();
        public int DispatchTime;
        bool pShouldRunDispatcher = true;
        public bool DispatchKeyboard = true;
        public bool DispatchMouse = true;
        public InputDispatcher(UDPClient pClientToSendTo, int pDispatchTime = 14)
        {
            this.ClientToSendTo = pClientToSendTo;
            this.DispatchTime = pDispatchTime;
            DispatcherThread = new Thread(new ThreadStart(RunDispatcher));
            DispatcherThread.Start();
        }
        void RunDispatcher()
        {
            while (pShouldRunDispatcher)
            {
                if (pInputQue.Count > 0)
                {
                    List<byte> pData = new List<byte>();
                    lock (pInputLock)
                    {                        
                        for(short x = 0; x < pInputQue.Count; x++)
                        {
                            pData.Add(Convert.ToByte(pInputQue[x].IsKeyboard));
                            if (pInputQue[x].IsKeyboard)
                            {
                                if (DispatchKeyboard)
                                {
                                    byte pIsKeyUp = Convert.ToByte(pInputQue[x].KeyboardState);
                                    byte[] pScanCode = BitConverter.GetBytes(pInputQue[x].ScanCode);
                                    pData.Add(pIsKeyUp);
                                    pData.Add(pScanCode[0]);
                                }
                            }
                            else
                            {
                                if (DispatchMouse)
                                {
                                    if (pInputQue[x].PosX != 0 || pInputQue[x].PosY != 0)
                                    {
                                        pInputQue[x].KeyState |= MOUSEEVENTF.MOVE;
                                    }
                                    byte[] pState = BitConverter.GetBytes((ushort)pInputQue[x].KeyState);
                                    pData.AddRange(pState);
                                    if ((pInputQue[x].KeyState & MOUSEEVENTF.MOVE) == MOUSEEVENTF.MOVE)
                                    {
                                        pData.AddRange(BitConverter.GetBytes(pInputQue[x].PosX));
                                        pData.AddRange(BitConverter.GetBytes(pInputQue[x].PosY));
                                    }
                                    if ((pInputQue[x].KeyState & MOUSEEVENTF.WHEEL) == MOUSEEVENTF.WHEEL ||(pInputQue[x].KeyState & MOUSEEVENTF.HWHEEL) == MOUSEEVENTF.HWHEEL)
                                    {
                                        pData.AddRange(BitConverter.GetBytes(pInputQue[x].WheelDelta));
                                    }
                                }
                            }
                        }
                        pInputQue.Clear();
                    }
                    if (DispatchMouse)
                    {
                        Common.ResetMouseToCenter();
                    }
                    ClientToSendTo.SendData(pData.ToArray());
                }
                Thread.Sleep(DispatchTime);
            }
        }
        public void AddInput(InputSender.InputData pInput)
        {
            lock(pInputLock)
            {
                pInputQue.Add(pInput);
            }
        }
        public void Dispose()
        {
            pShouldRunDispatcher = false;
            DispatcherThread = null;
        }
    }
}
