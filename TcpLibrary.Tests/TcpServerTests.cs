using System;
using System.Net;
using System.Net.Sockets;
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
            var server = new TcpServer(IPAddress.Parse(ip), port);
            
            var doubleStartTask = Task.Run(async () => {
                var firstStart = server.StartAsync();
                await Task.Delay(1000);
                var secondStart = server.StartAsync();
        
                await secondStart;    
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

        [Fact]
        public void RunningServerOnAlreadyUsingPortShouldFail()
        {
            var endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8000);
            var listener = new TcpListener(endPoint);
            var tcpServer = new TcpServer(endPoint);
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

        [Fact]
        public void StoppingServerShouldNotHaveDelay()
        {
            var tcpServer = new TcpServer(IPAddress.Parse("127.0.0.1"), 8000);
            try 
            {
                var startTask = tcpServer.StartAsync();
                tcpServer.Stop();
                startTask.Wait();
            }
            finally
            {
                tcpServer.Stop();
            }
        }
    }
}
