using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Axiinput
{
    public static class Shortcuts
    {
        public delegate void DShortcutInput();
        public static event DShortcutInput EQuitInput = null;
        public static event DShortcutInput EToggleMinimize = null;
        public static event DShortcutInput EToggleConnect = null;
        private static long pEscInRow = 0;
        private static double pLastEscTime = 0;
        private static ushort pEscQuitEventAt = 4;
        private static bool pAltKeyDown = false;
        private static bool pXKeyDown = false;
        private static bool pCKeyDown = false;
        private static bool pToggleFired = false;
        private static bool pConnectFired = false;
        public static bool ProcessShortcutsSend(bool pKeyUp, byte pScanCode)
        {
            if (pScanCode == 1)
            {
                double pCurTime = Common.UnixNowMilis();
                if(pLastEscTime == 0 || pCurTime - pLastEscTime < 100)
                {                    
                    pLastEscTime = pCurTime;
                    pEscInRow++;
                    if (pEscInRow == pEscQuitEventAt)
                    {
                        if(EQuitInput != null)
                        {
                            EQuitInput();
                        }
                        ResetEscTracking();
                    }
                }
                else
                {
                    ResetEscTracking();
                }
            }
            else
            {
                ResetEscTracking();
            }
            if(pScanCode == 56)
            {
                pAltKeyDown = !pKeyUp;                
            }
            if (pScanCode == 45)
            {
                pXKeyDown = !pKeyUp;
            }
            if (pScanCode == 46)
            {
                pCKeyDown = !pKeyUp;
            }
            if(pAltKeyDown)
            {
                if(pXKeyDown)
                {
                    if(EToggleConnect != null)
                    {
                        EToggleConnect();
                        pConnectFired = true;
                    }
                }
                else
                {
                    pConnectFired = false;
                }
                if (pCKeyDown)
                {
                    if (EToggleMinimize != null)
                    {
                        EToggleMinimize();
                        pToggleFired = true;
                    }
                }
                else
                {
                    pToggleFired = false;
                }
            }
            else
            {
                pConnectFired = false;
                pToggleFired = false;
            }
            return true;
        }
        private static void ResetEscTracking()
        {
            pLastEscTime = 0;
            pEscInRow = 0;
        }
    }
}
