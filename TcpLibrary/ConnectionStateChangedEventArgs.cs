using System;

namespace TcpLibrary
{
    public class ClientConnectionStateChangedEventArgs : EventArgs
    {
        public ClientConnection Client { get; set; }
    }
}