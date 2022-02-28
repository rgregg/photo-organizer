using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CommandLine;

namespace PhotoOrganizer
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new CommandLine.Parser(with => with.HelpWriter = null);
            var parsedResult = parser.ParseArguments<MoveAction, CopyAction, ResetCacheAction, DeduplicateFilesAction>(args);
            parsedResult
                .WithParsed<MoveAction>(OrganizeFiles)
                .WithParsed<CopyAction>(OrganizeFiles)
                .WithParsed<ResetCacheAction>(ResetCache)
                .WithParsed<DeduplicateFilesAction>(DedupeFiles)
                .WithNotParsed(errs => HandleParseError(parsedResult, errs));
            
        }

        static void DedupeFiles(DeduplicateFilesAction obj)
        {
            throw new NotImplementedException();
        }

        static void ResetCache(ResetCacheAction opts)
        {
            ParsedFileCache cache = new ParsedFileCache(opts.SourceFolder);
            cache.ClearAll();
            cache.PersistCache();
            Console.WriteLine("Shared metadata cache has been cleared.");
        }

        static void OrganizeFiles(OrganizedFilesAction opts)
        {
            if (opts.Debug)
            {
                System.Diagnostics.Debugger.Break();
            }

            ParsedFileCache cache = new ParsedFileCache(opts.SourceFolder);
            Console.CancelKeyPress += (sender, eventArgs) => {
                
                if (opts.CacheFileInfo)
                {
                    Console.WriteLine("Flushing cache to disk...");
                    cache.PersistCache();
                }
                Environment.Exit(-1);
            };


            if (string.IsNullOrEmpty(opts.SourceFolder))
                opts.SourceFolder = System.Environment.CurrentDirectory;

            

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

            FileOrganizer organizer = new FileOrganizer(destination, opts, cache);
            organizer.ProcessSourceFolder(source);
            if (cache.HasChanged)
            {
                cache.PersistCache();
            }
        }

        static void HandleParseError<T>(ParserResult<T> result, IEnumerable<Error> errs)
        {
            Console.WriteLine("errors {0}", errs.Count());

            var helpText = CommandLine.Text.HelpText.AutoBuild(result);
            Console.WriteLine(helpText);
        }
    }
}
