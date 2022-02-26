using System;
using System.Text;

namespace jfc {
    /// <summary> Scans the document for tokens </summary>
    public class Scanner {
        private readonly SourceFileReader _src;

        /// <summary> Creates a new scanner on the given file </summary>
        /// <param name="src"> The source code </param>
        /// <exception cref="ArgumentNullException"/>
        public Scanner(SourceFileReader src) {
            if (src is null) throw new ArgumentNullException(nameof(src));
            _src = src;
        }

        /// <summary> Reads until resolving the next token and returns it </summary>
        /// <returns> The next token in the program </returns>
        public Token Scan() {
            // The first step is to get the current character in the file. If it is -1, we are at the end of the file.
            int cur = _src.Read();
            if (cur == -1) return new(TokenType.EOF);

            // If we have a whitespace, we'll try again with the next character
            if (cur == ' ' || cur == '\t' || cur == '\r' || cur == '\n') {
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
                if (_src.Peek() == '=') {
                    _src.Read();
                    return new(TokenType.ASSIGN);
                } else {
                    return new(TokenType.COLON);
                }
            }
            if (cur == '<') {
                // This could be less-than or less-than-or-equal-to
                if (_src.Peek() == '=') {
                    _src.Read();
                    return new(TokenType.LT_EQ);
                } else {
                    return new(TokenType.LT);
                }
            }
            if (cur == '>') {
                // This could be greater-than or greater-than-or-equal-to
                if (_src.Peek() == '=') {
                    _src.Read();
                    return new(TokenType.GT_EQ);
                } else {
                    return new Token(TokenType.GT);
                }
            }
            if (cur == '=') {
                // This must be followed by another equals sign
                if (_src.Peek() == '=') {
                    _src.Read();
                    return new(TokenType.EQ);
                }
            }
            if (cur == '!') {
                // This must be followed by an equals sign
                if (_src.Peek() == '=') {
                    _src.Read();
                    return new(TokenType.NEQ);
                }
            }

            // Now we'll handle number literals
            if (cur >= '0' && cur <= '9') {
                // Build up the number
                StringBuilder sb = new();
                sb.Append((char) cur);

                // Read until we hit a non-numeric character
                while (_src.Peek() >= '0' && _src.Peek() <= '9') sb.Append((char) _src.Read());

                // If we have a period, then it's a float, otherwise, it's an integer
                if (_src.Peek() == '.') {
                    _src.Read();
                    sb.Append('.');
                    while (_src.Peek() >= '0' && _src.Peek() <= '9') sb.Append((char) _src.Read());
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
                    cur = _src.Read();
                    switch (cur) {
                    case -1:
                        return new(TokenType.EOF);
                    case '"':
                        endOfString = true;
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
                if (_src.Peek() == '/') {
                    // We'll continue to read until we reach a newline or EOF, at which point we'll start over
                    _src.Read();
                    while(_src.Peek() != '\n' && _src.Peek() != -1) _src.Read();
                    return Scan();
                } else if (_src.Peek() == '*') {
                    // We'll read until we reach the escape comment symbol, making sure to track nesting
                    _src.Read();
                    int commentDepth = 1;
                    while (commentDepth > 0) {
                        switch (_src.Read()) {
                        case -1:
                            return new(TokenType.EOF);
                        case '*':
                            // This may be the an escape sequence
                            if (_src.Peek() == '/') {
                                _src.Read();
                                commentDepth--;
                            }
                            break;
                        case '/':
                            // This may cause a new comment depth level to be added
                            if (_src.Peek() == '*') {
                                _src.Read();
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
                while ((_src.Peek() >= 'A' && _src.Peek() <= 'Z') ||
                       (_src.Peek() >= 'a' && _src.Peek() <= 'z') ||
                       (_src.Peek() >= '0' && _src.Peek() <= '9') ||
                       _src.Peek() == '_') {
                    sb.Append((char) _src.Read());
                }
                string identifier = sb.ToString();

                // Now we just need to return the reserved word or identifier
                Token token = identifier.ToUpper() switch {
                    "PROGRAM"   => new(TokenType.PROGRAM_RW),
                    "IS"        => new(TokenType.IS_RW),
                    "BEGIN"     => new(TokenType.BEGIN_RW),
                    "END"       => new(TokenType.END_RW),
                    "GLOBAL"    => new(TokenType.GLOBAL_RW),
                    "PROCEDURE" => new(TokenType.PROCEDURE_RW),
                    "VARIABLE"  => new(TokenType.VARIABLE_RW),
                    "INTEGER"   => new(TokenType.INTEGER_RW),
                    "FLOAT"     => new(TokenType.FLOAT_RW),
                    "STRING"    => new(TokenType.STRING_RW),
                    "BOOL"      => new(TokenType.BOOL_RW),
                    "IF"        => new(TokenType.IF_RW),
                    "THEN"      => new(TokenType.THEN_RW),
                    "ELSE"      => new(TokenType.ELSE_RW),
                    "FOR"       => new(TokenType.FOR_RW),
                    "RETURN"    => new(TokenType.RETURN_RW),
                    "NOT"       => new(TokenType.NOT_RW),
                    "TRUE"      => new(TokenType.TRUE_RW),
                    "FALSE"     => new(TokenType.FALSE_RW),
                    _           => new(TokenType.IDENTIFIER, identifier)
                };
                return token;
            }

            // If we've gotten this far, then the character is illegal. We'll send a message saying such and try again.
            _src.Report(MsgLevel.ERROR, $"Illegal character \"{(char) cur}\"", true);
            return Scan();
        }
    }
}
