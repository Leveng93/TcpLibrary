using System;

namespace TcpLibrary
{
    public class ClientConnectionStateChangedEventArgs : EventArgs
    {
        public ClientSocket Client { get; set; }
    }
}