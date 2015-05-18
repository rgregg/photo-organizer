using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuplicatePhotoFinder
{
    class CommandLineOptions
    {
        [Option("refresh-token", MutuallyExclusiveSet="auth-token")]
        public string RefreshToken { get; set; }

        [Option("access-token", MutuallyExclusiveSet = "auth-token")]
        public string AccessToken { get; set; }

        [Option("path", DefaultValue="/special/photos/")]
        public string Path { get; set; }
    }
}
