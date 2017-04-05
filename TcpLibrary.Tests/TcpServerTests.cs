using System;
using System.Net;
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
        public void CtorIncorrectEndPointTest(long ip, int port)
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>{
                var server = new TcpServer(ip, port);
            });
        }

        [Fact]
        public void RunningServerThatIsAlreadyRunning()
        {   
            var server = new TcpServer(IPAddress.Parse(ip), port);
            
            var doubleStartTask = Task.Run(async () => {
                var firstStart = Task.Run(()=> server.StartAsync());
                await Task.Delay(1000);
                var secondStart = Task.Run(()=> server.StartAsync());
        
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
    }
}
