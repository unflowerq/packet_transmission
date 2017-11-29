using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using FilePacket;

namespace 소실과제4_Client
{
    public partial class Client : Form
    {
        private NetworkStream m_NetStream;
        private TcpListener m_Listener;

        private byte[] sendBuffer = new byte[1024 * 4];
        private byte[] readBuffer = new byte[1024 * 4];

        private bool m_blsClientOn = false;

        private Thread m_Thread;
        private Thread m_Reader;
        private Thread m_SendToServer;
        private Thread m_prog;

        public NameSize m_NameSize;
        public DataName m_DataName;
        public DataName m_DataName_Copy;
        public DataSend m_DataSend;

        public string Get_MyIP()
        {
            IPHostEntry host = Dns.GetHostByName(Dns.GetHostName());
            string myip = host.AddressList[0].ToString();

            return myip;
        }

        TcpClient client = new TcpClient();

        public Client()
        {
            InitializeComponent();
        }

        public void Send()
        {
            
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void Client_Load(object sender, EventArgs e)
        {
            listView1.View = View.Details;
           
            // listView1.GridLines = true;
            // listView1.FullRowSelect = true;
             //listView1.CheckBoxes = true;
            
             listView1.Columns.Add("Name", 70);
             listView1.Columns.Add("Size", 100);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if(button4.Text == "Connect")
            {
                client.Connect(textBox1.Text, Convert.ToInt32(textBox2.Text));

                m_NetStream = client.GetStream();

                m_Reader = new Thread(new ThreadStart(Receive));
                m_Reader.Start();

                button4.Text = "Disconnect";
                button4.ForeColor = Color.Red;
            }        

            else if(button4.Text == "Disconnect")
            {
                client.Close();
                m_NetStream.Close();
                m_Reader.Abort();
                //m_SendToServer.Abort();

                button4.Text = "Connect";
                button4.ForeColor = Color.Black;
            }
        }

        public void Receive()
        {
            while (true)
            {
                m_NetStream.Read(readBuffer, 0, readBuffer.Length);

                Packet packet = (Packet)Packet.Deserialize(this.readBuffer);

                switch ((int)packet.Type)
                {
                    case (int)PacketType.이름과사이즈:
                        {
                            ListViewItem item;

                            this.m_NameSize = (NameSize)Packet.Deserialize(this.readBuffer);
                            this.Invoke(new MethodInvoker(delegate ()
                            {
                                item = listView1.Items.Add(m_NameSize.name);
                                item.SubItems.Add(m_NameSize.size.ToString());
                            }));
                                   
                            break;
                        }

                    case (int)PacketType.파일이름:
                        {
                            this.m_DataName = (DataName)Packet.Deserialize(this.readBuffer);

                            break;
                        }

                    case (int)PacketType.파일:
                        {
                            this.m_DataSend = (DataSend)Packet.Deserialize(this.readBuffer);                 

                            FileStream fs = new FileStream(textBox3.Text + "\\" + m_DataName.name, FileMode.Append, FileAccess.Write);
                            fs.Write(m_DataSend.data, 0, m_DataSend.data.Length);

                            this.Invoke(new MethodInvoker(delegate ()
                            {
                                progressBar1.Value += (int)(progressBar1.Maximum / ((progressBar1.Maximum / (1024 * 3)) + 1));
                            }));

                            fs.Close();

                            if (m_DataSend.data.Length < 1024 * 3)
                            {
                                this.Invoke(new MethodInvoker(delegate ()
                                {
                                    progressBar1.Value = 0;
                                }));
                            }

                            break;
                        }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.ShowDialog();
            string selected = dialog.SelectedPath;

            textBox3.Text = selected;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string filepath = openFileDialog1.FileName;
                textBox4.Text = filepath;
            }
        }

        private void Server_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.m_Listener.Stop();
            this.m_NetStream.Close();
            this.m_Thread.Abort();
            this.client.Close();
            this.m_SendToServer.Abort();
            this.m_Reader.Abort();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            m_SendToServer = new Thread(new ThreadStart(SendTo));
            m_SendToServer.Start();
        }

        public void Client_Send()
        {
            this.m_NetStream.Write(this.sendBuffer, 0, this.sendBuffer.Length);
            this.m_NetStream.Flush();

            for (int i = 0; i < sendBuffer.Length; i++)
            {
                this.sendBuffer[i] = 0;
            }
        }

        public void SendTo()
        {
            FileStream fs = new FileStream(textBox4.Text, FileMode.Open, FileAccess.Read);
            BinaryReader read = new BinaryReader(fs);
            FileInfo fi = new FileInfo(textBox4.Text);
            int count=(int)(fi.Length / (1024 * 3))+1;
            
            NameSize ns = new NameSize();
            ns.name = fi.Name;
            
            ns.Type = (int)PacketType.이름과사이즈;
            Packet.Serialize(ns).CopyTo(this.sendBuffer, 0);
            this.Client_Send();
            
            progressBar1.Maximum = (int)fi.Length;
            
            for (int i=0; i< count;i++)
            {
                DataSend ds = new DataSend();
                ds.data = read.ReadBytes(1024 * 3);
                ds.Type = (int)PacketType.파일;

                progressBar1.Value += progressBar1.Maximum / count;

                Packet.Serialize(ds).CopyTo(this.sendBuffer, 0);
                this.Client_Send();
            }

            progressBar1.Value = 0;
        }

        private void Client_DoubleClick(object sender, EventArgs e)
        {

        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if(listView1.SelectedItems.Count == 1)
            {
                ListView.SelectedListViewItemCollection items = listView1.SelectedItems;
                ListViewItem nsItem = items[0];
                //ListViewItem szItem = items[1];

                string nameOfItem = nsItem.SubItems[0].Text;
                long sizeOfItem = Convert.ToInt32(nsItem.SubItems[1].Text);

                progressBar1.Maximum = (int)sizeOfItem;

                DoubleClickItem dc = new DoubleClickItem();
                dc.Type = (int)PacketType.더블클릭;
                dc.itemName = nameOfItem;

                Packet.Serialize(dc).CopyTo(this.sendBuffer, 0);
                this.Client_Send();
            }
        }
    }
}
