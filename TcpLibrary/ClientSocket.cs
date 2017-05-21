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
            Id = Guid.NewGuid();
            _client = client;
            _client.Client.ReceiveTimeout = _timeout;
            _client.Client.SendTimeout = _timeout;
            _tokenSource = new CancellationTokenSource();
            _token = _tokenSource.Token;
        }
        public ClientSocket(TcpClient client, int timeout)
        {
            Id = Guid.NewGuid();
            _timeout = timeout;
            _client = client;
            _client.Client.ReceiveTimeout = _timeout;
            _client.Client.SendTimeout = _timeout;
            _tokenSource = new CancellationTokenSource();
            _token = _tokenSource.Token;
        }

        internal CancellationToken DisconnectToken => _token;

        internal NetworkStream GetStream()
        {
            return _client.GetStream();
        }

        public override EndPoint EndPoint { get { return _client.Client.RemoteEndPoint; } }

        public override int Timeout
        {
            get { return _timeout; }
            set 
            {
                if (value < -1)
                    throw new ArgumentOutOfRangeException();

                _timeout = value;
                _client.Client.SendTimeout = value;
                _client.Client.ReceiveTimeout = value;
            }
        }

        public override bool IsActive => _client.Client.IsConnected();

        public async Task SendAsync(byte[] data)
        {
            if (!this.IsActive)
            {
                Disconnect();
                return;
            }
            await _client.GetStream().WriteAsync(data, 0, data.Length, _token);
        }

        public async Task SendAsync(byte[] data, CancellationToken token)
        {
            if (!this.IsActive)
            {
                Disconnect();
                return;
            }
            var cts = CancellationTokenSource.CreateLinkedTokenSource(token, _token);
            var ct = _tokenSource.Token;
            await _client.GetStream().WriteAsync(data, 0, data.Length, ct);
        }

        public void Disconnect()
        {
            _tokenSource.Cancel();
            if (_client.Client.Connected)
                _client.Client.Shutdown(SocketShutdown.Both);
        }
    }
}