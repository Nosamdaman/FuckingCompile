using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace jfc {
    /// <summary> Scans the document for tokens </summary>
    public class Scanner : IDisposable {
        private readonly StreamReader _sr;
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
            _sr = new StreamReader(fs, leaveOpen: true);
        }

        /// <summary> Reads until resolving the next token and returns it </summary>
        /// <returns> The next token in the program </returns>
        public Token Scan() {
            // The first step is to get the current character in the file. If it is -1, we are at the end of the file.
            int cur = _sr.Read();
            if (cur == -1) return new(TokenType.EOF);

            // If we have a whitespace, we'll try again with the next character
            if (cur == ' ' || cur == '\t' || cur == '\r' || cur == '\n') {
                if (cur == '\n') _lineCount++;
                return Scan();
            }

            // These tokens can be returned from just a single match
            if (cur == '.') return new(TokenType.PERIOD);
            if (cur == ';') return new(TokenType.SEMICOLON);
            if (cur == ',') return new(TokenType.COMMA);
            if (cur == '(') return new(TokenType.L_PAREN);
            if (cur == ')') return new(TokenType.R_PAREN);
            if (cur == '[') return new(TokenType.L_BRACKET);
            if (cur == ']') return new(TokenType.R_BRACKET);
            if (cur == '&') return new(TokenType.AND);
            if (cur == '|') return new(TokenType.OR);
            if (cur == '+') return new(TokenType.PLUS);
            if (cur == '-') return new(TokenType.MINUS);
            if (cur == '*') return new(TokenType.TIMES);

            // These tokens require some basic look-ahead to determine their exact type
            if (cur == ':') {
                // This could be a colon or an assignment operator
                if (_sr.Peek() == '=') {
                    _sr.Read();
                    return new(TokenType.ASSIGN);
                } else {
                    return new(TokenType.COLON);
                }
            }
            if (cur == '<') {
                // This could be less-than or less-than-or-equal-to
                if (_sr.Peek() == '=') {
                    _sr.Read();
                    return new(TokenType.LT_EQ);
                } else {
                    return new(TokenType.LT);
                }
            }
            if (cur == '>') {
                // This could be greater-than or greater-than-or-equal-to
                if (_sr.Peek() == '=') {
                    _sr.Read();
                    return new(TokenType.GT_EQ);
                } else {
                    return new Token(TokenType.GT);
                }
            }
            if (cur == '=') {
                // This must be followed by another equals sign
                if (_sr.Peek() == '=') {
                    _sr.Read();
                    return new(TokenType.EQ);
                }
            }
            if (cur == '!') {
                // This must be followed by an equals sign
                if (_sr.Peek() == '=') {
                    _sr.Read();
                    return new(TokenType.NEQ);
                }
            }

            // Now we'll handle number literals
            if (cur >= '0' && cur <= '9') {
                // Build up the number
                StringBuilder sb = new();
                sb.Append((char) cur);

                // Read until we hit a non-numeric character
                while (_sr.Peek() >= 0 && _sr.Peek() <= 9) sb.Append((char) _sr.Read());

                // If we have a period, then it's a float, otherwise, it's an integer
                if (_sr.Peek() == '.') {
                    _sr.Read();
                    sb.Append('.');
                    while (_sr.Peek() >= 0 && _sr.Peek() <= 9) sb.Append((char) _sr.Read());
                    return new(TokenType.FLOAT, double.Parse(sb.ToString()));
                } else {
                    return new(TokenType.INTEGER, int.Parse(sb.ToString()));
                }
            }

            // Now we'll handle string literals
            if (cur == '"') {
                StringBuilder sb = new();
                bool endOfString = false;
                while (!endOfString) {
                    cur = _sr.Read();
                    switch (cur) {
                    case -1:
                        return new(TokenType.EOF);
                    case '"':
                        endOfString = true;
                        break;
                    case '\n':
                        _lineCount++;
                        sb.Append((char) cur);
                        break;
                    default:
                        sb.Append((char) cur);
                        break;
                    }
                }
                return new(TokenType.STRING, sb.ToString());
            }

            // Now we'll handle the "/" symbol. This can either mean division or be the start of a comment.
            if (cur == '/') {
                if (_sr.Peek() == '/') {
                    // We'll continue to read until we reach a newline or EOF, at which point we'll start over
                    _sr.Read();
                    while(_sr.Peek() != '\n' || _sr.Peek() != -1) _sr.Read();
                    return Scan();
                } else if (_sr.Peek() == '*') {
                    // We'll read until we reach the escape comment symbol, making sure to track nesting
                    _sr.Read();
                    int commentDepth = 1;
                    while (commentDepth > 0) {
                        switch (_sr.Read()) {
                        case -1:
                            return new(TokenType.EOF);
                        case '\n':
                            _lineCount++;
                            break;
                        case '*':
                            // This may be the an escape sequence
                            if (_sr.Peek() == '/') {
                                _sr.Read();
                                commentDepth--;
                            }
                            break;
                        case '/':
                            // This may cause a new comment depth level to be added
                            if (_sr.Peek() == '*') {
                                _sr.Read();
                                commentDepth++;
                            }
                            break;
                        }
                    }
                    return Scan();
                } else {
                    return new(TokenType.DIVIDE);
                }
            }

            // If we have any alphabetical character, it could be an identifier or a keyword
            if ((cur >= 'A' && cur <= 'Z') || (cur >= 'a' && cur <= 'z')) {
                // Build up the identifier string
                StringBuilder sb = new();
                sb.Append((char) cur);

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

            // If we've gotten this far, then the character is illegal. We'll send a message saying such and try again.
            // TODO: ADD ERROR REPORTING
            return Scan();
        }
    }
}
