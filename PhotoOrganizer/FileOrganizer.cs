using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizer
{
    public class FileOrganizer
    {
        public IDirectory Destination { get; private set; }

        CommandLineOptions Options { get; set; }

        public FileOrganizer(IDirectory destination)
        {
            this.Destination = destination;
        }

        public FileOrganizer(IDirectory destination, CommandLineOptions opts) : this(destination)
        {
            this.Options = opts;
        }

        public async Task ProcessSourceFolderAsync(IDirectory source)
        {
            if (Options.VerboseOutput)
                Console.WriteLine("Process source folder {0}.", source.FullName);

            var files = await source.EnumerateFilesAsync();
            if (this.Options.RunInParallel)
            {
                await files.ForEachAsync(4, async file => await ProcessFileAsync(file));
            }
            else
            {
                var filesToProcess = files.ToArray();
                foreach (var file in filesToProcess)
                {
                    await ProcessFileAsync(file, null);
                }
            }

            if (this.Options.Recursive)
            {
                var folders = await source.EnumerateDirectoriesAsync();
                if (Options.RunInParallel)
                {
                    Parallel.ForEach(folders, async (folder) => await ProcessSourceFolderAsync(folder));
                }
                else
                {
                    foreach (var folder in folders) 
                    { 
                        await ProcessSourceFolderAsync(folder); 
                    }
                }
            }
        }

        /// <summary>
        /// Called on each file in the source directory. Decide how to handle that file, and 
        /// then handle it.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="suggestion"></param>
        /// <returns>The dateTaken value of this photo or video, if available.</returns>
        private async Task<DateTimeOffset?> ProcessFileAsync(IFile file, DateTimeOffset? suggestion = null)
        {
            bool moveThisFile = (this.Options.ActOnImages && file.PerceivedType == DetailFileInfo.PerceivedFileType.Image) ||
                                (this.Options.ActOnVideos && file.PerceivedType == DetailFileInfo.PerceivedFileType.Video);

            DateTimeOffset? dateTaken = file.DateTaken;
            if (!dateTaken.HasValue && file.PerceivedType == DetailFileInfo.PerceivedFileType.Video 
                && this.Options.InferVideoDate && suggestion.HasValue)
            {
                if (this.Options.VerboseOutput) Console.WriteLine("Infering date for {0} as {1}", file.Name, suggestion);
                dateTaken = suggestion;
            }

            if (moveThisFile)
            {
                string targetFullName = null;
                if (dateTaken.HasValue)
                {
                    targetFullName = dateTaken.Value.ToString(Options.DirectoryFormat);
                }
                else
                {
                    targetFullName = "No date";
                }
                IDirectory targetDirectory = await this.Destination.GetChildDirectoryAsync(targetFullName);

                if (this.Options.VerboseOutput)
                    Console.WriteLine("Moving {0} to {1}", file.Name, targetDirectory.FullName);

                if (!this.Options.Simulate)
                {
                    try
                    {
                        await targetDirectory.CreateAsync();
                        await PushFileToTarget(file, targetDirectory, this.Options.ExistingFileBehavior);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.ToString());
                    }
                }
            }
            else if (this.Options.VerboseOutput && moveThisFile)
            {
                Console.WriteLine("Skipping file (no date taken): " + file.Name);
            }
            else if (this.Options.VerboseOutput)
            {
                Console.WriteLine("Skipping file (not included type): " + file.Name);
            }

            return dateTaken;
        }

        private void VerboseLog(string message)
        {
            if (Options.VerboseOutput)
                Console.WriteLine(message);
        }
       
        private async Task PushFileToTarget(IFile file, IDirectory targetPath, ExistingFileMode fileExistsBehavior)
        {
            if (this.Options.CopyInsteadOfMove)
            {
                await file.CopyToAsync(targetPath, fileExistsBehavior);
            }
            else
            {
                await file.MoveToAsync(targetPath, fileExistsBehavior);
            }
        }

    }
    
    [Flags]
    public enum FileComparison
    {
        None = 0,
        Filename = 1,
        Length = 2,
        SHA1Hash = 4,
        DateTimeModified = 8,

        All = 15
    }
}
