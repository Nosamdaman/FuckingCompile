using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace jfc {
    /// <summary> Scans the document for tokens </summary>
    public class Scanner : IDisposable {
        private readonly StreamReader _sr;
        private readonly ReadOnlyCollection<char> _whitespace = new List<char>() { ' ', '\t', '\r', '\n' }.AsReadOnly();
        private readonly Dictionary<string, Token> _symbolTable = new(new StringNoCaseComparer());
        private int _lineCount = 1;

        /// <summary> The current line in the file we are on </summary>
        public int LineCount { get => _lineCount; }

        /// <summary> The symbol table </summary>
        public Dictionary<string, Token> SymbolTable { get => _symbolTable; }

        /// <summary> Releases all managed resources </summary>
        public void Dispose() {
            _sr.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary> Creates a new scanner on the given file </summary>
        /// <param name="fs"> The file stream containing the source code </param>
        /// <exception cref="ArgumentNullException"/>
        public Scanner(FileStream fs) {
            if (fs is null) throw new ArgumentNullException(nameof(fs));
            _sr = new StreamReader(fs);
        }

        /// <summary> Reads until resolving the next token and returns it </summary>
        /// <returns> The next token in the program </returns>
        public Token Scan() {
            // The first step is to get the current character in the file. If it is -1, we are at the end of the file.
            int curInt = _sr.Read();
            if (curInt == -1) return new Token(TokenType.EOF);
            char cur = (char) curInt;

            // If we have a whitespace, we'll try again with the next character
            if (_whitespace.Contains(cur)) {
                if (cur == '\n') _lineCount++;
                return Scan();
            }

            // If we have any alphabetical character, it could be an identifier or a keyword
            if ((cur >= 'A' && cur <= 'Z') || (cur >= 'a' && cur <= 'z')) {
                // Build up the identifier string
                StringBuilder sb = new();
                sb.Append(cur);

                // Loop to extract the rest of the identifier
                while ((_sr.Peek() >= 'A' && _sr.Peek() <= 'Z') ||
                       (_sr.Peek() >= 'a' && _sr.Peek() <= 'z') ||
                       (_sr.Peek() >= '0' && _sr.Peek() <= '9') ||
                       _sr.Peek() == '_') {
                    sb.Append((char) _sr.Read());
                }
                string identifier = sb.ToString();

                // Now we just need to return the reserved word or identifier
                Token token;
                switch (identifier.ToUpper()) {
                case "PROGRAM":
                    token = new(TokenType.PROGRAM_RW);
                    break;
                case "IS":
                    token = new(TokenType.IS_RW);
                    break;
                case "BEGIN":
                    token = new(TokenType.BEGIN_RW);
                    break;
                case "END":
                    token = new(TokenType.END_RW);
                    break;
                case "GLOBAL":
                    token = new(TokenType.GLOBAL_RW);
                    break;
                case "PROCEDURE":
                    token = new(TokenType.PROCEDURE_RW);
                    break;
                case "VARIABLE":
                    token = new(TokenType.VARIABLE_RW);
                    break;
                case "INTEGER":
                    token = new(TokenType.INTEGER_RW);
                    break;
                case "FLOAT":
                    token = new(TokenType.FLOAT_RW);
                    break;
                case "STRING":
                    token = new(TokenType.STRING_RW);
                    break;
                case "BOOL":
                    token = new(TokenType.BOOL_RW);
                    break;
                case "IF":
                    token = new(TokenType.IF_RW);
                    break;
                case "THEN":
                    token = new(TokenType.THEN_RW);
                    break;
                case "ELSE":
                    token = new(TokenType.ELSE_RW);
                    break;
                case "FOR":
                    token = new(TokenType.FOR_RW);
                    break;
                case "RETURN":
                    token = new(TokenType.RETURN_RW);
                    break;
                case "NOT":
                    token = new(TokenType.NOT_RW);
                    break;
                case "TRUE":
                    token = new(TokenType.TRUE_RW);
                    break;
                case "FALSE":
                    token = new(TokenType.FALSE_RW);
                    break;
                default:
                    if (!_symbolTable.ContainsKey(identifier))
                        _symbolTable[identifier] = new(TokenType.IDENTIFIER, identifier);
                    token = _symbolTable[identifier];
                    break;
                }
                return token;
            }

            throw new NotImplementedException();
        }
    }
}
