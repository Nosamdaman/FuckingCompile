using System;
using System.Collections.Generic;

namespace jfc {
    /// <summary> The main entry-point class for the application </summary>
    class Program {
        private const string _helpText =
            "Just Fucking Compile - a compiler by Mason Davy\n\n" +
            "Usage:\n" +
            "  jfc [options...] SOURCE\n" +
            "  jfc -h | --help\n" +
            "  jfc -v | --version\n\n" +
            "Options:\n" +
            "  -o --output OUTPUT    Set the output file for a successful compilation.\n" + 
            "                        Defaults to \"a.out\".\n" +
            "  -V --verbosity LEVEL  Sets the verbosity level. Can be one of \"0\", \"1\", or\n" +
            "                        \"2\". Defaults to \"0\".\n" +
            "  -h --help             Show this screen\n" +
            "  -v --version          Show version information\n";
        private const string _versionText = "v0.2.0";

        /// <summary> Main entry-point function </summary>
        /// <param name="args"> Ordered list of all command-line arguments passed in </param>
        static void Main(string[] args) {
            // Initialize parameters
            string sourceFile = null;
            string outputFile = "a.out";
            MsgLevel verbosity = MsgLevel.INFO;

            // We'll parse most of the inputs in a loop here
            Queue<string> argQueue = new(args);
            while (argQueue.Count > 0) {
                string arg = argQueue.Dequeue();

                // First we'll handle any full-length options
                if (arg == "--help") {
                    Console.WriteLine(_helpText);
                    return;
                } else if (arg == "--version") {
                    Console.WriteLine(_versionText);
                    return;
                } else if (arg == "--verbosity") {
                    if (argQueue.Count == 0) {
                        Console.WriteLine(" WARN: No verbosity level specified after \"--verbosity\"");
                        continue;
                    }
                    string level = argQueue.Dequeue();
                    switch (level) {
                    case "0":
                        verbosity = MsgLevel.INFO;
                        break;
                    case "1":
                        verbosity = MsgLevel.DEBUG;
                        break;
                    case "2":
                        verbosity = MsgLevel.TRACE;
                        break;
                    default:
                        Console.WriteLine($" WARN: Ignoring invalid verbosity level \"{level}\". " +
                                            "Valid levels are \"0\", \"1\", and \"2\"");
                        break;
                    }
                    continue;
                } else if (arg == "--output") {
                    if (argQueue.Count == 0) {
                        Console.WriteLine(" WARN: No output file specified after \"--output\"");
                        continue;
                    }
                    outputFile = argQueue.Dequeue();
                    continue;
                }

                // Next we'll try to parse an argument block
                if (arg[0] == '-' && arg.Length > 1) {
                    Queue<char> optionQueue = new(arg[1..]);
                    while (optionQueue.Count > 0) {
                        char option = optionQueue.Dequeue();
                        switch (option) {
                        case 'h':
                            Console.WriteLine(_helpText);
                            return;
                        case 'v':
                            Console.WriteLine(_versionText);
                            return;
                        case 'V':
                            verbosity = MsgLevel.DEBUG;
                            break;
                        case 'o':
                            if (optionQueue.Count > 0) {
                                outputFile = new string(optionQueue.ToArray());
                                optionQueue.Clear();
                            } else {
                                if (argQueue.Count == 0) {
                                    Console.WriteLine(" WARN: No output file specified after \"--output\"");
                                    continue;
                                }
                                outputFile = argQueue.Dequeue();
                            }
                            break;
                        default:
                            Console.WriteLine($" WARN: Ignoring unkown switch \"{option}\"");
                            break;
                        }
                    }
                    continue;
                }

                // If this is the last argument, then it's the source file
                if (argQueue.Count == 0) {
                    sourceFile = arg;
                    continue;
                }

                // Otherwise, the option is bad
                Console.WriteLine($" WARN: Ignoring unkown option \"{arg}\"");
            }

            // Get the source file
            if (sourceFile == null) {
                Console.WriteLine("ERROR: No source file specified");
                Console.WriteLine(_helpText);
                Environment.ExitCode = 0;
                return;
            }

            // Print starting information
            Console.WriteLine("Just Fucking Compile " + _versionText);
            Console.WriteLine($"| Source File: \"{sourceFile}\"");
            Console.WriteLine($"| Output File: \"{outputFile}\"");
            Console.WriteLine($"|   Verbosity: {verbosity}\n");

            // Try to open the indicated file
            SourceFileReader src;
            try {
                src = new(sourceFile);
                src.MinReportLevel = verbosity;
            } catch (Exception) {
                Environment.ExitCode = 1;
                return;
            }

            // Test code, we'll run the parser
            Parser parser = new(src);
            ParseInfo status = parser.Program();
            if (!status.Success) {
                src.Report(MsgLevel.ERROR, "Unable to successfully parse the program, aborting execution");
                Environment.ExitCode = 1;
                src.Dispose();
                return;
            }

            // Close the file
            src.Dispose();
        }
    }
}
