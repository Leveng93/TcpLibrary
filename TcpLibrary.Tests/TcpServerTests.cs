using System;
using Xunit;

namespace TcpLibrary.Tests
{
    public class TcpServerTests
    {
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
    }
}
