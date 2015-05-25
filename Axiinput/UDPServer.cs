using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace Axiinput
{
    class UDPServer : IDisposable
    {
        public delegate bool DDataRecivedContinue(byte[] pData);
        public DDataRecivedContinue EDataRecivedContinue = null;
        Thread ServerThread = null;
        UdpClient MainClient = null;
        private int Port;
        private bool pShouldRunServer = true;
        public bool Running
        {
            get
            {
                return pShouldRunServer;
            }
        }
        public UDPServer(int pPort = 8128)
        {
            this.Port = pPort;
            ServerThread = new Thread(new ThreadStart(RunServer));
            ServerThread.Start();           
        }
        public void Dispose()
        {
            pShouldRunServer = false;
            if (MainClient != null)
            {
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Loopback, Common.DefaultPort);
                MainClient.Connect(RemoteIpEndPoint);
                MainClient.Send(new byte[] { 0x00 }, 1);
            }
            ServerThread = null;
        }
        private void RunServer()
        {
            MainClient = new UdpClient(Port);
            try
            {
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, Common.DefaultPort);
                while (true)
                {
                    byte[] pData = MainClient.Receive(ref RemoteIpEndPoint);
                    if (!pShouldRunServer)
                    {
                        break;
                    }
                    if (EDataRecivedContinue != null)
                    {
                        if (EDataRecivedContinue(pData) == false)
                        {
                            break;
                        }
                    }
                }
            }
            finally
            {
               if(MainClient != null)
               {
                   MainClient.Close();
               }
            }
        }
    }
}
