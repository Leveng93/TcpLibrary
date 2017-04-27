using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TcpLibrary
{
    public class TcpServer : IDisposable
    {
        readonly TcpListener _listener;
        readonly List<ClientSocket> _clients;
        bool _listening;
        int _bufferSize;
        CancellationTokenSource _tokenSource;    
        CancellationToken _token;

        public event EventHandler<ClientConnectionStateChangedEventArgs> ClientConnected;
        public event EventHandler<ClientConnectionStateChangedEventArgs> ClientDisonnected;
        public event EventHandler<DataReceivedEventArgs> DataRceived;
        public event EventHandler<UnhandledExceptionEventArgs> ExceptionThrown;

        public TcpServer(IPEndPoint endPoint)
        {
            _listener = new TcpListener(endPoint);
            _clients = new List<ClientSocket>();
        }
        public TcpServer(long ipAddr, int port) : this(new IPEndPoint(ipAddr, port)) {}
        public TcpServer(IPAddress ipAddr, int port) : this(new IPEndPoint(ipAddr, port)) {}

        public EndPoint EndPoint { get { return _listener.LocalEndpoint; } }
        public int BufferSize
        {
            get { return _bufferSize; }
            set
            {
                if (value < 2)
                    throw new ArgumentOutOfRangeException("Buffer size is to small");
                if (value > (2 ^ 32))
                    throw new ArgumentOutOfRangeException("Buffer size is to large");
                
                _bufferSize = value;
            }           
        }

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
                    var tcpClient = await _listener.AcceptTcpClientAsync().WithWaitCancellation(_token);              
                    var task = StartHandleConnectionAsync(tcpClient);
                    // if already faulted, re-throw any error on the calling context
                    if (task.IsFaulted)
                        task.Wait();
                }
            }
            catch(OperationCanceledException) { } // server stopped by cancellation token source
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
                ExceptionThrown?.Invoke(this, new UnhandledExceptionEventArgs
                {
                    ExceptionObject = ex
                });
            }
            finally
            {
                lock(_clients)
                    _clients.Remove(client);
            }
        }

        private async Task HandleConnectionAsync(ClientSocket client)
        {
            await Task.Yield();
            // continue asynchronously on another threads

            using (var networkStream = client.GetStream())
            {
                var buffer = new byte[_bufferSize];
                await networkStream.ReadAsync(buffer, 0, buffer.Length, _token);
                DataRceived?.Invoke(this, new DataReceivedEventArgs
                {
                    Client = client,
                    Data = buffer
                });
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
