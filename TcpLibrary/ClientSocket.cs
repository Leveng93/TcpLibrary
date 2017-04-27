using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TcpLibrary
{
    public class ClientSocket
    {
        readonly TcpClient _client;
        public Guid Id { get; }
        int _bufferSize;
        public int BufferSize
        {
            get { return _bufferSize; }
            set
            {
                if (value < 2)
                    throw new ArgumentOutOfRangeException("Buffer size is too small");
                if (value > (2 ^ 32))
                    throw new ArgumentOutOfRangeException("Buffer size is too large");
                
                _bufferSize = value;
            }           
        }

        public ClientSocket(TcpClient client)
        {
            Id = new Guid();
            _client = client;
        }

        internal NetworkStream GetStream()
        {
            return _client.GetStream();
        }

        public async Task SendAsync(byte[] data)
        {
            using (var networkStream = _client.GetStream())
            {
                await networkStream.WriteAsync(data, 0, data.Length);
            }
        }

        public void Disconnect()
        {
            
        }
    }
}