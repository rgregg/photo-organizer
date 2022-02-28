using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace PhotoOrganizer
{
    class CommonCommandLineOptions
    {
        [Option('s', "source", HelpText = "Source folder path")]
        public string SourceFolder { get; set; }

        [Option('r', "recursive", HelpText = "Recurse through subfolders of the source folder", Default = true)]
        public bool Recursive { get; set; }

        [Option("debug")]
        public bool Debug { get; set; }

        [Option("verbose", HelpText = "Verbose console output")]
        public bool VerboseOutput { get; set; }

        [Option("dry-run", HelpText = "Perform a dry-run, simulating the action, without actually moving or deleting files")]
        public bool Simulate { get; set; }

        [Option("parallel", HelpText = "Perform the file operations in parallel")]
        public bool RunInParallel { get; set; }

        [Option("cache", Default = false, HelpText = "Use a persistent cache for expensive to calculate media properties")]
        public bool CacheFileInfo { get; set; }
        
        [Option("parser", HelpText = "Choose the data parser used to evaluate the files.")]
        public DataParser DataParser { get; set; }

    }

    abstract class OrganizedFilesAction : CommonCommandLineOptions
    {
        [Option('d', "destination", HelpText = "Destination directory for organized files", Required = true)]
        public string DestinationFolder { get; set; }

        [Option("conflict", HelpText = "Define the conflict behavior. Options are skip, rename, overwrite, or delete (the source file).", Default = ExistingFileMode.Skip)]
        public ExistingFileMode ConflictBehavior { get; set; }

        [Option("dest-format", HelpText = "Destination directory format. Uses the DateTime formatting syntax.", Default = "yyyy\\\\yyyy-MM-MMMM")]
        public string DirectoryFormat { get; set; }

        public abstract bool CopyInsteadOfMove { get; }
    }


    [Verb("copy", HelpText = "Copy files from the source to destination folder.")]
    class CopyAction : OrganizedFilesAction
    {
        public override bool CopyInsteadOfMove {  get { return true; } }
    }

    [Verb ("move", HelpText = "Move files from the source to destination folder.")]
    class MoveAction : OrganizedFilesAction
    {
        public override bool CopyInsteadOfMove { get { return false; } }
    }

    [Verb("reset-cache", HelpText = "Reset the cached metadata properties and read metadata from files.")]
    class ResetCacheAction : CommonCommandLineOptions
    {

    }


    class DeduplicateFilesAction : CommonCommandLineOptions
    {

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
        //Shell32,
        Universal
    }
}
