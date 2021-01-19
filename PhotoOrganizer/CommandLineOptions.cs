using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace PhotoOrganizer
{

    [Verb("copy", HelpText = "Copy files from the source to destination in an organized fashion.")]
    public class CopyCommandOptions : OrganizeCommandLineOptions
    {
    }

    [Verb("move", HelpText = "Move files from the source to destination in an organized fashion.")]
    public class MoveCommandOptions : OrganizeCommandLineOptions
    {
    }

    [Verb("scan", HelpText = "Scan and report on files in the source directory.")]
    public class ScanCommandOptions : UniversalCommandLineOptions
    {
        [Option('s', "source", HelpText = "Source folder path")]
        public string SourceFolder { get; set; }

        [Option('r', "recursive", HelpText = "Recurse through subfolders of the source folder", Default = true)]
        public bool Recursive { get; set; }

        [Option("parser", HelpText = "Choose the data parser used to evaluate the files.")]
        public DataParser DataParser { get; set; }
    }

    public class OrganizeCommandLineOptions : UniversalCommandLineOptions
    {
        [Option('d', "destination", HelpText = "Parent directory where files should be moved", Required = true)]
        public string DestinationFolder { get; set; }

        [Option('s', "source", HelpText = "Source folder path")]
        public string SourceFolder { get; set; }

        [Option("conflict", HelpText = "Define the conflict behavior. Options are skip, rename, overwrite, or delete (the source file).", Default = ExistingFileMode.Skip)]
        public ExistingFileMode ConflictBehavior { get; set; }

        [Option("parallel", HelpText = "Perform the file operations in parallel")]
        public bool RunInParallel { get; set; }

        [Option("dest-format", HelpText = "Destination folder format. Uses the DateTime formatting syntax.", Default = "yyyy\\\\yyyy-MM-MMMM")]
        public string DirectoryFormat { get; set; }

        [Option('r', "recursive", HelpText = "Recurse through subfolders of the source folder", Default = true)]
        public bool Recursive { get; set; }

        [Option("cache", Default = false, HelpText = "Use a persistent cache for expensive to calculate media properties")]
        public bool CacheFileInfo { get; set; }

        [Option("reset-cache", Default = false, HelpText = "Force the cache file to be cleared and reload all file properties.")]
        public bool ResetCache { get; set; }

        [Option("parser", HelpText = "Choose the data parser used to evaluate the files.")]
        public DataParser DataParser { get; set; }

        public bool CopyInsteadOfMove { get; set; }
    }

    public class UniversalCommandLineOptions
    {
        [Option("verbose", HelpText = "Verbose console output", Default = false)]
        public bool VerboseOutput { get; set; }

        [Option("simulate", HelpText = "Simulate the action, without actually moving files", Default = false)]
        public bool Simulate { get; set; }

        [Option("debug", HelpText = "Force a debugger to be attached to the process.", Default = false)]
        public bool Debug { get; set; }
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
