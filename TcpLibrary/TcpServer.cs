using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TcpLibrary
{
    public class TcpServer : IDisposable
    {
        readonly TcpListener _listener;
        readonly BlockingCollection<ClientConnection> _clients;
        bool _listening;
        CancellationTokenSource _tokenSource;    
        CancellationToken _token;

        public event EventHandler<ClientConnectionStateChangedEventArgs> ClientConnected;
        public event EventHandler<ClientConnectionStateChangedEventArgs> ClientDisonnected;
        public event EventHandler<DataReceivedEventArgs> DataRceived;


        public TcpServer(IPEndPoint endPoint)
        {
            _listener = new TcpListener(endPoint);
        }
        public TcpServer(long ipAddr, int port) : this(new IPEndPoint(ipAddr, port)) {}
        public TcpServer(IPAddress ipAddr, int port) : this(new IPEndPoint(ipAddr, port)) {}

        public EndPoint EndPoint { get { return _listener.LocalEndpoint; } }


        public bool Listening => _listening;
        public async Task StartAsync(CancellationToken? token = null)
        {
            if (_listening)
                throw new InvalidOperationException("Server is already running");

            _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token ?? new CancellationToken());
            _token = _tokenSource.Token;
            _listener.Start();
            _listening = true;
            
            try
            {
                while (!_token.IsCancellationRequested)
                {   
                    await Task.Run(async () =>
                    {
                        var tcpClient = await _listener.AcceptTcpClientAsync();
                        var clientConnection = new ClientConnection(tcpClient);
                        _clients.Add(clientConnection);

                        var eventArgs = new ClientConnectionStateChangedEventArgs
                        {
                            Client = clientConnection
                        };                    
                        ClientConnected?.Invoke(this, eventArgs);
                    }, _token);
                }
            }
            finally
            {
                _listener.Stop();
                _listening = false;
            }
        }

        public void Stop()
        {
            _tokenSource?.Cancel();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Stop();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~TcpServer() {
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
