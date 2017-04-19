using System;
using System.Net.Sockets;

namespace TcpLibrary
{
    public class ClientSocket
    {
        readonly TcpClient _client;
        public Guid Id { get; }

        public ClientSocket(TcpClient client)
        {
            Id = new Guid();
            _client = client;
        }

        public NetworkStream GetStream()
        {
            return _client.GetStream();
        }
    }
}