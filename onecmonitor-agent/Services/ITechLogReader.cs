using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnecMonitor.Agent.Services
{
    internal interface ITechLogReader
    {
        bool MoveNext();
        long Position { get; }
        string FilePath { get; }
        string EventContent { get; }
    }
}
