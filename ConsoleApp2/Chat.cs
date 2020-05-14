using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;


namespace ConsoleApp2
{
    class Chat
    {
        static string nick;
        static int port = 8883;
        static int brPort = 8881;
        static IPAddress ipAddr;
        static List<Client> clients = new List<Client>();
        private bool isConnect = false;
        public NetworkStream stream;
        public TcpClient tcpClient;

        public const byte TMessage = 1;
        public const byte TUserConnect = 2;
        public const byte TUserDisconnected = 3;

        public void ClientConnect()
        {
            ipAddr = GetIPAddress();

            Console.WriteLine("Write your name below:");
            nick = Console.ReadLine();

            Task.Factory.StartNew(() => ListenerUDP());

            ClientRegister();
        }

        private void ClientRegister()
        {
            isConnect = true;
            UdpClient Uclient = new UdpClient("255.255.255.255", brPort);
            byte[] nickmsg = Encoding.Unicode.GetBytes(nick);
            Uclient.EnableBroadcast = true;
            Task.Factory.StartNew(() => ConnectionCatcher());
            try
            {
                for (int i = 0; i < 5; i++)
                {
                    Uclient.Send(nickmsg, nickmsg.Length);
                }
            }
            catch(Exception ex) { }
            finally
            {
                Uclient.Dispose();
            }
        }

        public void ClientSendMessage(string message)
        {
            Console.WriteLine(ShowTime() + "[" + ipAddr.ToString() + "] " +
                "" + nick + ": " + message);
            foreach (Client client in clients)
            {
                var messageBytes = Encoding.Unicode.GetBytes(message);
                var sendMessage = new byte[5 + messageBytes.Length];
                sendMessage[0] = TMessage;
                var byteLengthNick = BitConverter.GetBytes(messageBytes.Length);
                Buffer.BlockCopy(byteLengthNick, 0, sendMessage, 1, 4);
                Buffer.BlockCopy(messageBytes, 0, sendMessage, 5, messageBytes.Length);
                try
                {
                    client.stream.Write(sendMessage, 0, sendMessage.Length);
                }
                catch (Exception ex) { }
            }

        }

        public void ListenerUDP()
        {
            var udpCatcher = new UdpClient(brPort);
            udpCatcher.EnableBroadcast = true;
            while (true)
            {
                IPEndPoint hostremote = null;
                var data = udpCatcher.Receive(ref hostremote);
                if (isConnect)
                {
                    var client = new Client();
                    client.ipv4Addr = hostremote.Address;
                    client.nick = Encoding.Unicode.GetString(data);
                    if (client.ipv4Addr != ipAddr)
                    {
                        bool presence = false;
                        foreach (var temp in clients)
                        {
                            if (temp.ipv4Addr.Equals(client.ipv4Addr))
                            {
                                presence = true;
                                break;
                            }
                        }
                        if (presence == false)
                        {
                            try
                            {
                                tcpClient = new TcpClient();
                                tcpClient.Connect(new IPEndPoint(client.ipv4Addr, port));
                                stream = tcpClient.GetStream();
                                client.stream = tcpClient.GetStream();
                                var NicknameBytes = Encoding.Unicode.GetBytes(nick);
                                var ConnectMessage = new byte[5 + NicknameBytes.Length];
                                ConnectMessage[0] = TUserConnect;
                                var ByteLengthNick = BitConverter.GetBytes(NicknameBytes.Length);
                                Buffer.BlockCopy(ByteLengthNick, 0, ConnectMessage, 1, 4);
                                Buffer.BlockCopy(NicknameBytes, 0, ConnectMessage, 5, NicknameBytes.Length);
                                stream.Write(ConnectMessage, 0, ConnectMessage.Length);
                                client.tcpClient = tcpClient;
                                clients.Add(client);

                                Console.WriteLine(ShowTime() + client.nick + " connected");
                                Task.Factory.StartNew(() => ListenerTCP(clients[clients.IndexOf(client)]));
                            }
                            catch (Exception ex) { }
                        }
                    }
                }
            }
        }

        public void ConnectionCatcher()
        {
            TcpListener tcpListener = new TcpListener(ipAddr, port);
            tcpListener.Start();
            while (isConnect)
            {
                if (tcpListener.Pending())
                {
                    try
                    {
                        var client = new Client();
                        client.tcpClient = tcpListener.AcceptTcpClient();
                        client.ipv4Addr = ((IPEndPoint)client.tcpClient.Client.RemoteEndPoint).Address;
                        client.stream = client.tcpClient.GetStream();
                        byte[] data = new byte[5];
                        client.stream.Read(data, 0, 5);
                        int length = BitConverter.ToInt32(data, 1);
                        byte[] message = new byte[length];
                        client.stream.Read(message, 0, length);
                        string messagetxt = Encoding.Unicode.GetString(message);
                        client.nick = messagetxt;
                        clients.Add(client);
                        if (data[0] == TUserConnect)
                        {
                            Console.WriteLine(ShowTime() + "You connect to the chat");
                            Task.Factory.StartNew(() => ListenerTCP(clients[clients.IndexOf(client)]));
                        }
                    }
                    catch (Exception ex) { }
                   
                }
            }
            tcpListener.Stop();
        }

        public void ListenerTCP(Client client)
        {
            while (client.isConnect)
            {
                if (client.stream.DataAvailable)
                {
                    byte[] data = new byte[5];
                    try
                    {
                        client.stream.Read(data, 0, 5);
                    }
                    catch (Exception ex) { }
                    byte Type = data[0];
                    int Length = BitConverter.ToInt32(data, 1);
                    switch (Type)
                    {
                        case TMessage:
                            try
                            {
                                byte[] msg = new byte[Length];
                                client.stream.Read(msg, 0, Length);
                                string msgtxt = Encoding.Unicode.GetString(msg);
                                Console.WriteLine(ShowTime() + "[" + client.ipv4Addr + "] " + client.nick + ": " + msgtxt);
                            }
                            catch (Exception ex) { }
                            break;
                        case TUserDisconnected:
                            client.isConnect = false;
                            Console.WriteLine(ShowTime() + client.nick + " disconnected");

                            client.stream.Close();
                            client.stream.Dispose();
                            client.tcpClient.Close();
                            client.tcpClient.Dispose();
                            clients.Remove(client);
                            break;
                    }
                }
            }

        }


        public void ClientDisconnect()
        {
            isConnect = false;
            Console.WriteLine("You've disconnected from the chat");
            foreach (var client in clients)
            {
                try
                {
                    var data = new byte[5];
                    data[0] = TUserDisconnected;
                    int length = 0;
                    var lengthBytes = BitConverter.GetBytes(length);
                    Buffer.BlockCopy(lengthBytes, 0, data, 1, 4);
                    client.stream.Write(data, 0, data.Length);
                    client.isConnect = false;
                    client.stream.Close();
                    client.stream.Dispose();
                    client.tcpClient.Close();
                    client.tcpClient.Dispose();
                }
                catch (Exception ex) { }
            }
            clients.Clear();
        }

        IPAddress GetIPAddress()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress tempIp in host.AddressList)
            {
                if (tempIp.AddressFamily == AddressFamily.InterNetwork)
                {
                    return tempIp;
                }
            }
            return null;
        }

        string ShowTime()
        {
            return "[" + DateTime.Now.ToString("HH:mm:ss") + "] ";
        }
    }

}
