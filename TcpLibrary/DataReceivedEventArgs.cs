using System;

namespace TcpLibrary
{
    public class DataReceivedEventArgs : EventArgs
    {
        public ClientConnection Client { get; set; }
        public byte[] Data { get; set; }
    }
}