using PuyoPac.HedgeLib.Archives;
using PuyoPac.HedgeLib.Compression;
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
                    ArgumentHelpName = "path",
                },
                new Option<string>(new string[] { "-c", "--compression" }, "Set the compression format to use when compressing embedded PACs. If set to none, embedded PACs will not be compressed. If not set, defaults to lz4.\n\nAllowed values: none, deflate, lz4\n")
                {
                    ArgumentHelpName = "format",
                },
                new Option<bool>("--no-splits", "Do not split embedded PACs."),
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
#if DEBUG
                    console.Error.Write(e + Environment.NewLine);
#else
                    console.Error.Write(e.Message + Environment.NewLine);
#endif
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
                    // Get the dependency path or map the alias to a dependency path.
                    string dependencyPath;
                    if (dependency.Equals("ui_cutin_cmn", StringComparison.OrdinalIgnoreCase))
                    {
                        dependencyPath = @"ui\ui_cutin_cmn";
                    }
                    else if (dependency.Equals("ui_system", StringComparison.OrdinalIgnoreCase))
                    {
                        dependencyPath = @"ui\ui_system";
                    }
                    else if (dependency.Equals("ui_adv_area_common", StringComparison.OrdinalIgnoreCase))
                    {
                        dependencyPath = @"ui\adventure\ui_adv_area_common";
                    }
                    else if (dependency.Equals("ui_resident", StringComparison.OrdinalIgnoreCase))
                    {
                        dependencyPath = @"ui\ui_resident";
                    }
                    else
                    {
                        dependencyPath = dependency.Replace('/', '\\');
                        dependencyPath = Path.ChangeExtension(dependencyPath, null);
                    }

                    // Add the dependency and any dependents.
                    dependencies.Add(dependencyPath);
                    if (dependencyPath.Equals(@"ui\ui_cutin_cmn", StringComparison.OrdinalIgnoreCase))
                    {
                        dependencies.Add(@"ui\ui_resident");
                        dependencies.Add(@"ui\ui_system");
                    }
                    else if (dependencyPath.Equals(@"ui\ui_system", StringComparison.OrdinalIgnoreCase))
                    {
                        dependencies.Add(@"ui\ui_resident");
                    }
                    else if (dependencyPath.Equals(@"ui\adventure\ui_adv_area_common", StringComparison.OrdinalIgnoreCase))
                    {
                        dependencies.Add(@"ui\ui_resident");
                    }
                }
            }

            // If no dependencies were provided, try to determine them by the output file name
            else if (!options.NoAutoDependencies)
            {
                var outputFilename = Path.GetFileName(options.Output);
                var autoDependencies = Dependency.GetDependencies(outputFilename);
                if (autoDependencies is not null)
                {
                    dependencies.AddRange(autoDependencies);
                }
            }

            // Remove duplicate dependencies and order them.
            dependencies = dependencies
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            // Get the compression format to use.
            var compressionFormat = PacCompressionFormat.Lz4;
            if (string.Equals(options.Compression, "deflate", StringComparison.OrdinalIgnoreCase))
            {
                compressionFormat = PacCompressionFormat.Deflate;
            }
            else if (string.Equals(options.Compression, "none", StringComparison.OrdinalIgnoreCase))
            {
                compressionFormat = PacCompressionFormat.None;
            }

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
                compressionFormat: compressionFormat,
                splitCount: options.NoSplits ? null : ForcesArchive.DefaultSplitSize,
                compressSize: compressionFormat == PacCompressionFormat.None ? null : ForcesArchive.DefaultCompressSize);
        }

        static void Extract(ExtractCommandOptions options)
        {
            var archive = new ForcesArchive();
            archive.Load(options.Input.FullName);
            archive.Extract(options.Output ?? Environment.CurrentDirectory);
        }
    }
}
