using PuyoPac.HedgeLib.Archives;
using PuyoPac.IO;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PuyoPac
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var createCommand = new Command("create", "Create a new PAC archive.")
            {
                new Option<List<string>>(new string[] { "-d", "--dependency" }, "Add a dependency on a PAC archive. This option can be repeated multiple times for multiple dependencies. The path of the dependent PAC is relative to the raw folder, uses backslashes for folder seperators, and does not include the .pac file extension (e.g. \"ui\\ui_resident\").\n\nBy default, if no dependencies are provided, they will be automatically added based on the output name. This behavior can be disabled with the --no-auto-dependencies option.\n")
                {
                    AllowMultipleArgumentsPerToken = false,
                    Argument = new Argument<List<string>>("path"),
                },
                new Option<bool>("--no-splits", "Do not split embedded PACs."),
                new Option<bool>("--no-compression", "Do not compress embedded PACs."),
                new Option<bool>("--no-auto-dependencies", "Do not automatically add dependencies based on the output name."),
                new Argument<string>("output", "Set the output name.")
                    .LegalFilePathsOnly(),
                new Argument<List<FileSystemEnumerable>>("inputs", "The files, or folders containing files, to add. If a folder is specified, all files in that folder will be added. File and folder names can contain wildcard characters.")
                {
                    Arity = ArgumentArity.OneOrMore,
                }
                .ExistingOrSearchPatternsOnly(),
            };
            createCommand.Handler = CommandHandler.Create<CreateCommandOptions, IConsole>((options, console) => InvokeCommand(Create, options, console));

            var extractCommand = new Command("extract", "Extract a PAC archive.")
            {
                new Option<string>(new string[] { "-o", "--output" }, "The output folder. Defaults to the current folder.")
                    .LegalFilePathsOnly(),
                new Argument<FileInfo>("input", "The PAC archive to extract.")
                    .ExistingOnly(),
            };
            extractCommand.Handler = CommandHandler.Create<ExtractCommandOptions, IConsole>((options, console) => InvokeCommand(Extract, options, console));

            var rootCommand = new RootCommand("Create and extract PAC archives used in Puyo Puyo Tetris 2.")
            {
                createCommand,
                extractCommand,
            };

            return await rootCommand.InvokeAsync(args);
        }

        static int InvokeCommand<T>(Action<T> action, T options, IConsole console = null)
        {
            try
            {
                action(options);

                return 0;
            }
            catch (Exception e)
            {
                if (console != null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    console.Error.Write(e.Message + Environment.NewLine);
                    Console.ResetColor();
                }

                return e.HResult;
            }
        }

        static void Create(CreateCommandOptions options)
        {
            // Verify output path has a file extension. If not, add it.
            if (Path.GetExtension(options.Output) == string.Empty)
            {
                options.Output += ".pac";
            }

            // Add dependencies, and allow aliases.
            var dependencies = new List<string>();
            if (options.Dependency?.Any() == true)
            {
                foreach (var dependency in options.Dependency)
                {
                    if (dependency.Equals("ui_cutin_2p", StringComparison.OrdinalIgnoreCase))
                    {
                        dependencies.Add(@"ui\ui_cutin_cmn");
                        dependencies.Add(@"ui\ui_resident");
                        dependencies.Add(@"ui\ui_system");
                    }
                    if (dependency.Equals("ui_cutin_cmn", StringComparison.OrdinalIgnoreCase))
                    {
                        dependencies.Add(@"ui\ui_resident");
                        dependencies.Add(@"ui\ui_system");
                    }
                    else if (dependency.Equals("ui", StringComparison.OrdinalIgnoreCase))
                    {
                        dependencies.Add(@"ui\ui_resident");
                    }
                    else
                    {
                        var dependencyPath = dependency.Replace('/', '\\');
                        dependencyPath = Path.ChangeExtension(dependencyPath, null);
                        dependencies.Add(dependencyPath);
                    }
                }
            }

            // If no dependencies were provided, try to determine them by the output file name
            else if (!options.NoAutoDependencies)
            {
                var outputFilename = Path.GetFileName(options.Output);
                if (outputFilename.StartsWith("ui_cutin_2p_", StringComparison.OrdinalIgnoreCase))
                {
                    dependencies.Add(@"ui\ui_cutin_cmn");
                    dependencies.Add(@"ui\ui_resident");
                    dependencies.Add(@"ui\ui_system");
                }
                else if (outputFilename.StartsWith("ui_cutin_cmn", StringComparison.OrdinalIgnoreCase))
                {
                    dependencies.Add(@"ui\ui_resident");
                    dependencies.Add(@"ui\ui_system");
                }
                else if (outputFilename.StartsWith("ui_", StringComparison.OrdinalIgnoreCase)
                    && !outputFilename.StartsWith("ui_resident", StringComparison.OrdinalIgnoreCase))
                {
                    dependencies.Add(@"ui\ui_resident");
                }
            }

            // Remove duplicate dependencies and order them.
            dependencies = dependencies
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            var archive = new ForcesArchive();

            // Add files to the archive
            foreach (var file in options.Inputs.SelectMany(x => x))
            {
                archive.Data.Add(new ArchiveFile(file));
            }

            // Save the archive
            archive.Save(options.Output,
                overwrite: true,
                dependencies: dependencies,
                splitCount: options.NoSplits ? null : ForcesArchive.DefaultSplitSize,
                compressSize: options.NoCompression ? null : ForcesArchive.DefaultCompressSize);
        }

        static void Extract(ExtractCommandOptions options)
        {
            var archive = new ForcesArchive();
            archive.Load(options.Input.FullName);
            archive.Extract(options.Output ?? Environment.CurrentDirectory);
        }
    }
}
