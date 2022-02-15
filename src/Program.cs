using System;

namespace jfc {
    /// <summary> The main entry-point class for the application </summary>
    class Program {
        private const string _helpText =
            "Just Fucking Compile - a compiler by Mason Davy\n\n" +
            "Usage:\n" +
            "  jfc file\n" +
            "  jfc -h | --help\n" +
            "  jfc -v | --version\n\n" +
            "Options:\n" +
            "  -h --help     Show this screen\n" +
            "  -v --version  Show version information\n";
        private const string _versionText = "v0.1.2";

        /// <summary> Main entry-point function </summary>
        /// <param name="args"> Ordered list of all command-line arguments passed in </param>
        static void Main(string[] args) {
            // First check the number of argumnets
            if (args.Length == 0) {
                Console.WriteLine("ERROR: No file specified");
                Console.WriteLine(_helpText);
                Environment.ExitCode = 1;
                return;
            } else if (args.Length > 1) {
                Console.WriteLine(" WARN: Multiple arguments detected, only the first will be considered");
            }

            // Handle non source-file inputs
            if (args[0] == "-h" || args[0] == "--help") {
                Console.WriteLine(_helpText);
                return;
            } else if (args[0] == "-v" || args[0] == "--version") {
                Console.WriteLine(_versionText);
                return;
            }

            // Try to open the indicated file
            SourceFileReader src;
            try {
                src = new(args[0]);
            } catch (Exception) {
                Environment.ExitCode = 1;
                return;
            }

            // Test code, we'll dump everything the scanner sees here
            // Scanner scanner = new(src);
            // Token curToken = scanner.Scan();
            // while (curToken.TokenType != TokenType.EOF) {
            //     Console.WriteLine($"Line Count: {src.LineCount,3}   | Token Type: {curToken.TokenType,-15}| Token Mark: {curToken.TokenMark}");
            //     curToken = scanner.Scan();
            // }

            // Test code, we'll run the parser
            Parser parser = new(src);
            parser.ExpressionList();

            // Close the file
            src.Dispose();
        }
    }
}
