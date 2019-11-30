using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace PhotoOrganizer
{
    public class CommandLineOptions
    {
        [Option('d', "destination", HelpText="Parent directory where files should be moved", Required=true)]
        public string DestinationFolder { get; set; }

        [Option('s', "source", HelpText="Source folder path")]
        public string SourceFolder { get; set; }

        [Option("verbose", HelpText="Verbose console output")]
        public bool VerboseOutput { get; set; }

        [Option("simulate", HelpText="Simulate the action, without actually moving files")]
        public bool Simulate { get; set; }

        [Option("copy", HelpText="Copy files to the destination and leave them in the source folder.")]
        public bool CopyInsteadOfMove { get; set; }

        [Option("conflict", HelpText = "Define the conflict behavior. Options are skip, rename, overwrite, or delete (the source file).", DefaultValue = ExistingFileMode.Skip)]
        public ExistingFileMode ConflictBehavior { get; set; }

        [Option("parallel", HelpText="Perform the file operations in parallel")]
        public bool RunInParallel { get; set; }

        [Option("dest-format", HelpText = "Destination folder format. Uses the DateTime formatting syntax.", DefaultValue = "yyyy\\\\yyyy-MM-MMMM")]
        public string DirectoryFormat { get; set; }

        [Option('r', "recursive", HelpText="Recurse through subfolders of the source folder", DefaultValue=true)]
        public bool Recursive { get; set; }

        [Option("debug")]
        public bool Debug { get; set; }


        [Option("parser", HelpText = "Choose the data parser used to evaluate the files.")]
        public DataParser DataParser { get; set; }
    }

    public enum ExistingFileMode
    {
        Skip,
        Rename,
        Overwrite,
        Delete
    }

    public enum DataParser
    {
        Default,
        TagLib,
        Shell32,
        Universal
    }
}
