using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TcpLibrary
{
    public class ClientSocket : TcpBase
    {
        readonly TcpClient _client;
        public Guid Id { get; }

        public ClientSocket(TcpClient client)
        {
            Id = new Guid();
            _client = client;
        }

        public override EndPoint EndPoint { get { return _client.Client.RemoteEndPoint; } }

        internal NetworkStream GetStream()
        {
            return _client.GetStream();
        }

        public async Task SendAsync(byte[] data)
        {
            using (var networkStream = _client.GetStream())
            {
                await networkStream.WriteAsync(data, 0, data.Length);
            }
        }

        public async Task SendAsync(byte[] data, CancellationToken? token)
        {
            _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token ?? new CancellationToken());
            _token = _tokenSource.Token;
            using (var networkStream = _client.GetStream())
            {
                await networkStream.WriteAsync(data, 0, data.Length, _token);
            }
        }

        public void Disconnect()
        {
            _tokenSource?.Cancel();
            _client.Client.Shutdown(SocketShutdown.Both);
        }
    }
}