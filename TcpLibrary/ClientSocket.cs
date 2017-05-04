using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TcpLibrary
{
    public class ClientSocket : TcpBase
    {
        const int defaultBufferSize = 8192;
        readonly TcpClient _client;
        public Guid Id { get; }

        public ClientSocket(TcpClient client)
        {
            Id = Guid.NewGuid();
            _client = client;
            _tokenSource = new CancellationTokenSource();
            _token = _tokenSource.Token;
            _bufferSize = defaultBufferSize;
        }

        public override EndPoint EndPoint { get { return _client.Client.RemoteEndPoint; } }

        internal CancellationToken DisconnectToken => _token;

        internal NetworkStream GetStream()
        {
            return _client.GetStream();
        }

        public bool IsConnected => _client.Client.IsConnected();

        public async Task SendAsync(byte[] data)
        {
            if (!this.IsConnected)
            {
                Disconnect();
                return;
            }              

            using (var networkStream = _client.GetStream())
            {
                await networkStream.WriteAsync(data, 0, data.Length, _token);
            }
        }

        public async Task SendAsync(byte[] data, CancellationToken token)
        {
            if (!this.IsConnected)
            {
                Disconnect();
                return;
            }                

            var cts = CancellationTokenSource.CreateLinkedTokenSource(token, _token);
            var ct = _tokenSource.Token;
            using (var networkStream = _client.GetStream())
            {
                await networkStream.WriteAsync(data, 0, data.Length, ct);
            }
        }

        public void Disconnect()
        {
            _tokenSource.Cancel();
            if (_client.Client.Connected)
                _client.Client.Shutdown(SocketShutdown.Both);
        }
    }
}