using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace Axiinput
{
    class UDPClient : IDisposable
    {
        Thread ClientThread = null;
        private List<byte[]> pDataQue = new List<byte[]>();
        private IPAddress IpAdress;
        private object pClientLock = new object();
        private int Port;
        private bool pShouldRunClient = true;
        public bool Running
        {
            get
            {
                return pShouldRunClient;
            }
        }
        public UDPClient(IPAddress pIpAdress, int pPort)
        {            
            this.IpAdress = pIpAdress;
            this.Port = pPort;
            ClientThread = new Thread(new ThreadStart(RunClient));
            ClientThread.Start();           
        }
        private void RunClient()
        {
            UdpClient pClient = new UdpClient(IpAdress.ToString(), Port);         
            while(true)
            {
                lock (pClientLock)
                {
                    if (pDataQue.Count == 0)
                    {
                        Monitor.Wait(pClientLock);
                    }
                    if (pShouldRunClient)
                    {
                        for (short x = 0; x < pDataQue.Count; x++)
                        {
                            pClient.Send(pDataQue[x], pDataQue[x].Length);
                        }
                        pDataQue.Clear();
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        public void Dispose()
        {
            pShouldRunClient = false;
            SendData(new  byte[]{ 0x00 });
            ClientThread = null;
        }
        public void SendData(byte[] pData)
        {
            lock (pClientLock)
            {
                pDataQue.Add(pData);
                Monitor.PulseAll(pClientLock);
            }
        }
    }
}
