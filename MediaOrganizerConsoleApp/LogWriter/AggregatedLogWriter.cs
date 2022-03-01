using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaOrganizerConsoleApp
{
    internal class AggregatedLogWriter : BaseLogWriter
    {
        List<ILogWriter> writers;
        public AggregatedLogWriter(bool verbose, params ILogWriter[] writers) : base(verbose)
        {
            this.writers = new List<ILogWriter>(writers);
        }

        public void AddWriter(ILogWriter writer)
        {
            writers.Add(writer);
        }

        public void RemoveWriter(ILogWriter writer) 
        {
            writers.Remove(writer); 
        }

        public override void WriteLine(string message)
        {
            foreach(var writer in writers) { writer.WriteLine(message); }
        }
    }
}
