using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaOrganizerConsoleApp
{
    internal class ConsoleLogWriter : BaseLogWriter
    {
        public ConsoleLogWriter(bool verboseEnabled) : base(verboseEnabled)
        {
            
        }
        public override void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

    }
}
