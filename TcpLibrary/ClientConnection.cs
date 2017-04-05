using System;
using System.Net.Sockets;

namespace TcpLibrary
{
    public class ClientConnection
    {
        readonly TcpClient _client;
        public Guid Id { get; }

        public ClientConnection(TcpClient client)
        {
            Id = new Guid();
            _client = client;
        }
    }
}