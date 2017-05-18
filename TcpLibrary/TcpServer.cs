using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TcpLibrary
{
    public class TcpServer : TcpBase, IDisposable
    {
        const int defaultPollingRate = 250;
        const int defaultBufferSize = 8192;
        readonly TcpListener _listener;
        readonly List<ClientSocket> _clients;
        Timer _pollingTimer;
        int _pollingRate;
        bool _listening;

        public event EventHandler<ClientConnectionStateChangedEventArgs> ClientConnected;
        public event EventHandler<ClientConnectionStateChangedEventArgs> ClientDisonnected;
        public event EventHandler<DataReceivedEventArgs> DataRceived;
        public event EventHandler<UnhandledExceptionEventArgs> ClientThreadExceptionThrown;

        public TcpServer(IPEndPoint endPoint)
        {
            _listener = new TcpListener(endPoint);
            _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            _clients = new List<ClientSocket>();
            _pollingRate = defaultPollingRate;
            _bufferSize = defaultBufferSize;
        }
        public TcpServer(long ipAddr, int port) : this(new IPEndPoint(ipAddr, port)) {}
        public TcpServer(IPAddress ipAddr, int port) : this(new IPEndPoint(ipAddr, port)) {}

        public override EndPoint EndPoint { get { return _listener.LocalEndpoint; } }
        public ReadOnlyCollection<ClientSocket> Clients { get { lock(_clients) return _clients.AsReadOnly(); } }

        public override bool IsActive => _listening;

        public async Task StartAsync(CancellationToken? token = null)
        {
            if (_listening)
                throw new InvalidOperationException("Server is already running");

            _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token ?? new CancellationToken());
            _token = _tokenSource.Token;
            _listener.Start();
            _listening = true;
            _pollingTimer = new Timer(PollClients, new object(), 0, _pollingRate);

            try
            {
                while (!_token.IsCancellationRequested)
                {   
                    var tcpClient = await _listener.AcceptTcpClientAsync().WithWaitCancellation(_token);
                    var task = StartHandleConnectionAsync(tcpClient);
                }
            }
            catch(OperationCanceledException) { } // server stopped by cancellation token source
            finally
            {
                _listener.Stop();
                _pollingTimer.Dispose();
                _listening = false;
            }
        }

        public void Stop()
        {
            _tokenSource?.Cancel();
        }

        private void PollClients(object state)
        {
            lock(_clients)
                foreach(var client in _clients)
                    if (!client.IsActive)
                        client.Disconnect();
        }

        private async Task StartHandleConnectionAsync(TcpClient acceptedTcpClient)
        {

            var client = new ClientSocket(acceptedTcpClient);
            try
            {
                lock(_clients)
                    _clients.Add(client);
                ClientConnected?.Invoke(this, new ClientConnectionStateChangedEventArgs
                {
                    Client = client
                });
                await HandleConnectionAsync(client);
            }
            catch (Exception ex)
            {
                ClientThreadExceptionThrown?.Invoke(this, new UnhandledExceptionEventArgs
                {
                    ExceptionObject = ex
                });
            }
            finally
            {
                lock(_clients)
                    _clients.Remove(client);
                client.Disconnect();
                ClientDisonnected?.Invoke(this, new ClientConnectionStateChangedEventArgs
                {
                    Client = client
                });                
            }
        }

        private async Task HandleConnectionAsync(ClientSocket client)
        {
            await Task.Yield();
            // continue asynchronously on another threads

            using (var networkStream = client.GetStream())
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(client.DisconnectToken, _token);
                var ct = cts.Token;
                while(client.IsActive && !cts.IsCancellationRequested)
                {
                    var buffer = new byte[_bufferSize];
                    var bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length, ct);
                    if (bytesRead != 0)
                    {
                        DataRceived?.Invoke(this, new DataReceivedEventArgs
                        {
                            Client = client,
                            Data = buffer
                        });
                    }
                }
            }
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
                    ClientConnected = null;
                    ClientDisonnected = null;
                    DataRceived = null;
                    ClientThreadExceptionThrown = null;
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
