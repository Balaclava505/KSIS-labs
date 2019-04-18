using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UdpBroadcast
{
    class p2p
    {
        const int PORT = 9876;
        const int TCPMessagePort = 8888;
        const int TCPHistoryPort = 13000;

        private TcpListener tcpListener { get; set; }
        private TcpListener HistListener { get; set; }
        private UdpClient udpClient;
        private string ClientName;
        private IPAddress broadcast;
        private IPEndPoint toPeer;
        private bool firstMes = true;

        private List<UdpUser> ConnectedUser { get; set; }
        public List<string> History;
        public p2p(string name)
        {
            udpClient = new UdpClient();
            ClientName = name;
            ConnectedUser = new List<UdpUser>();
            History = new List<string>();
            broadcast = IPAddress.Parse("192.168.43.255");
            toPeer = new IPEndPoint(broadcast, PORT);
        }

        public void SendMessage()
        {
            byte[] buffer = Encoding.UTF8.GetBytes(ClientName);
            udpClient.Send(buffer, buffer.Length, toPeer);

        }

        public void Listen()
        {
            UdpClient client = new UdpClient();
            client.ExclusiveAddressUse = false; 
            client.Client.Bind(new IPEndPoint(IPAddress.Any, PORT));

            string encodeData;
            IPEndPoint fromPeer = new IPEndPoint(0, 0);
            Task.Run(() =>
            {
                while (true)
                {
                    int Number = 0;
                    byte[] recvBuffer = client.Receive(ref fromPeer);
                    if (ConnectedUser.Find(x => x.ipAddress.ToString() == fromPeer.Address.ToString()) == null)
                    {
                        encodeData = Encoding.UTF8.GetString(recvBuffer);
                        if (encodeData != ClientName)
                        {
                            ConnectedUser.Add(new UdpUser()
                            {
                                chatConnection = null,
                                username = encodeData,
                                ipAddress = fromPeer.Address
                            });
                        }
                        Console.WriteLine("User " + ConnectedUser[Number].username + " Connected");
                        firstMes = false;
                        History.Add("User " + ConnectedUser[Number].username + " Connected" + " ");
                        Number = ConnectedUser.FindIndex(x => x.ipAddress.ToString() == fromPeer.Address.ToString());
                        InitTCP(Number);
                    }
                }
            });

        }

        private void InitTCP(int index)
        {
            var newtcpConnect = new TcpClient();
            newtcpConnect.Connect(new IPEndPoint(ConnectedUser[index].ipAddress, TCPMessagePort)); 
            ConnectedUser[index].chatConnection = newtcpConnect;
            Thread tcpReceive = new Thread(() => TcpReceiveMessage(newtcpConnect, ConnectedUser[index].username));
            tcpReceive.Start();
            Thread tcpSend = new Thread(() => BroadcastMessage(newtcpConnect, ConnectedUser[index].username));
            tcpSend.Start();
        }

        public void TCPListen()
        {
            tcpListener = new TcpListener(IPAddress.Any, TCPMessagePort);
            tcpListener.Start();

            while (true)
            {
                TcpClient tcpClient = tcpListener.AcceptTcpClient();
                IPAddress remoteAddress = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address;
                string Name;
                if (ConnectedUser.FindIndex(x => x.ipAddress.ToString() == remoteAddress.ToString()) == -1)
                {
                    ConnectedUser.Add(new UdpUser()
                    {
                        chatConnection = tcpClient,
                        username = "",
                        ipAddress = remoteAddress
                    });
                    Name = "";
                }
                else
                {
                    Name = ConnectedUser.Find(x => x.ipAddress.ToString() == remoteAddress.ToString()).username;
                }

                Thread tcpReceive = new Thread(() => TcpReceiveMessage(tcpClient, Name));
                tcpReceive.Start();
                Thread tcpSend = new Thread(() => BroadcastMessage(tcpClient, Name));
                tcpSend.Start();
            }
        }

        public void HistoryListen()
        {
            HistListener = new TcpListener(IPAddress.Any, TCPHistoryPort);
            HistListener.Start();
            while (true)
            {
                var newClient = HistListener.AcceptTcpClient();

                var localStream = new MemoryStream();
                var writer = new StreamWriter(localStream);
                foreach (var line in History)
                {
                    writer.WriteLine(line);
                }

                writer.Flush();
                localStream.Seek(0, SeekOrigin.Begin);
                localStream.CopyTo(newClient.GetStream());
                newClient.Close();
            }
        }

        public void RecvHistory()
        {
            if (ConnectedUser.Count == 0)
            {
                return;
            } 
            TcpClient historyClient = new TcpClient();
            try
            {
                historyClient.Connect(new IPEndPoint(ConnectedUser[0].ipAddress, TCPHistoryPort));

                var connectionStream = historyClient.GetStream();
                var hist = new StreamReader(connectionStream);
                while (true)
                {
                    string line;
                    if ((line = hist.ReadLine()) != null)
                    {
                        History.Add(line);
                    }
                    else
                        return;
                }
            }
            catch
            {
                return;
            }
        }

        private void TcpReceiveMessage(TcpClient connection, string username)
        {
            NetworkStream stream = connection.GetStream();
            StreamReader reader = new StreamReader(stream);
            try
            {
                while (true)
                {                    
                    string message = reader.ReadLine();
                    string[] info = message.Split(':');
                    if (username == "")
                    {
                        username = info[0];
                        ConnectedUser[(ConnectedUser.FindIndex(x => x.ipAddress.ToString() == ((IPEndPoint)connection.Client.RemoteEndPoint).Address.ToString()))].username = info[0];
                    }
                    Console.WriteLine(message);
                    string date = DateTime.Now.ToLongTimeString();
                    Console.WriteLine(date + "\n");
                    History.Add(username + ": " + info[1] + " " + date + "\n");
                }
            }
            catch
            {
                Console.WriteLine(username + " left the chat."); 
                History.Add(username + " left the chat.");
                var address = ((IPEndPoint)connection.Client.RemoteEndPoint).Address;
                ConnectedUser.RemoveAll(X => X.ipAddress.ToString() == address.ToString());
                Console.WriteLine(address);
                if (stream != null)
                    stream.Close();
                reader.Close();
                if (connection != null)
                    connection.Close();

            }

        }

        
        protected internal void BroadcastMessage(TcpClient connection, string username)
        {
            NetworkStream stream = connection.GetStream();
            StreamWriter writer = new StreamWriter(stream);
            
            try
            {
                string data;
                while (true)
                {
                    if (firstMes)
                    {
                        firstMes = false;
                        RecvHistory();
                        Thread.Sleep(1000);
                        if (History.Count != 0)
                        {
                            Console.WriteLine("-----------------------------------------------");
                            foreach (var text in History)
                            {
                                Console.WriteLine(text);
                            }
                            Console.WriteLine("-----------------------------------------------");
                        }
                    } 
                    else
                    {
                        data = Console.ReadLine();
                        string date = DateTime.Now.ToLongTimeString();
                        Console.WriteLine(date);
                        Console.WriteLine();
                        string message = ClientName + " :" + data;
                        History.Add(ClientName + " :" + data + " " + date);
                        ConnectedUser.ForEach(client =>
                        {
                            writer.WriteLine(message);
                        });
                        writer.Flush();
                    }
                }
            }
            catch
            {
                writer.Close();
                stream.Close();
            }
        }
    }
}
