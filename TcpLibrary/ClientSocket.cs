using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Authentication;
using System.IO;

namespace TcpLibrary
{
    public class ClientSocket : TcpBase
    {
        readonly TcpClient _client;
        readonly Stream _stream;
        public Guid Id { get; }

        public ClientSocket(TcpClient client, bool encrypt)
        {
            Id = Guid.NewGuid();
            _client = client;
            _tokenSource = new CancellationTokenSource();
            _token = _tokenSource.Token;
            if (encrypt)
                _stream = new SslStream(client.GetStream(), false);
            else
                _stream = _client.GetStream();
        }
        public ClientSocket(TcpClient client) : this(client, false) {}

        internal CancellationToken DisconnectToken => _token;

        internal Stream GetStream()
        {
            return _stream;
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
                _stream.ReadTimeout = value;
                _stream.WriteTimeout = value;
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
            await _stream.WriteAsync(data, 0, data.Length, _token);
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
            await _stream.WriteAsync(data, 0, data.Length, ct);
        }

        public void Disconnect()
        {
            _tokenSource.Cancel();
            if (_client.Client.IsConnected())
                _client.Client.Shutdown(SocketShutdown.Both);
        }
    }
}