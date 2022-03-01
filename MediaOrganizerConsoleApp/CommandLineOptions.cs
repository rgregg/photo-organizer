using CommandLine;

namespace MediaOrganizerConsoleApp
{

    [Verb("copy", HelpText = "Copy files from the source to destination in an organized fashion.")]
    public class CopyCommandOptions : OrganizeCommandLineOptions
    {
    }

    [Verb("move", HelpText = "Move files from the source to destination in an organized fashion.")]
    public class MoveCommandOptions : OrganizeCommandLineOptions
    {
    }

    [Verb("convert", HelpText = "Convert files from one format to another.")]
    public class ConvertCommandOptions : UniversalCommandLineOptions
    {
        [Option('s', "source", Required = true, HelpText = "Source folder for media files")]
        public string SourceFolder { get; set; }

        [Option("media", Default = MediaType.Default, HelpText = "Allow performing conversion on only a specific type of media files. Default converts all supported files.")]
        public MediaType MediaTypeFilter { get; set; }

        [Option('d', "destination", Required = true, HelpText = "Destination folder for converted files")]
        public string DestinationFolder { get; set; }

        [Option ("resize", Default = false, HelpText = "Resize media while converting to another format.")]
        public bool Resize { get; set; }

        [Option('w', HelpText = "Desired width for converted files")]
        public int Width { get; set; }

        [Option('h', HelpText = "Desired width for converted files")]
        public int Height { get; set; }

        [Option("format", Default = "jpg", HelpText = "File format for output files")]
        public string Format { get; set; }

        [Option("quality", Default = 80, HelpText = "Quality setting for the output files")]
        public int Quality { get; set; }

        [Option("overwrite", Default = false, HelpText = "Overwrite existing files in the output directory.")]
        public bool Overwrite { get; set; }
    }

    [Verb("scan", HelpText = "Scan and report on files in the source directory.")]
    public class ScanCommandOptions : UniversalCommandLineOptions
    {
        [Option('s', "source", Required = true, HelpText = "Source folder path")]
        public string SourceFolder { get; set; }

        [Option('y', HelpText = "Answer yes/no questions with YES automatically.")]
        public bool DefaultToYes { get; set; }

        [Option('n', HelpText = "Answer yes/no questions with NO automatically.")]
        public bool DefaultToNo { get; set; }
    }

    [Verb("dedupe", HelpText = "Check for duplicate files and optionally clean them up.")]
    public class DupeCheckerCommandOptions : UniversalCommandLineOptions
    {
        [Option('s', "source", Required = true, HelpText = "Source folder for media files")]
        public string SourceFolder { get; set; }
        [Option("cache", Default = false, HelpText = "Use a persistent cache for expensive to calculate media properties")]
        public bool CacheFileInfo { get; set; }

        [Option("ignore-live-photos", HelpText = "Ignore duplicate filenames where there is only an image and QuickTime video.")]
        public bool IgnoreLivePhotos{ get; set; }


    }

    public class OrganizeCommandLineOptions : UniversalCommandLineOptions
    {
        [Option('d', "destination", HelpText = "Parent directory where files should be moved", Required = true)]
        public string DestinationFolder { get; set; }

        [Option('s', "source", HelpText = "Source folder path")]
        public string SourceFolder { get; set; }

        [Option("conflict", HelpText = "Define the conflict behavior. Options are skip, rename, overwrite, or delete (the source file).", Default = ExistingFileMode.Skip)]
        public ExistingFileMode ConflictBehavior { get; set; }

        [Option("dest-format", HelpText = "Destination folder format. Uses the DateTime formatting syntax.", Default = "yyyy\\\\yyyy-MM-MMMM")]
        public string DirectoryFormat { get; set; }

        [Option("cache", Default = false, HelpText = "Use a persistent cache for expensive to calculate media properties")]
        public bool CacheFileInfo { get; set; }

        [Option("reset-cache", Default = false, HelpText = "Force the cache file to be cleared and reload all file properties.")]
        public bool ResetCache { get; set; }
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
        
        [Option("log", HelpText = "Path to a file where logs will be written")]
        public string LogFile { get; set; }

        [Option('r', "recursive", HelpText = "Recurse through subfolders of the source folder", Default = true)]
        public bool Recursive { get; set; }

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
