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

        [Option('s', "source", HelpText="Source folder to reader from")]
        public string SourceFolder { get; set; }

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

        [Option("rename", HelpText="Rename when an existing file is encountered", MutuallyExclusiveSet="existBehavior")]
        public bool RenameOnExistingFile { get; set; }

        [Option("overwrite", HelpText = "Overwrite when an existing file is encountered", MutuallyExclusiveSet = "existBehavior")]
        public bool OverwriteOnExistingFile { get; set; }

        [Option("delete", HelpText = "Delete the source file when the same file already exists in the destination", MutuallyExclusiveSet = "existBehavior")]
        public bool DeleteSourceOnExistingFile { get; set; }

        [Option("parallel", HelpText="Perform the file operations in parallel")]
        public bool RunInParallel { get; set; }

        [Option("infer-video-date", HelpText="Infer the date a video was recorded based on the images before it (not compatible with parallel mode.")]
        public bool InferVideoDate { get; set; }

        [Option("dest-format", HelpText = "Destination folder format. Uses the DateTime formatting syntax.", DefaultValue = "yyyy\\\\yyyy-MM-MMMM")]
        public string DirectoryFormat { get; set; }

        [Option('r', "recursive", HelpText="Recurse through subfolders of the source folder")]
        public bool Recursive { get; set; }

        internal ExistingFileMode ExistingFileBehavior
        {
            get
            {
                if (RenameOnExistingFile)
                    return ExistingFileMode.Rename;
                else if (OverwriteOnExistingFile)
                    return ExistingFileMode.Overwrite;
                else if (DeleteSourceOnExistingFile)
                    return ExistingFileMode.DeleteSourceFile;
                else
                    return ExistingFileMode.Skip;
            }
        }
    }

    public enum ExistingFileMode
    {
        Skip,
        Rename,
        Overwrite,
        DeleteSourceFile
    }
}
