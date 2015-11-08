using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DispatcherClient
{
    public partial class MyProcesses : Form
    {
        private Thread Thr = new Thread(MyProcesses.UpdateProcessesThreadFunc);
        private TcpClient MyClient = new TcpClient();
        private NetworkStream NS = null;
        private byte[] b = new byte[1024];
        private MemoryStream MS = new MemoryStream();
        private EventWaitHandle EWH = new EventWaitHandle(false, EventResetMode.ManualReset);
        public MyProcesses()
        {
            InitializeComponent();
            Thr.IsBackground = true;
            Thr.Start(this);
        }

        private static void UpdateProcessesThreadFunc(object f)
        {
            while (((MyProcesses)f).EWH.WaitOne())
            {
                byte[] a = Encoding.UTF8.GetBytes("REFRESH|");
                ((MyProcesses)f).Invoke(new Action(delegate() { ((MyProcesses)f).NetworkOperation(a); }));
                Thread.Sleep(2000);
            }
        }

        private void NetworkOperation(byte[] msg)
        {
            lock (NS)
            {
                BinaryWriter BW = new BinaryWriter(NS);
                BW.Write(msg.Length);
                NS.Write(msg, 0, msg.Length);
                BinaryReader BR = new BinaryReader(NS);
                int totalSize = BR.ReadInt32();
                if (totalSize == 0)
                {
                    MessageBox.Show("Failed connection with the server!", "Error", MessageBoxButtons.OK);
                }

                totalSize = 0;
                BR = new BinaryReader(NS);
                int transmitSize = BR.ReadInt32();
                do
                {
                    int cnt = NS.Read(b, 0, b.Length);
                    if (cnt == 0)
                    {
                        MessageBox.Show("Failed connection with the server!", "Error", MessageBoxButtons.OK);
                        EWH.Reset();
                        MyClient.Close();
                        NS.Close();
                        processesLV.Items.Clear();
                    }
                    MS.Write(b, 0, cnt);
                    totalSize += cnt;
                } while (totalSize < transmitSize);
                BW = new BinaryWriter(NS);
                BW.Write(totalSize);
                String answer = Encoding.UTF8.GetString(MS.GetBuffer(), 0, (int)MS.Length);
                MS.SetLength(0);
                string[] data = answer.Split('|');
                switch (data[0])
                {
                    case "REFRESH":
                        lock (this.processesLV)
                        {
                            bool bAct = true;
                            string[] processes = data[1].Split('^');
                            for (int i = 0; i < this.processesLV.Items.Count; i++)
                            {
                                for (int j = 0; j < processes.Length; j++)
                                {
                                    string[] process = processes[j].Split('$');
                                    if (this.processesLV.Items[i].Text == process[0])
                                    {
                                        bAct = false;
                                        break;
                                    }
                                }
                                if (bAct == true)
                                {
                                    this.processesLV.Items.RemoveAt(i);
                                    i--;
                                }
                                bAct = true;
                            }
                            for (int i = 0; i < processes.Length; i++)
                            {
                                string[] process = processes[i].Split('$');
                                for (int j = 0; j < this.processesLV.Items.Count; j++)
                                {
                                    if (this.processesLV.Items[j].Text == process[0])
                                    {
                                        bAct = false;
                                        break;
                                    }
                                }
                                if (bAct == true)
                                {
                                    if (process.Length == 6)
                                    {
                                        ListViewItem lvi = new ListViewItem(process[0]);
                                        lvi.SubItems.Add(process[1]);
                                        lvi.SubItems.Add(process[2]);
                                        lvi.SubItems.Add(process[3]);
                                        lvi.SubItems.Add(process[4]);
                                        lvi.SubItems.Add(process[5]);
                                        processesLV.Items.Add(lvi);
                                        i--;
                                    }
                                }
                                bAct = true;
                            }
                        }
                        break;
                    case "START":
                        switch (data[1])
                        {
                            case "OK":
                                break;
                            default:
                                MessageBox.Show("An error occured: " + data[1], "Error", MessageBoxButtons.OK);
                                break;
                        }
                        break;
                    case "KILL":
                        switch (data[1])
                        {
                            case "OK":
                                break;
                            default:
                                MessageBox.Show("An error occured: " + data[1], "Error", MessageBoxButtons.OK);
                                break;
                        }
                        break;
                }
            }
        }

        private void connectbttn_Click(object sender, EventArgs e)
        {
            if (Thr.IsAlive)
                EWH.Reset();
            MyClient.Close();
            processesLV.Items.Clear();
            try
            {
                MyClient = new TcpClient();
                MyClient.Connect(IPTB.Text, Convert.ToInt32(PORTTB.Text));
                NS = MyClient.GetStream();
                EWH.Set();
            }
            catch (SocketException ex)
            {
                MessageBox.Show("Connection Error: " + ex.Message, "Error", MessageBoxButtons.OK);
            }
            catch (FormatException ex)
            {
                MessageBox.Show("Invalid Port value.", "Error", MessageBoxButtons.OK);
            }
        }

        private void startbttn_Click(object sender, EventArgs e)
        {
            if (MyClient.Connected)
            {
                if (STARTTB.Text != string.Empty)
                {
                    byte[] a = Encoding.UTF8.GetBytes("START|"+STARTTB.Text);
                    NetworkOperation(a);
                }
                else
                    MessageBox.Show("Input the full name of the process to start first!", "Error", MessageBoxButtons.OK);
            }
            else
            {
                MessageBox.Show("Connect to the remote computer first!", "Error", MessageBoxButtons.OK);
            }
        }

        private void killbttn_Click(object sender, EventArgs e)
        {
            if (MyClient.Connected)
            {
                if (KILLTB.Text != string.Empty)
                {
                    try
                    {
                        byte[] a = Encoding.UTF8.GetBytes("KILL|" + Convert.ToInt32(KILLTB.Text));
                        NetworkOperation(a);
                    }
                    catch (FormatException ex)
                    {
                        MessageBox.Show("Invalid ID value.", "Error", MessageBoxButtons.OK);
                    }
                }
                else
                    MessageBox.Show("Input the full name of the process to start first!", "Error", MessageBoxButtons.OK);
            }
            else
            {
                MessageBox.Show("Connect to the remote computer first!", "Error", MessageBoxButtons.OK);
            }
        }
    }
}
