using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace TcpLibrary.Tests
{
    public class ClientSocketTests
    {
        [Fact]
        public void ParallelSendAndReceive()
        {
            using (var tcpServer = new TcpServer(IPAddress.Parse("127.0.0.1"), 8000))
            using(var client = new TcpClient())
            {                
                var requestStr = String.Concat(Enumerable.Repeat("Ping", 500));
                var responseStr = String.Concat(Enumerable.Repeat("Pong", 500));;                
                var serverTask = tcpServer.StartAsync();
                tcpServer.ClientConnected += (s, e) => Console.WriteLine(e.Client.EndPoint.ToString() + " connected");                 
                tcpServer.ClientDisonnected += (s, e) => Console.WriteLine(e.Client.EndPoint.ToString() + " disconnected");                 
                tcpServer.ClientThreadExceptionThrown += (s, e) => Console.WriteLine(e.ExceptionObject.Message);                                 
                client.ConnectAsync(IPAddress.Parse("127.0.0.1"), 8000).Wait();

                var requestTask = Task.Run(() => client.Client.Send(Encoding.UTF8.GetBytes(requestStr)));
                var responseTask = tcpServer.Clients[0].SendAsync(Encoding.UTF8.GetBytes(responseStr));
                Task.WaitAll(requestTask, responseTask);
                
                Console.WriteLine(responseTask.AsyncState.ToString());
            }
        }
    }
}