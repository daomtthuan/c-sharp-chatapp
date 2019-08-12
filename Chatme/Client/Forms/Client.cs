﻿using System;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraTab;
using DevExpress.XtraTab.ViewInfo;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Net;

namespace Client.Forms
{
    public partial class Client : DevExpress.XtraEditors.XtraForm
    {
        #region Instance variables
        public static readonly string ip = "127.0.0.1";                             // IP
        public static readonly int port = 2019;                                     // Port
        private readonly String name;                                               // Name Client          
        private TcpClient socket;                                                   // TCP/IP protocol for Client
        private NetworkStream stream;                                               // Use NetworkStream for sending and receiving message
        private StreamReader reader;                                                // Use StreamReader for reading message
        private Thread receive;                                                     // Thread Receive message form Server
        private bool connected;                                                     // Is Connected or not
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor Client
        /// </summary>
        /// <param name="username"></param>
        public Client(string username)
        {
            InitializeComponent();

            name = username;
            connected = false;
            receive = null;
        }
        #endregion

        #region Method
        /// <summary>
        /// Method connect to Server
        /// </summary>
        /// <returns></returns>
        private bool Connect()
        {
            try
            {
                socket = new TcpClient(ip, port);
                stream = socket.GetStream();
                reader = new StreamReader(stream);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Register in connected Clients list
        /// </summary>
        private void Login()
        {
            if (connected)
            {
                try
                {
                    string command = "connect|" + name;
                    Byte[] outbytes = Encoding.ASCII.GetBytes(command.ToCharArray());
                    stream.Write(outbytes, 0, outbytes.Length);

                    string[] tokens = reader.ReadLine().Trim().Split(new Char[] { '|' });
                    if (tokens[0] == "list")
                    {
                        for (int i = 1; i < tokens.Length - 1; i++)
                            listboxConnectedClients.Items.Add(tokens[i].Trim(new char[] { '\r', '\n' }));
                    }
                }
                catch
                {
                    connected = false;
                    Close();
                }
            }
            else
            {
                Close();
            }
        }

        /// <summary>
        /// Method Receive message form Server
        /// </summary>
        private void Receive()
        {
            while (connected)
            {
                try
                {
                    Byte[] buffer = new Byte[2048];
                    stream.Read(buffer, 0, buffer.Length);
                    string chatter = Encoding.ASCII.GetString(buffer);

                    string[] tokens = chatter.Split(new Char[] { '|' });

                    if (tokens[0] == "logout")
                    {
                        listboxConnectedClients.Items.Remove(tokens[1].Trim(new char[] { '\r', '\n' }));
                    }
                    if (tokens[0] == "close")
                    {
                        connected = false;
                        Close();
                    }
                }
                catch
                {
                    if (connected)
                    {
                        connected = false;
                        Close();
                    }
                }
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Event click Client in list connected client to open tab chat
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListboxConnectedClients_MouseClick(object sender, MouseEventArgs e)
        {
            int index = listboxConnectedClients.IndexFromPoint(e.Location);
            if (index != -1)
            {
                bool found = false;
                foreach (XtraTabPage tab in tabChat.TabPages)
                {
                    if (tab.Text == listboxConnectedClients.Items[index].ToString())
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    tabChat.TabPages.Add(listboxConnectedClients.Items[index].ToString());
                }
            }
        }

        /// <summary>
        /// Event click close tab chat
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabChat_CloseButtonClick(object sender, EventArgs e)
        {
            tabChat.TabPages.Remove((e as ClosePageButtonEventArgs).Page as XtraTabPage);
        }

        /// <summary>
        /// Event load client form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_Load(object sender, EventArgs e)
        {
            connected = Connect();
            if (connected)
            {
                labelUsername.Text += name;
                Login();
                receive = new Thread(new ThreadStart(Receive));
                receive.Start();
            }
            else Close();
        }

        /// <summary>
        /// Event close client form, Close all
        /// Send message to Server "I logout"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (connected)
            {
                try
                {
                    string command = "logout|" + name;
                    Byte[] outbytes = Encoding.ASCII.GetBytes(command.ToCharArray());
                    stream.Write(outbytes, 0, outbytes.Length);
                }
                catch
                {
                    MessageBox.Show("Could not connect to Server", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
                connected = false;
                if (stream != null) stream.Close();
                if (reader != null) reader.Close();
                if (receive != null) receive.Abort();
                if (socket != null) socket.Close();
            }
            else
            {
                MessageBox.Show("Could not connect to Server", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
        #endregion
    }
}