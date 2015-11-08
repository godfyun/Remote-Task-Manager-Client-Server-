using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DispatcherServer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 5000);
                server.Start();
                while (true)
                {
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connection from: " + client.Client.RemoteEndPoint.ToString());
                    new ThrReadWriter(client);
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        class ThrReadWriter
        {
            private TcpClient client;

            public ThrReadWriter(TcpClient client)
            {
                this.client = client;
                Thread T = new Thread(this.run);
                T.IsBackground = true;
                T.Start();
            }

            public void run()
            {
                NetworkStream NS = client.GetStream();
                byte[] b = new byte[1024];
                byte[] a = null;
                MemoryStream MS = new MemoryStream();
                try
                {
                    while (true)
                    {
                        int totalSize = 0;
                        BinaryReader BR = new BinaryReader(NS);
                        int transmitSize = BR.ReadInt32();
                        do
                        {
                            int cnt = NS.Read(b, 0, b.Length);
                            if (cnt == 0)
                            {
                                throw new IOException("0 bytes received.");
                            }
                            MS.Write(b, 0, cnt);
                            totalSize += cnt;
                        } while (totalSize < transmitSize);
                        String msg = Encoding.UTF8.GetString(MS.GetBuffer(), 0, (int)MS.Length);
                        BinaryWriter BW = new BinaryWriter(NS);
                        BW.Write(totalSize);
                        string[] data = msg.Split('|');
                        switch (data[0])
                        {
                            case "REFRESH":
                                Console.WriteLine("Refresh request.");
                                Process[] processes = Process.GetProcesses();
                                string prcssMsg = "REFRESH|";
                                for (int i = 0; i < processes.Length; i++)
                                {
                                    try
                                    {
                                        prcssMsg = prcssMsg + processes[i].Id + "$";
                                    }
                                    catch
                                    {
                                        prcssMsg = prcssMsg + "N/A" + "$";
                                    }
                                    try
                                    {
                                        prcssMsg = prcssMsg + processes[i].ProcessName + "$";
                                    }
                                    catch
                                    {
                                        prcssMsg = prcssMsg + "N/A" + "$";
                                    }
                                    try
                                    {
                                        prcssMsg = prcssMsg + processes[i].BasePriority.ToString() + "$";
                                    }
                                    catch
                                    {
                                        prcssMsg = prcssMsg + "N/A" + "$";
                                    }
                                    try
                                    {
                                        prcssMsg = prcssMsg + processes[i].Threads.Count + "$";
                                    }
                                    catch
                                    {
                                        prcssMsg = prcssMsg + "N/A" + "$";
                                    }
                                    try
                                    {
                                        prcssMsg = prcssMsg + processes[i].Modules.Count + "$";
                                    }
                                    catch
                                    {
                                        prcssMsg = prcssMsg + "N/A" + "$";
                                    }
                                    try
                                    {
                                        prcssMsg = prcssMsg + processes[i].MainModule.FileName + "^";
                                    }
                                    catch
                                    {
                                        prcssMsg = prcssMsg + "N/A" + "^";
                                    }
                                }
                                Console.WriteLine("The list of the processes was successfully sent to the remote computer.\n");
                                a = Encoding.UTF8.GetBytes(prcssMsg);
                                break;
                            case "START":
                                Console.WriteLine("Starting new process \"" + data[1] + "\" request.");
                                try
                                {
                                    Process.Start(data[1]);
                                    a = Encoding.UTF8.GetBytes("START|OK");
                                    Console.WriteLine("The process " + data[1] + " was successfully started on the server.\n");
                                }
                                catch (Exception ex)
                                {
                                    a = Encoding.UTF8.GetBytes("START|" + ex.Message);
                                    Console.WriteLine("The process " + data[1] + " wasn't successfully started on the server. " + ex.Message + "\n");
                                }
                                break;
                            case "KILL":
                                Console.WriteLine("Killing process ID " + data[1] + " request.");
                                try
                                {
                                    Process.GetProcessById(Convert.ToInt32(data[1])).Kill();
                                    a = Encoding.UTF8.GetBytes("KILL|OK");
                                    Console.WriteLine("The process " + data[1] + " was successfully killed on the server.\n");
                                }
                                catch(Exception ex)
                                {
                                    a = Encoding.UTF8.GetBytes("KILL|"+ex.Message);
                                    Console.WriteLine("The process " + data[1] + " wasn't successfully killed on the server. " + ex.Message + "\n");
                                }
                                break;
                        }
                        BW = new BinaryWriter(NS);
                        BW.Write(a.Length);
                        NS.Write(a, 0, a.Length);
                        BR = new BinaryReader(NS);
                        totalSize = BR.ReadInt32();
                        if (totalSize == 0)
                        {
                            Console.WriteLine("Client received: " + totalSize + " bytes");
                        }
                        MS.SetLength(0);
                    }
                }
                catch (IOException ex)
                {
                    Console.WriteLine("Remote computer failed the connection: " + ex.Message);
                }
            }
        }
    }
}
