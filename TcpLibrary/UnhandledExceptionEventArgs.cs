using System;

namespace TcpLibrary
{
    public class UnhandledExceptionEventArgs : EventArgs
    {
        public Exception ExceptionObject { get; set; }
    }
}