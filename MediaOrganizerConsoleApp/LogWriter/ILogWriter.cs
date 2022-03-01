using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaOrganizerConsoleApp
{
    public interface ILogWriter
    {
        void WriteLog(string message, bool verbose);
        void WriteVerboseLine(string message);
        void WriteLine(string message);
    }
}
