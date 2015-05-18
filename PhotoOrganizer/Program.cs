using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CommandLine.Text;

namespace PhotoOrganizer
{
    class Program
    {
        static void Main(string[] args)
        {
            var opts = new CommandLineOptions();
            if (!CommandLine.Parser.Default.ParseArguments(args, opts))
            {
                Console.WriteLine(HelpText.AutoBuild(opts));
                return;
            }

            if (string.IsNullOrEmpty(opts.SourceFolder))
                opts.SourceFolder = System.Environment.CurrentDirectory;

            IDirectory destination = new LocalFileSystem.LocalDirectory(opts.DestinationFolder);
            if (!destination.Exists)
            {
                Console.WriteLine("Error: Destination folder doesn't exist.");
                return;
            }

            IDirectory source = new LocalFileSystem.LocalDirectory(opts.SourceFolder);
            if (!source.Exists)
            {
                Console.WriteLine("Error: Source folder doesn't exist. Nothing to do.");
                return;
            }

            if (opts.DeleteSourceOnExistingFile)
            {
                Console.Write("Delete source files on existing files in destination is enabled.\nTHIS MAY CAUSE DATA LOSS, are you sure? [Y/N]: ");
                var key = Console.ReadKey();
                if (!(key.KeyChar == 'y' || key.KeyChar == 'Y'))
                    return;
                Console.WriteLine();
            }

            FileOrganizer organizer = new FileOrganizer(destination, opts);
            organizer.ProcessSourceFolder(source);
        }
    }
}
