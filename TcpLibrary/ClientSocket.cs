using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TcpLibrary
{
    public class ClientSocket : TcpBase, IDisposable
    {
        readonly TcpClient _client;
        public Guid Id { get; }

        public ClientSocket(TcpClient client)
        {
            Id = new Guid();
            _client = client;
            _tokenSource = new CancellationTokenSource();
            _token = _tokenSource.Token;
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
            using (var networkStream = _client.GetStream())
            {
                await networkStream.WriteAsync(data, 0, data.Length, _token);
            }
        }

        public async Task SendAsync(byte[] data, CancellationToken token)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(token, _token);
            var ct = _tokenSource.Token;
            using (var networkStream = _client.GetStream())
            {
                await networkStream.WriteAsync(data, 0, data.Length, ct);
            }
        }

        public void Disconnect()
        {
            _tokenSource?.Cancel();
            _client.Client.Shutdown(SocketShutdown.Both);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Disconnect();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ClientSocket() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}