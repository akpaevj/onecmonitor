using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnecMonitor.Common.Models
{
    public class Message
    {
        public MessageHeader Header { get; set; }
        public ReadOnlyMemory<byte> Data { get; set; }

        public Message(MessageHeader header)
        {
            Header = header;
            Data = Memory<byte>.Empty;
        }

        public Message(MessageHeader header, Memory<byte> data) 
        {
            Header = header;
            Data = data;
        }
    }
}
