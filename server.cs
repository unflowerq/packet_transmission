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
using System.Diagnostics;
using FilePacket;

namespace 소실과제4
{
    public partial class Server : Form
    {
        private NetworkStream m_NetStream;
        private TcpListener m_Listener;

        private byte[] sendBuffer = new byte[1024 * 4];
        private byte[] readBuffer = new byte[1024 * 4];

        private bool m_blsClientOn = false;

        private Thread m_DataSendT;
        private Thread m_Thread;

        public NameSize m_NameSize;

        public DataSend m_DataSend;

        //textBox3 초기화 해야됨 탐색기에서 드라이브 가져오는거 처럼 하는건가?

        //IP주소 가져오는 함수
        public string Get_MyIP()
        {
            IPHostEntry host = Dns.GetHostByName(Dns.GetHostName());
            string myip = host.AddressList[0].ToString();

            return myip;
        }

        public void RUN()
        {
                try {
                    this.m_Listener = new TcpListener(Convert.ToInt32(textBox2.Text));
                    this.m_Listener.Start();
                }
                
                catch
                {
                    MessageBox.Show("포트번호를 입력하세요");
                }

            if (!this.m_blsClientOn)
            {
                this.Invoke(new MethodInvoker(delegate ()
                {
                    this.textBox4.AppendText("Server : Start \r\n");
                    this.textBox4.AppendText("Storage Path : " + textBox3.Text);
                    this.textBox4.AppendText("\r\nwaiting for client access...");
                }));
                
            }

            Socket Client = this.m_Listener.AcceptSocket();
            
            if (Client.Connected)
            {
                this.m_blsClientOn = true;
                this.m_NetStream = new NetworkStream(Client);

                this.Invoke(new MethodInvoker(delegate ()
                {
                    this.textBox4.AppendText("\r\nClient is connected");
                }));

                m_DataSendT = new Thread(new ThreadStart(Receive));
                m_DataSendT.Start();

                NameSize ns = new NameSize();

                DirectoryInfo di;
                di = new DirectoryInfo(textBox3.Text);
                FileInfo[] fiArray;

                fiArray = di.GetFiles();

                foreach(FileInfo fis in fiArray)
                {
                    ns.name = fis.Name;
                    ns.size = fis.Length;

                    Packet.Serialize(ns).CopyTo(this.sendBuffer, 0);
                    this.Send();
                }
            }
            else if(!Client.Connected)
            {
                this.m_Listener.Stop();
                this.m_Thread.Abort();
                this.m_NetStream.Close();
            }
        }

        public Server()
        {
            InitializeComponent();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(button2.Text == "Start")
            {
                this.m_Thread = new Thread(new ThreadStart(RUN));
                this.m_Thread.Start();

                textBox3.Enabled = false;
                button1.Enabled = false;

                button2.Text = "Stop";
                button2.ForeColor = Color.Red;
            }

            else if(button2.Text == "Stop")
            {
                //서버 멈춰있으면 Start로 바꾸고 색상 검은색으로
                this.m_Listener.Stop();
                this.m_Thread.Abort();

                button1.Enabled = true;
                textBox3.Enabled = true;

                button2.Text = "Start";
                button2.ForeColor = Color.Black;
            }
        }

        private void Server_Load(object sender, EventArgs e)
        {
            //IP주소 가져오기
            this.textBox1.AppendText(Get_MyIP());

            //드라이브 리스트 가져오기
            string[] Drv_list;
            TreeNode root;
            Drv_list = Environment.GetLogicalDrives();

            foreach(string Drv in Drv_list)
            {
                textBox3.AppendText(Drv);
            }
        }

        private void Server_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.m_Listener.Stop();
            this.m_NetStream.Close();
            this.m_Thread.Abort();
            this.m_DataSendT.Abort();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.ShowDialog();
            string selected = dialog.SelectedPath;
            
            textBox3.Clear();
            textBox3.AppendText(selected);
        }

        public void Send()
        {
            this.m_NetStream.Write(this.sendBuffer, 0, this.sendBuffer.Length);
            this.m_NetStream.Flush();

            for(int i = 0; i < sendBuffer.Length; i++)
            {
                this.sendBuffer[i] = 0;
            }
        }

        public void Receive()
        {
            string fileName = null;
            string itemName = null;
            string itemName_Copy = null;

            while (true)
            {
                m_NetStream.Read(readBuffer, 0, readBuffer.Length);

                Packet packet = (Packet)Packet.Deserialize(this.readBuffer);
                NameSize m_Name;
                DoubleClickItem m_ItemName;

                switch ((int)packet.Type)
                {
                    case (int)PacketType.이름과사이즈:
                        {
                            m_Name = (NameSize)Packet.Deserialize(this.readBuffer);
                            fileName = m_Name.name;

                            break;
                        }
                    
                    case (int)PacketType.파일:
                        {
                            this.m_DataSend = (DataSend)Packet.Deserialize(this.readBuffer);
                            
                            FileStream fs = new FileStream(textBox3.Text+"\\"+fileName, FileMode.Append, FileAccess.Write);
                            fs.Write(m_DataSend.data, 0, m_DataSend.data.Length);

                            fs.Close();
                            
                            break;
                        }

                    case (int)PacketType.더블클릭:
                        {
                            m_ItemName = (DoubleClickItem)Packet.Deserialize(this.readBuffer);
                            itemName = m_ItemName.itemName;

                            /*if(itemName == itemName_Copy)
                            {
                                MessageBox.Show("같은 파일이 있습니다.");
                                break;
                            }

                            itemName_Copy = itemName;*/
                            
                            FileStream fs = new FileStream(textBox3.Text + "\\" + itemName, FileMode.Open, FileAccess.Read);
                            BinaryReader read = new BinaryReader(fs);
                            FileInfo fi = new FileInfo(textBox3.Text + "\\" + itemName);
                            int count = (int)(fi.Length / (1024 * 3)) + 1;
                            
                            DataName dsn = new DataName();
                            dsn.name = fi.Name;

                            dsn.Type = (int)PacketType.파일이름;
                            Packet.Serialize(dsn).CopyTo(this.sendBuffer, 0);
                            this.Send();

                            for (int i = 0; i < count; i++)
                            {
                                DataSend ds = new DataSend();
                                ds.data = read.ReadBytes(1024 * 3);
                                ds.Type = (int)PacketType.파일;

                                Packet.Serialize(ds).CopyTo(this.sendBuffer, 0);
                                this.Send();
                            }

                            fs.Close(); ;

                            break;
                        }
                }
            }
        }
    }
}
