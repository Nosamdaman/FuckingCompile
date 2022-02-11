using System;
using System.IO;

namespace jfc {
    /// <summary> Parses the program </summary>
    public class Parser : IDisposable {
        private readonly Scanner _scanner;
        private Token _curToken = null;

        /// <summary> Releases all managed resources </summary>
        public void Dispose() {
            _scanner.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary> Creates a new parser on the given file </summary>
        /// <param name="fs"> The file stream containing the source code </param>
        /// <exception cref="ArgumentNullException"/>
        public Parser(FileStream fs) {
            _scanner = new Scanner(fs);
        }

        private Token NextToken() {
            _curToken = _scanner.Scan();
            return _curToken;
        }

        private bool Factor() {
            // For now we'll just worry about strings, trues, and falses
            if (_curToken.TokenType == TokenType.STRING) {
                Console.WriteLine($"String Literal: \"{_curToken.TokenMark}\"");
            } else if (_curToken.TokenType == TokenType.TRUE_RW) {
                Console.WriteLine("Reserved Word: TRUE");
            } else if (_curToken.TokenType == TokenType.FALSE_RW) {
                Console.WriteLine("Reserved Word: FALSE");
            } else {
                Console.WriteLine("ERROR: Invalid factor");
                return false;
            }
            NextToken();
            return true;
        }

        private bool Term() {
            // First we expect a factor
            if (!Factor()) {
                Console.WriteLine("ERROR: Expected a factor at the start of a term");
                return false;
            }

            // Then we need to see how many operation are chained together
            return TermPrime();
        }

        private bool TermPrime() {
            // If we don't have a "*" or a "/", then we're good to go
            char symbol;
            if (_curToken.TokenType == TokenType.TIMES) {
                symbol = '*';
            } else if (_curToken.TokenType == TokenType.DIVIDE) {
                symbol = '/';
            } else {
                return true;
            }

            // Otherwise, we need the next token(s) to be a factor
            NextToken();
            if (!Factor()) {
                Console.WriteLine($"ERROR: Expected a factor to follow the \"{symbol}\" symbol");
                return false;
            }

            // Now we try again
            return TermPrime();
        }
    }
}
