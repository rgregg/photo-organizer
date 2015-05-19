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
        

        [Option('d', "destination", HelpText="Parent directory where files should be organized", Required=true)]
        public string DestinationFolder { get; set; }

        [Option('s', "source", HelpText="Source folder to reader from")]
        public string SourceFolder { get; set; }

        [Option("service", DefaultValue=SupportedServices.LocalFileSystem, HelpText="Specify the target service to use: LocalFileSystem, OneDrive")]
        public SupportedServices TargetService { get; set; } 

        [Option("video", HelpText = "Enable oranizing videos", DefaultValue=false)]
        public bool ActOnVideos { get; set; }

        [Option("images", HelpText = "Enable organizing pictures", DefaultValue = true)]
        public bool ActOnImages { get; set; }

        [Option("verbose", HelpText="Output verbose logging")]
        public bool VerboseOutput { get; set; }

        [Option("simulate", HelpText="Simulate the move, without actually moving files")]
        public bool Simulate { get; set; }

        [Option("copy", HelpText="Copy files to the destination instead of moving them")]
        public bool CopyInsteadOfMove { get; set; }

        [Option("existing-file", HelpText="Control the behavior when a file already exists in the destination folder. Values: Abort, Ignore, Rename, Overwrite, DeleteSourceFileWhenIdentical")]
        public ExistingFileMode ExistingFileBehavior { get; set; }

        [Option("parallel", HelpText="Perform the file operations in parallel")]
        public bool RunInParallel { get; set; }

        [Option("infer-video-date", HelpText="Infer the date a video was recorded based on the images before it (not compatible with parallel mode.")]
        public bool InferVideoDate { get; set; }

        [Option("dest-format", HelpText = "Destination folder format. Uses the DateTime formatting syntax.", DefaultValue = @"yyyy\\yyyy-MM-MMMM")]
        public string DirectoryFormat { get; set; }

        [Option('r', "recursive", HelpText="Recurse through subfolders of the source folder")]
        public bool Recursive { get; set; }

        [Option("access-token", HelpText="Provide an access token for using networked services")]
        public string AccessToken { get; set; }

    }

    public enum SupportedServices
    {
        LocalFileSystem,
        OneDrive
    }

    
}
