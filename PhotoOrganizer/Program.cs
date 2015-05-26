using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CommandLine.Text;
using RyanGregg.Extensions;

namespace PhotoOrganizer
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("PhotoOrganizer {0}", args.ComponentsJoinedByString(" "));

            var opts = new CommandLineOptions();
            if (!CommandLine.Parser.Default.ParseArguments(args, opts))
            {
                Console.WriteLine(HelpText.AutoBuild(opts));
                return 1;
            }

            if (string.IsNullOrEmpty(opts.SourceFolder))
                opts.SourceFolder = System.Environment.CurrentDirectory;

            IDirectory destination = GetDirectory(opts, opts.DestinationFolder);
            IDirectory source = GetDirectory(opts, opts.SourceFolder);

            if (!destination.Exists)
            {
                Console.WriteLine("Error: Destination folder doesn't exist.");
                return 2;
            }
            else
            {
                Console.WriteLine("Destination: " + destination.FullName);
            }

            if (!source.Exists)
            {
                Console.WriteLine("Error: Source folder doesn't exist. Nothing to do.");
                return 3;
            }
            else
            {
                Console.WriteLine("Source: " + source.FullName);
            }

            if (opts.ExistingFileBehavior == ExistingFileMode.DeleteSourceFileWhenIdentical)
            {
                Console.Write("Delete source files on existing files in destination is enabled.\nTHIS MAY CAUSE DATA LOSS, are you sure? [Y/N]: ");
                var key = Console.ReadKey();
                if (!(key.KeyChar == 'y' || key.KeyChar == 'Y'))
                    return 4;
                Console.WriteLine();
            }


            if (opts.TargetService != SupportedServices.LocalFileSystem && string.IsNullOrEmpty(opts.AccessToken))
            {
                Console.WriteLine("Enter an access token for {0}: ", opts.TargetService);
                opts.AccessToken = Console.ReadLine();
            }

            FileOrganizer organizer = new FileOrganizer(destination, opts);
            Nito.AsyncEx.AsyncContext.Run(() => organizer.ProcessSourceFolderAsync(source));

            Console.ReadKey();
            return 0;
        }

        private static OneDrive.ODConnection OneDriveConnection = null;

        private static void InitOneDrive(CommandLineOptions options)
        {
            if (null != OneDriveConnection) return;

            OneDriveConnection = new OneDrive.ODConnection("https://api.onedrive.com/v1.0", options.AccessToken);
        }

        private static IDirectory GetDirectory(CommandLineOptions options, string path)
        {
            switch (options.TargetService)
            {
                case SupportedServices.LocalFileSystem:
                    return new LocalFileSystem.LocalDirectory(path);
                case SupportedServices.OneDrive:
                    InitOneDrive(options);
                    return new OneDriveFileSystem.OneDriveDirectory(OneDriveConnection, path);
                default:
                    throw new NotImplementedException("Support for service " + options.TargetService.ToString() + " is not implemeneted.");
            }
        }
    }
}
