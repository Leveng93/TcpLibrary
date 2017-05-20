using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace TcpLibrary.Tests
{
    public class TcpServerTests
    {
        const string ip = "127.0.0.1";
        const int port = 8000;

        [Theory]
        [InlineData(-15, 8000)] // IP is less then minimum
        [InlineData(0x0000000FFFFFFFFF, 8000)] // IP is greater then maximum
        [InlineData(0, -1)] // Port is less then minimum
        [InlineData(0, ushort.MaxValue + 1)] // Port is greater then maximum
        public void CreatingServerWithWrongEndPointShouldFail(long ip, int port)
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>{
                var server = new TcpServer(ip, port);
            });
        }

        [Fact]
        public void RunningServerThatIsAlreadyRunningShouldFail()
        {   
            using (var tcpServer = new TcpServer(IPAddress.Parse(ip), port))
            {
                var doubleStartTask = Task.Run(async () => {
                    tcpServer.StartAsync().Wait(100);
                    await tcpServer.StartAsync(); 
                });

                Assert.Throws<InvalidOperationException>(()=>{
                    try
                    {
                        doubleStartTask.Wait();
                    }
                    catch(AggregateException ex)
                    {
                        throw ex.InnerException;
                    }
                });
            }
        }

        [Fact]
        public void RunningServerOnAlreadyUsingPortShouldFail()
        {
            using (var tcpServer = new TcpServer(IPAddress.Parse(ip), port))
            {
                var listener = new TcpListener(IPAddress.Parse(ip), port);
                Assert.Throws<SocketException>(()=>{
                    try 
                    {
                        listener.Start();
                        tcpServer.StartAsync().Wait();
                    }
                    catch(AggregateException ex)
                    {
                        throw ex.InnerException;
                    }
                    finally
                    {
                        tcpServer.Stop();
                        listener.Stop();
                    }
                });
            }
        }

        [Fact]
        public void StoppingServerAwaitingConnections()
        {
            using (var tcpServer = new TcpServer(IPAddress.Parse(ip), port))
            {
                var startTime = DateTime.Now;

                var startTask = tcpServer.StartAsync();
                tcpServer.Stop();
                startTask.Wait();

                Assert.InRange((DateTime.Now - startTime).Milliseconds, 0, 1000);
            }
        }

        [Fact]
        public void ClientShouldSuccessfullyConnect()
        {
            using (var tcpServer = new TcpServer(IPAddress.Parse(ip), port))
            {
                var serverTask = tcpServer.StartAsync();
                var cts = new CancellationTokenSource();
                var ct = cts.Token;
                tcpServer.ClientConnected += (s, e) => cts.Cancel();
                tcpServer.ClientThreadExceptionThrown += (s, e) => throw e.ExceptionObject;

                Assert.Raises<ClientConnectionStateChangedEventArgs>(
                    (handler) => tcpServer.ClientConnected += handler, 
                    (handler) => tcpServer.ClientConnected -= handler, 
                    ()=>{ 
                        using(var client = new TcpClient())
                        {
                            client.ConnectAsync(IPAddress.Parse("127.0.0.1"), 8000).Wait();
                        }
                    }
                );
                try {
                    serverTask.Wait(ct);
                }
                catch (OperationCanceledException) {}                
            }
        }

        [Fact]
        public void ClientShouldSuccessfullyDisconnect()
        {
            using (var tcpServer = new TcpServer(IPAddress.Parse(ip), port))
            using(var client = new TcpClient())
            {
                var serverTask = tcpServer.StartAsync();
                var cts = new CancellationTokenSource();
                var ct = cts.Token;
                tcpServer.ClientDisconnected += (s, e) => cts.Cancel();
                tcpServer.ClientThreadExceptionThrown += (s, e) => throw e.ExceptionObject;

                Assert.Raises<ClientConnectionStateChangedEventArgs>(
                    (handler) => tcpServer.ClientDisconnected += handler, 
                    (handler) => tcpServer.ClientDisconnected -= handler, 
                    ()=>{                         
                        client.ConnectAsync(IPAddress.Parse(ip), port).Wait();
                        client.Client.Shutdown(SocketShutdown.Both);
                        Task.Delay(tcpServer.ClientsPollRate).Wait();
                    }
                );
                try {
                    serverTask.Wait(ct);
                }
                catch (OperationCanceledException) {}
            }
        }
        
        [Fact]
        public void ParallelSendAndReceive()
        {
            using (var tcpServer = new TcpServer(IPAddress.Parse(ip), port))
            using(var client = new TcpClient())
            {                
                var bufferSize = ushort.MaxValue;
                tcpServer.BufferSize = bufferSize;
                client.SendBufferSize = bufferSize;
                client.ReceiveBufferSize = bufferSize;
                var requestStr = String.Concat(Enumerable.Repeat("Ping", 10000));
                var responseStr = String.Concat(Enumerable.Repeat("Pong", 10000));
                var cts = new CancellationTokenSource();
                var ct = cts.Token;
                Task requestTask = null;
                Task responseTask = null;
                tcpServer.ClientConnected += (s, e) => {               
                    requestTask = Task.Run(() => client.Client.Send(Encoding.UTF8.GetBytes(requestStr)), ct);
                    responseTask = e.Client.SendAsync(Encoding.UTF8.GetBytes(responseStr), ct);
                };
                tcpServer.ClientDisconnected += (s, e) => cts.Cancel();
                tcpServer.DataRceived += (s, e) => {
                    Task.WaitAll(requestTask, responseTask);
                    string receivedRequest, receivedResponse;
                    receivedRequest = Encoding.UTF8.GetString(e.Data, 0, e.BytesCount);
                    using (var networkStream = client.GetStream())
                    {
                        var buffer = new byte[bufferSize];
                        var bytesCount = networkStream.Read(buffer, 0, buffer.Length);
                        receivedResponse = Encoding.UTF8.GetString(buffer, 0, bytesCount);
                    }
                    Assert.Equal(requestStr, receivedRequest);
                    Assert.Equal(responseStr, receivedResponse);
                    client.Client.Shutdown(SocketShutdown.Both);
                };
                tcpServer.ClientThreadExceptionThrown += (s, e) => throw e.ExceptionObject;

                var serverTask = tcpServer.StartAsync();
                client.ConnectAsync(IPAddress.Parse(ip), port).Wait(ct);
                try {
                    serverTask.Wait(ct);
                }
                catch (OperationCanceledException) {}
            }
        }

        [Fact]
        public void ManyClientsConnected()
        {
            using (var tcpServer = new TcpServer(IPAddress.Parse(ip), port))
            {   
                var clientsCount = 2000;
                var clientsLeft = clientsCount;
                var requestStr = String.Concat(Enumerable.Repeat("Ping", 1000));
                var responseStr = String.Concat(Enumerable.Repeat("Pong", 1000));
                var cts = new CancellationTokenSource();
                var ct = cts.Token;
                tcpServer.DataRceived += async (s, e) => {
                    var receivedRequest = Encoding.UTF8.GetString(e.Data, 0, e.BytesCount);
                    await e.Client.SendAsync(Encoding.UTF8.GetBytes(responseStr), ct);
                    // Console.WriteLine($"Client {e.Client.EndPoint} sended {receivedRequest}");
                    // Console.WriteLine($"Server sended {responseStr} to {e.Client.EndPoint}");
                    Assert.Equal(requestStr, receivedRequest);
                    e.Client.Disconnect();                    
                };
                tcpServer.ClientDisconnected += (s, e) => {
                    clientsLeft -= 1; 
                    if (clientsLeft == 0)
                        cts.Cancel();
                };
                tcpServer.ClientThreadExceptionThrown += (s, e) => throw e.ExceptionObject;

                var serverTask = tcpServer.StartAsync();
                var clients = new List<TcpClient>(clientsCount);
                for (int i = 0; i < clientsCount; i++)
                {
                    var client = new TcpClient();
                    clients.Add(client);
                    client.ConnectAsync(IPAddress.Parse(ip), port)
                        .ContinueWith((ar) => client.Client.Send(Encoding.UTF8.GetBytes(requestStr)));
                }
                try {
                    serverTask.Wait(ct);
                    foreach(var client in clients)
                        client.Dispose();
                }
                catch (OperationCanceledException) {}
            }
        }
    }
}
