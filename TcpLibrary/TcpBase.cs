using System;
using System.Net;
using System.Threading;

namespace TcpLibrary
{
    public abstract class TcpBase
    {
        protected CancellationTokenSource _tokenSource;    
        protected CancellationToken _token;
        protected int _bufferSize;
        public virtual int BufferSize 
        {
            get { return _bufferSize; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("Buffer size is too small");
                if (value > UInt16.MaxValue)
                    throw new ArgumentOutOfRangeException("Buffer size is too large");
                
                _bufferSize = value;
            }
        }

        public abstract EndPoint EndPoint { get; }
        public abstract bool IsActive { get; }
    }
}