using System.Net.Sockets;
using System.Net;

namespace ConsoleApp2
{
    public class Client
    {
        public IPAddress ipv4Addr { get; set; }
        public TcpClient tcpClient;
        public string nick { get; set; }
        public NetworkStream stream;
        public bool isConnect = true;

    }
}
