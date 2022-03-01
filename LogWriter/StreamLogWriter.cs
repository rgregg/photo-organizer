using System.IO;

namespace MediaOrganizerConsoleApp
{
    internal class StreamLogWriter : BaseLogWriter
    {
        private StreamWriter writer;
        public StreamLogWriter(string logFilePath, bool verbose) : base(verbose)
        {
            writer = new StreamWriter(logFilePath, true) { AutoFlush = true };
        }

        public override void WriteLine(string message)
        {
            writer.WriteLine(message);
        }
    }
}
