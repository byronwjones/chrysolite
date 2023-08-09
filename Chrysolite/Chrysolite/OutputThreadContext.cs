using System.IO;

namespace Chrysolite
{
    internal class OutputThreadContext
    {
        public OutputThreadContext(StreamReader stream, int maxOutputLatency)
        {
            Stream = stream;
            MaxOutputLatency = maxOutputLatency;
        }

        public StreamReader Stream { get; }
        public int MaxOutputLatency { get; }
    }
}
