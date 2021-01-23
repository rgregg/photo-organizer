using System;
using System.IO;
using CommandLine;
using System.Reflection;
using System.Diagnostics;

namespace MediaOrganizerConsoleApp
{
    public class Program : ILogWriter
    {
        static void Main(string[] args)
        {
            Console.WriteLine("PhotoOrganizer - {0}", Version);
            Program p = new Program();

            Parser.Default.ParseArguments<CopyCommandOptions, MoveCommandOptions, ScanCommandOptions, ConvertCommandOptions>(args)
                .WithParsed<MoveCommandOptions>(options => { p.OrganizeMedia(false, options); })
                .WithParsed<CopyCommandOptions>(options => { p.OrganizeMedia(true, options); })
                .WithParsed<ScanCommandOptions>(options => { p.ScanMedia(options); })
                .WithParsed<ConvertCommandOptions>(options => { p.ConvertMedia(options); })
                .WithNotParsed(errors => {
                    Console.WriteLine("Incorrect syntax. Error.");
                });
        }

        private void ConvertMedia(ConvertCommandOptions opts)
        {
            BreakForDebugger(opts);
            SetupLogging(opts);


            // Default to current folder
            if (string.IsNullOrEmpty(opts.SourceFolder))
            {
                opts.SourceFolder = System.Environment.CurrentDirectory;
            }

            if (!VerifyDirectoryExists("Destination", opts.DestinationFolder, false, out DirectoryInfo destination))
            {
                if (AskYesOrNoQuestion($"Destination directory '{opts.DestinationFolder}' doesn't exist. Create it now?", true))
                {
                    destination.Create();
                }
                else
                {
                    return;
                }
            }

            if (!VerifyDirectoryExists("Source", opts.SourceFolder, false, out DirectoryInfo source))
            {
                return;
            }

            WriteLog($"Converting media files in {opts.SourceFolder} to {opts.DestinationFolder}", false);
            WriteLog($"Resize: {opts.Resize}; Dimensions: {opts.Width}x{opts.Height}; Format: {opts.Format}; Quality: {opts.Quality}; Recursive: {opts.Recursive}", false);

            var converter = new Commands.ConvertMedia(source, destination, opts, this);
            converter.Scan();
        }

        private void OrganizeMedia(bool copyInsteadOfMove, OrganizeCommandLineOptions opts)
        {
            BreakForDebugger(opts);
            SetupLogging(opts);

            WriteLog($"Organizing media in {opts.SourceFolder} to {opts.DestinationFolder}", false);
            WriteLog($"Copy: {copyInsteadOfMove}; Conflict: {opts.ConflictBehavior}; Cache: {opts.CacheFileInfo}; Directory Format: {opts.DirectoryFormat}", true);
            opts.CopyInsteadOfMove = copyInsteadOfMove;

            ParserCache cache = new ParserCache(opts.SourceFolder, this);
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

            // Default to current folder
            if (string.IsNullOrEmpty(opts.SourceFolder))
            {
                opts.SourceFolder = System.Environment.CurrentDirectory;
            }

            if (!VerifyDirectoryExists("Destination", opts.DestinationFolder, false, out DirectoryInfo destination))
            {
                return;
            }

            if (!VerifyDirectoryExists("Source", opts.SourceFolder, false, out DirectoryInfo source)) 
            {
                return;
            }

            if (opts.ConflictBehavior == ExistingFileMode.Delete)
            {
                if (!AskYesOrNoQuestion("Deleting source files on existing files in destination is enabled.\nTHIS MAY CAUSE DATA LOSS, are you sure?", false, AnswerMode.Prompt))
                {
                    return;
                }
            }

            var organizer = new Commands.CopyMedia(source, destination, opts, cache, this);
            organizer.Scan();
            if (cache.CacheHasChanged)
            {
                cache.PersistCache();
            }
        }

        private bool VerifyDirectoryExists(string directoryType, string path, bool create, out DirectoryInfo directory)
        {
            directory = new DirectoryInfo(path);
            if (!directory.Exists)
            {
                if(create)
                {
                    directory.Create();
                }
                else
                {
                    WriteLog($"Error: {directoryType} directory does not exist: {path}", false);
                    return false;
                }
            }

            return true;
        }

        private void ScanMedia(ScanCommandOptions opts)
        {
            BreakForDebugger(opts);
            SetupLogging(opts);

            if (string.IsNullOrEmpty(opts.SourceFolder))
            {
                opts.SourceFolder = System.Environment.CurrentDirectory;
            }

            if (!VerifyDirectoryExists("Source", opts.SourceFolder, false, out DirectoryInfo source))
            {
                return;
            }

            WriteLog($"Scanning media in {source.FullName} for errors.", false);

            var scanner = new Commands.ScanMedia(source, opts, this);
            scanner.Scan();
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

        private bool VerboseLogOutput = false;
        private StreamWriter LogFileStream = null;
        public void WriteLog(string message, bool verbose)
        {
            if (!verbose || VerboseLogOutput)
            {
                Console.WriteLine(message);
                if (null != LogFileStream)
                {
                    LogFileStream.WriteLine($"{DateTime.Now} - {message}");
                }
            }
        }

        private void SetupLogging(UniversalCommandLineOptions opts)
        {
            VerboseLogOutput = opts.VerboseOutput;
            if (!string.IsNullOrEmpty(opts.LogFile))
            {
                LogFileStream = new StreamWriter(opts.LogFile, true) { AutoFlush = true };
            }
        }


        public static bool AskYesOrNoQuestion(string prompt, bool defaultIsYes, AnswerMode mode = AnswerMode.Prompt)
        {
            Console.Write(prompt);
            if (defaultIsYes)
            {
                Console.Write(" [Y/n]: ");
            }
            else
            {
                Console.Write(" [y/N]: ");
            }

            switch(mode)
            {
                case AnswerMode.AutoNo:
                    Console.WriteLine("N");
                    return false;
                    
                case AnswerMode.AutoYes:
                    Console.WriteLine("Y");
                    return true;
            }


            var key = Console.ReadKey();
            Console.WriteLine();
            if (key.KeyChar == 'y' || key.KeyChar == 'Y')
            {
                return true;
            }
            else if (key.KeyChar == 'n' || key.KeyChar == 'N')
            {
                return false;
            }

            // Otherwise, repeat the prompt.
            Console.WriteLine("Invalid input. Please answer y or n.");
            return AskYesOrNoQuestion(prompt, defaultIsYes, mode);
        }

        public enum AnswerMode
        {
            Prompt = 0,
            AutoYes,
            AutoNo
        }
    }
}
