using System;

namespace TcpLibrary
{
    public class DataReceivedEventArgs : EventArgs
    {
        public ClientSocket Client { get; set; }
        public byte[] Data { get; set; }
        public int BytesCount { get; set; }
    }
}