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
        /// List Clients
        /// </summary>
        private List<Client> clients;

        /// <summary>
        /// Is server alive ?
        /// </summary>
        private bool alive;

        /// <summary>
        /// Socket server
        /// </summary>
        private Socket socketServer;
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
            Clients = new List<Client>();
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
                boxCmd.Items.Add("Hello " + account + ", wellcome to Chatapp!");
                boxCmd.Items.Add("Preparing to start server...");
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

        /// <summary>
        /// Event click button Disconnect
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event Args</param>
        private void ButtonDisconnect_Click(object sender, EventArgs e)
        {
            clients.ForEach(client => Send(client, "disconnect|" + Clients[boxClients.SelectedIndex].Account));
        }

        /// <summary>
        /// Event Selected index changed boxClients
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event Args</param>
        private void BoxClients_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (boxClients.SelectedIndex >= 0) buttonDisconnect.Enabled = true;
            else buttonDisconnect.Enabled = false;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Startup server
        /// </summary>
        private void Start()
        {
            IPEndPoint client = new IPEndPoint(IPAddress.Any, Data.Config.Port);
            SocketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            alive = true;

            SocketServer.Bind(client);
            Thread listener = new Thread(Listen) { IsBackground = true };
            try
            {
                boxCmd.Items.Add("Listening...");
                listener.Start();
            }
            catch
            {
                alive = false;
                listener.Abort();
                Application.Exit();
            }
        }

        /// <summary>
        /// Listen to client
        /// </summary>
        private void Listen()
        {
            while (alive)
            {
                SocketServer.Listen(100);
                Client client = new Client(SocketServer.Accept());
                Clients.Add(client);
                boxCmd.Items.Add(client + " : Request to connect");

                Thread servicer = new Thread(() => Receive(client)) { IsBackground = true };
                servicer.Start();
            }
        }

        /// <summary>
        /// Receive message from client
        /// </summary>
        /// <param name="client">Receive from which client?</param>
        private void Receive(Client client)
        {
            try
            {
                while (alive)
                {
                    byte[] data = new byte[5120];
                    client.Socket.Receive(data);
                    string message = Data.Message.Deserialize(data);
                    boxMess.Items.Add(client.Socket.RemoteEndPoint + " --> " + message.Trim('\0'));

                    Analyze(client, message);
                }
            }
            catch
            {
                boxCmd.Items.Add(client + " : Disconnect");
                Clients.Remove(client);
                Clients.ForEach(e => Send(e, "disconnect|" + client.Account));
                boxClients.Items.Remove(client);
                client.Close();
            }
        }

        /// <summary>
        /// Send message
        /// </summary>
        /// <param name="client">Send to which client?</param>
        /// <param name="message">Message</param>
        private void Send(Client client, string message)
        {
            try
            {
                if (alive)
                {
                    client.Socket.Send(Data.Message.Serialize(message));
                    boxMess.Items.Add(client.Socket.RemoteEndPoint + " <-- " + message);
                }
            }
            catch
            {
                boxCmd.Items.Add(client + " : Disconnect");
                Clients.Remove(client);
                Clients.ForEach(e => Send(e, "disconnect|" + client.Account));
                boxClients.Items.Remove(client);
                client.Close();
            }
        }

        /// <summary>
        /// Analyze message
        /// </summary>
        /// <param name="client">Receive from which client</param>
        /// <param name="message">Message</param>
        private void Analyze(Client client, string message)
        {
            string[] tokens = message.Trim('\0').Split('|');
            switch (tokens[0])
            {
                case "connect":
                    Send(client, "list" + GetClients());
                    client.Account = tokens[1];
                    boxClients.Items.Add(client);
                    boxCmd.Items.Add(client + " : Accept and send list clients");
                    break;
            }
        }

        /// <summary>
        /// Return list clients to string
        /// </summary>
        /// <returns></returns>
        private string GetClients()
        {
            string result = "";
            Clients.ForEach(client => result += "|" + client.Account);
            return result;
        }
        #endregion

        #region Getter Setter
        /// <summary>
        /// Account login
        /// </summary>
        public static string Account { get => account; set => account = value; }

        /// <summary>
        /// Socket server
        /// </summary>
        public Socket SocketServer { get => socketServer; set => socketServer = value; }

        /// <summary>
        /// List Clients
        /// </summary>
        public List<Client> Clients { get => clients; set => clients = value; }
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
        /// Return string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Socket.RemoteEndPoint.ToString() + ((Account != null) ? (" - " + Account) : "");
        }

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
