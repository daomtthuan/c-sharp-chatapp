﻿using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace Server
{
    public partial class Server : XtraForm
    {
        #region Properties
        /// <summary>
        /// Account login server
        /// </summary>
        private static string account;

        /// <summary>
        /// List of clients connected
        /// </summary>
        private List<Client> clients;

        /// <summary>
        /// Is server alive ?
        /// </summary>
        private bool alive;

        /// <summary>
        /// Socket server
        /// </summary>
        private Socket server;

        /// <summary>
        /// Thread listen to client
        /// </summary>
        private Thread listener;
        #endregion

        #region Constructors
        /// <summary>
        /// Server constructor
        /// </summary>
        public Server()
        {
            InitializeComponent();
            Icon = Properties.Resources.ServerIcon;
            account = null;
            clients = new List<Client>();
            alive = false;
        }
        #endregion

        #region Events
        /// <summary>
        /// Event server has shown
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void Server_Shown(object sender, EventArgs e)
        {
            using (Login login = new Login()) { login.ShowDialog(); }
            if (Account == null) Application.Exit();
            else
            {
                Start();
            }
        }

        /// <summary>
        /// Event server is closing
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event Ảgs</param>
        private void Server_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Account != null) Data.Account.Instance.Logout(Account, 1);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Startup server
        /// </summary>
        private void Start()
        {
            IPEndPoint client = new IPEndPoint(IPAddress.Any, Data.Config.Port);
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            alive = true;

            server.Bind(client);
            listener = new Thread(Listen) { IsBackground = true };
            try { listener.Start(); }
            catch
            {
                alive = false;
                listener.Abort();
            }
        }

        /// <summary>
        /// Listen to client
        /// </summary>
        private void Listen()
        {
            while (alive)
            {
                server.Listen(100);
                clients.Add(new Client(server.Accept()));

                MessageBox.Show("Kết nối thành công");
            }
        }
        #endregion

        #region Getter Setter
        /// <summary>
        /// Account login
        /// </summary>
        public static string Account { get => account; set => account = value; }
        #endregion
    }


    /// <summary>
    /// Client class
    /// </summary>
    public class Client
    {
        /// <summary>
        /// account client login
        /// </summary>
        private string account;

        /// <summary>
        /// socket client
        /// </summary>
        private Socket socket;

        /// <summary>
        /// Client constructor
        /// </summary>
        /// <param name="socket">Socket client</param>
        public Client(Socket socket) { account = null; Socket = socket; }

        /// <summary>
        /// Close connect client
        /// </summary>
        public void Close() { Socket.Close(); }

        /// <summary>
        /// Account client login
        /// </summary>
        public string Account { get => account; set => account = value; }

        /// <summary>
        /// Socket client
        /// </summary>
        public Socket Socket { get => socket; set => socket = value; }
    }
}