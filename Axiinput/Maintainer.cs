using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Axiinput
{
    class Maintainer : IDisposable
    {
        public delegate void MaintainerMethod();
        private List<MaintainerMethod> pMaintainMethods = new List<MaintainerMethod>();
        Thread MaintainerThread = null;
        private object MaintainerLock = new object();
        private short pMaintainInterval = 16;
        private bool pShouldRunMaintainer = true;
        public Maintainer()
        {
            MaintainerThread = new Thread(new ThreadStart(RunMaintainer));
            MaintainerThread.Start();
        }
        private void RunMaintainer()
        {
            while (pShouldRunMaintainer)
            {
                if (pMaintainMethods.Count > 0)
                {
                    lock (MaintainerLock)
                    {
                        for (ushort x = 0; x < pMaintainMethods.Count; x++)
                        {
                            pMaintainMethods[x].Invoke();
                        }
                    }
                }
                Thread.Sleep(pMaintainInterval);
            }
        }
        public void AddMethod(MaintainerMethod pMethod)
        {
            lock (MaintainerLock)
            {
                pMaintainMethods.Add(pMethod);
            }
        }
        public void Dispose()
        {
            pShouldRunMaintainer = false;
            MaintainerThread = null;
        }
    }
}
