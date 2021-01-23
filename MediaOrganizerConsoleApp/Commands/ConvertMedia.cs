using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MediaOrganizerConsoleApp.Commands
{
    public class ConvertMedia : RecursiveFileScanner
    {
        protected DirectoryInfo Destination { get; set; }

        protected MediaType MediaTypeToConvert { get; set; }

        protected FormatConversion.ImageMagick Converter { get; set; }

        public ConvertMedia(DirectoryInfo source, DirectoryInfo destination, ConvertCommandOptions opts, ILogWriter logWriter) 
            : base(source, opts.Recursive, opts.VerboseOutput, false, logWriter)
        {
            Destination = destination;
            MediaTypeToConvert = opts.MediaTypeFilter;

            Converter = new FormatConversion.ImageMagick(logWriter)
            {
                CopyMetadata = true,
                OutputFormat = opts.Format,
                OutputQuality = opts.Quality,
                Resize = opts.Resize,
                Geometry = new FormatConversion.ImageMagick.GeometrySettings
                {
                    Height = opts.Height,
                    Width = opts.Width,
                    Mode = FormatConversion.ImageMagick.ResizeOptions.ShrinkAspectPreserved
                }
            };
        }

        protected override bool IsExcludedFile(FileInfo file, FormatSignature signature)
        {
            // Skip files that don't match the filter, if a filter is applied.
            if (MediaTypeToConvert != MediaType.Default && signature.Type != MediaTypeToConvert)
            {
                return false;
            }
            
            return base.IsExcludedFile(file, signature);
        }

        protected override void ScanFile(FileInfo file, FormatSignature signature)
        {
            base.ScanFile(file, signature);

            var destinationFile = Path.Combine(Destination.FullName, RelativePathFromSource(file), file.Name);
            Converter.ConvertFormat(file, destinationFile);
        }

        protected string RelativePathFromSource(FileInfo file)
        {
            var sourceFolder = OriginalSource.FullName;
            var currentFolder = file.Directory.FullName;
            if (currentFolder.StartsWith(sourceFolder))
            {
                var relativePath = currentFolder.Substring(sourceFolder.Length);
                if (relativePath.StartsWith(Path.DirectorySeparatorChar))
                {
                    relativePath = relativePath.Substring(1);
                }
                return relativePath;
            }

            throw new NotSupportedException("Cannot calculate relative path for files that are not in the source folder.");
        }

    }
}
