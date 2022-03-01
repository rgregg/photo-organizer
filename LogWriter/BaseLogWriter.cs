using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaOrganizerConsoleApp
{
    internal abstract class BaseLogWriter : ILogWriter
    {
        public bool Verbose { get; protected set; }
        public BaseLogWriter(bool verboseEnabled)
        {
            Verbose = verboseEnabled;
        }
        public abstract void WriteLine(string message);

        public virtual void WriteLog(string message, bool verbose)
        {
            if (!verbose || Verbose)
            {
                WriteLine(message);
            }
        }

        public virtual void WriteVerboseLine(string message)
        {
            WriteLog(message, true);
        }
    }
}
