using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CommandLine;
using System.Reflection;
using System.Diagnostics;

namespace PhotoOrganizer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("PhotoOrganizer - {0}", Version);
            Program p = new Program();

            Parser.Default.ParseArguments<CopyCommandOptions, MoveCommandOptions, ScanCommandOptions>(args)
                .WithParsed<MoveCommandOptions>(options => { p.OrganizeMedia(false, options); })
                .WithParsed<CopyCommandOptions>(options => { p.OrganizeMedia(true, options); })
                .WithParsed<ScanCommandOptions>(options => { p.ScanMedia(options); })
                .WithNotParsed(errors => {
                    Console.WriteLine("Incorrect syntax. Error.");
                });
        }

        private void OrganizeMedia(bool copyInsteadOfMove, OrganizeCommandLineOptions opts)
        {
            BreakForDebugger(opts);

            opts.CopyInsteadOfMove = copyInsteadOfMove;

            ParsedFileCache cache = new ParsedFileCache(opts.SourceFolder);
            if (opts.ResetCache)
            {
                cache.ClearAll();
            }

            Console.CancelKeyPress += (sender, eventArgs) => {

                if (opts.CacheFileInfo)
                {
                    Console.WriteLine("Flushing cache to disk...");
                    cache.PersistCache();
                }
                Environment.Exit(-1);
            };


            if (string.IsNullOrEmpty(opts.SourceFolder))
            {
                opts.SourceFolder = System.Environment.CurrentDirectory;
            }

            DirectoryInfo destination = new DirectoryInfo(opts.DestinationFolder);
            if (!destination.Exists)
            {
                Console.WriteLine("Error: Destination folder doesn't exist.");
                return;
            }

            DirectoryInfo source = new DirectoryInfo(opts.SourceFolder);
            if (!source.Exists)
            {
                Console.WriteLine("Error: Source folder doesn't exist. Nothing to do.");
                return;
            }

            if (opts.ConflictBehavior == ExistingFileMode.Delete)
            {
                Console.Write("Delete source files on existing files in destination is enabled.\nTHIS MAY CAUSE DATA LOSS, are you sure? [Y/N]: ");
                var key = Console.ReadKey();
                if (!(key.KeyChar == 'y' || key.KeyChar == 'Y'))
                    return;
                Console.WriteLine();
            }

            MediaOrganizer organizer = new MediaOrganizer(source, destination, opts, cache);
            organizer.Scan();
            if (cache.HasChanged)
            {
                cache.PersistCache();
            }
        }

        private void ScanMedia(ScanCommandOptions options)
        {
            BreakForDebugger(options);
        }

        private void BreakForDebugger(UniversalCommandLineOptions opts)
        {
            if (opts.Debug)
            {
                Debugger.Break();
            }
        }

        public static string Version
        {
            get
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(asm.Location);
                return String.Format("{0}.{1}", fvi.ProductMajorPart, fvi.ProductMinorPart);
            }
        }

    }
}
