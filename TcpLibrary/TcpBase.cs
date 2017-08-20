using System;
using System.Net;
using System.Threading;

namespace TcpLibrary
{
    public abstract class TcpBase
    {
        const int defaultTimeout = 5000;
        const int defaultBufferSize = 8192;
        protected CancellationTokenSource _tokenSource;    
        protected CancellationToken _token;
        protected int _timeout;
        protected int _bufferSize;
        public virtual int Timeout 
        {
            get { return _timeout; }
            set
            {
                if (value < -1)
                    throw new ArgumentOutOfRangeException();

                _timeout = value;
            }
        }
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

        public TcpBase()
        {
            _timeout = defaultTimeout;
            _bufferSize = defaultBufferSize;
        }
    }
}