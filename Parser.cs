using System;
using System.Text;

namespace jfc {
    public struct ParseInfo {
        public bool Success { get; set; }

        public ParseInfo(bool success) {
            Success = success;
        }
    }

    /// <summary> Parses the program </summary>
    public class Parser {
        private readonly SourceFileReader _src;
        private readonly Scanner _scanner;
        private Token _curToken = null;

        /// <summary> Creates a new parser on the given file </summary>
        /// <param name="src"> The  source code </param>
        /// <exception cref="ArgumentNullException"/>
        public Parser(SourceFileReader src) {
            _src = src;
            _scanner = new Scanner(src);
            NextToken();
        }

        private Token NextToken() {
            _curToken = _scanner.Scan();
            return _curToken;
        }

        public ParseInfo ExpressionList() {
            // TEMPORARY - DELETE THIS
            if (_curToken.TokenType == TokenType.EOF) { return new(true); }
            // END DELETE THIS

            // First we expect an expression
            ParseInfo status = Expression();
            if (!status.Success) {
                _src.Report(MsgLevel.ERROR, "Expected an expression", true);
                return new(false);
            }

            // Then we expect a semi-colon
            if (_curToken.TokenType != TokenType.SEMICOLON) {
                _src.Report(MsgLevel.ERROR, "Expected a \";\" after an expression", true);
                return new(false);
            }
            NextToken();

            // Then we do it all again
            return ExpressionList();
        }

        private ParseInfo Expression() {
            // First check for a "not"
            StringBuilder sb = new();
            sb.Append("Parsed expression as");
            if (_curToken.TokenType == TokenType.NOT_RW) {
                NextToken();
                sb.Append(" not");
            }
            sb.Append(" arithmetic operation");

            // Then we expect an arithmetic operation
            ParseInfo status = ArithOp();
            if (!status.Success) {
                _src.Report(MsgLevel.ERROR, "Expected an arithmetic operation", true);
                return new(false);
            }

            // Then we see how many operations to chain together
            return ExpressionPrime(sb);
        }

        private ParseInfo ExpressionPrime(StringBuilder sb) {
            // First check if we have a logical operator
            char symbol;
            if (_curToken.TokenType == TokenType.AND) {
                symbol = '&';
            } else if (_curToken.TokenType == TokenType.OR) {
                symbol = '|';
            } else {
                _src.Report(MsgLevel.DEBUG, sb.ToString(), true);
                return new(true);
            }
            NextToken();
            sb.Append($" {symbol}");

            // Then check for a "not"
            if (_curToken.TokenType == TokenType.NOT_RW) {
                NextToken();
                sb.Append(" not");
            }
            sb.Append(" arithmetic operation");

            // Then we expect another arithmetic operation
            ParseInfo status = ArithOp();
            if (!status.Success) {
                _src.Report(MsgLevel.ERROR, $"Expected an arithmetic operation after \"{symbol}\"", true);
                return new(false);
            }

            // Then we do it all over again
            return ExpressionPrime(sb);
        }

        private ParseInfo ArithOp() {
            // First we expect a relation
            ParseInfo status = Relation();
            if (!status.Success) {
                _src.Report(MsgLevel.ERROR, "Expected a relation", true);
                return new(false);
            }

            // Then we see how many relations we need to chain together
            StringBuilder sb = new();
            sb.Append("Parsed arithmetic operation as relation");
            return ArithOpPrime(sb);
        }

        private ParseInfo ArithOpPrime(StringBuilder sb) {
            // First check if we have an arithmatic operator
            char symbol;
            if (_curToken.TokenType == TokenType.PLUS) {
                symbol = '+';
            } else if (_curToken.TokenType == TokenType.MINUS) {
                symbol = '-';
            } else {
                _src.Report(MsgLevel.DEBUG, sb.ToString(), true);
                return new(true);
            }
            NextToken();
            sb.Append($" {symbol} relation");

            // Then we expect another relation
            ParseInfo status = Relation();
            if (!status.Success) {
                _src.Report(MsgLevel.ERROR, $"Expected a relation after \"{symbol}\"", true);
                return new(false);
            }

            // Then we go again
            return ArithOpPrime(sb);
        }

        private ParseInfo Relation() {
            // First we expect a term
            ParseInfo status = Term();
            if (!status.Success) {
                _src.Report(MsgLevel.ERROR, "Expected a term", true);
                return new(false);
            }

            // Then we see how many terms we need to chain together
            StringBuilder sb = new();
            sb.Append("Parsed relation as term");
            return RelationPrime(sb);
        }

        private ParseInfo RelationPrime(StringBuilder sb) {
            // First check if we have a comparison operator
            string symbol;
            switch (_curToken.TokenType) {
            case TokenType.EQ:
                symbol = "==";
                break;
            case TokenType.NEQ:
                symbol = "!=";
                break;
            case TokenType.GT:
                symbol = ">";
                break;
            case TokenType.GT_EQ:
                symbol = ">=";
                break;
            case TokenType.LT:
                symbol = "<";
                break;
            case TokenType.LT_EQ:
                symbol = "<=";
                break;
            default:
                _src.Report(MsgLevel.DEBUG, sb.ToString(), true);
                return new(true);
            }
            NextToken();
            sb.Append($" {symbol} term");

            // If so, we expect another term
            ParseInfo status = Term();
            if (!status.Success) {
                _src.Report(MsgLevel.ERROR, $"Expected a term after \"{symbol}\"", true);
                return new(false);
            }

            // Now we go again
            return RelationPrime(sb);
        }

        private ParseInfo Term() {
            // First we expect a factor
            ParseInfo status = Factor();
            if (!status.Success) {
                _src.Report(MsgLevel.ERROR, "Expected a factor", true);
                return new(false);
            }

            // Then we see how many factors we need to chain together
            StringBuilder sb = new();
            sb.Append("Parsed term as factor");
            return TermPrime(sb);
        }

        private ParseInfo TermPrime(StringBuilder sb) {
            // First check if we have a multiplication operation
            char symbol;
            if (_curToken.TokenType == TokenType.TIMES) {
                symbol = '*';
            } else if (_curToken.TokenType == TokenType.DIVIDE) {
                symbol = '/';
            } else {
                _src.Report(MsgLevel.DEBUG, sb.ToString(), true);
                return new(true);
            }
            NextToken();
            sb.Append($" {symbol} factor");

            // If so, we expect another factor
            ParseInfo status = Factor();
            if (!status.Success) {
                _src.Report(MsgLevel.ERROR, $"Expected a factor after \"{symbol}\"", true);
                return new(false);
            }

            // Now we go again
            return TermPrime(sb);
        }

        private ParseInfo Factor() {
            // The first step is to check if we have have a left parens. If so, we should have a nested expression
            // statement.
            if (_curToken.TokenType == TokenType.L_PAREN) {
                NextToken();
                ParseInfo status = Expression();
                if (!status.Success) {
                    _src.Report(MsgLevel.ERROR, "Expression expected after \"(\"", true);
                    return new(false);
                }
                if (_curToken.TokenType != TokenType.R_PAREN) {
                    _src.Report(MsgLevel.ERROR, "\")\" expected after expression", true);
                    return new(false);
                }
                NextToken();
                _src.Report(MsgLevel.DEBUG, "Parsed factor as a nested expression", true);
                return new(true);
            }

            // If that fails, we'll see if we have a procedure or name. If either is the case, then we'll need to see an
            // identifier first
            if (_curToken.TokenType == TokenType.IDENTIFIER) {
                // For now we'll just assume that it could be either one, and not worry about the symbol table
                NextToken();

                // If we have a procedure, then we expect expect an argument list in parens
                if (_curToken.TokenType == TokenType.L_PAREN) {
                    NextToken();

                    // The list could be empty
                    if (_curToken.TokenType == TokenType.R_PAREN) {
                        NextToken();
                        return new(true);
                    }

                    // Or it could have arguments
                    ParseInfo status = ArgumentList();
                    if (!status.Success) {
                        _src.Report(MsgLevel.ERROR, "Argument list expected after \"(\"", true);
                        return new(false);
                    }

                    // Either way, it needs to end with a right parens
                    if (_curToken.TokenType != TokenType.R_PAREN) {
                        _src.Report(MsgLevel.ERROR, "\")\" expected after argument list", true);
                        return new(false);
                    }
                    NextToken();
                    _src.Report(MsgLevel.DEBUG, "Parsed factor as procedure call", true);
                    return new(true);
                }

                // If we have a name, then we might have an indexing operation
                if (_curToken.TokenType == TokenType.L_BRACKET) {
                    NextToken();
                    ParseInfo status = Expression();
                    if (!status.Success) {
                        _src.Report(MsgLevel.ERROR, "Expression expected after \"[\"", true);
                        return new(false);
                    }
                    if (_curToken.TokenType != TokenType.R_BRACKET) {
                        _src.Report(MsgLevel.ERROR, "\"]\" expected after expression", true);
                        return new(false);
                    }
                    NextToken();
                    _src.Report(MsgLevel.DEBUG, "Parsed factor as name with indexing", true);
                } else {
                    _src.Report(MsgLevel.DEBUG, "Parsed factor as name", true);
                }

                // Either way, we should be good here
                return new(true);
            }

            // If we have a minus symbol, then we could have either a name or a number
            if (_curToken.TokenType == TokenType.MINUS) {
                NextToken();

                // If we see an identifier, it's a name
                if (_curToken.TokenType == TokenType.IDENTIFIER) {
                    NextToken();

                    // If we have a name, then we might have an indexing operation
                    if (_curToken.TokenType == TokenType.L_BRACKET) {
                        NextToken();
                        ParseInfo status = Expression();
                        if (!status.Success) {
                            _src.Report(MsgLevel.ERROR, "Expression expected after \"[\"", true);
                            return new(false);
                        }
                        if (_curToken.TokenType != TokenType.R_BRACKET) {
                            _src.Report(MsgLevel.ERROR, "\"]\" expected after expression", true);
                            return new(false);
                        }
                        NextToken();
                        _src.Report(MsgLevel.DEBUG, "Parsed factor as minus name with indexing", true);
                    } else {
                        _src.Report(MsgLevel.DEBUG, "Parsed factor as minus name", true);
                    }

                    // We should be good here
                    return new(true);
                }

                // Otherwise, it should be a number
                if (_curToken.TokenType != TokenType.INTEGER || _curToken.TokenType != TokenType.FLOAT) {
                    _src.Report(MsgLevel.ERROR, "Name or number expected after \"-\"", true);
                    return new(false);
                }
                NextToken();
                _src.Report(MsgLevel.DEBUG, "Parsed factor as minus number literal", true);
                return new(true);
            }

            // We can accept numbers
            if (_curToken.TokenType == TokenType.INTEGER || _curToken.TokenType == TokenType.FLOAT) {
                NextToken();
                _src.Report(MsgLevel.DEBUG, "Parsed factor as number literal", true);
                return new(true);
            }

            // We can accept strings
            if (_curToken.TokenType == TokenType.STRING) {
                NextToken();
                _src.Report(MsgLevel.DEBUG, "Parsed factor as string literal", true);
                return new(true);
            }

            // We can accept boolean literals
            if (_curToken.TokenType == TokenType.TRUE_RW || _curToken.TokenType == TokenType.FALSE_RW) {
                NextToken();
                _src.Report(MsgLevel.DEBUG, "Parsed factor as boolean literal", true);
                return new(true);
            }

            // Anything else is unacceptable
            return new(false);
        }

        private ParseInfo ArgumentList() {
            // We should have an expression first
            ParseInfo status = Expression();
            if (!status.Success) {
                _src.Report(MsgLevel.ERROR, "Expression expected in argument list", true);
                return new(false);
            }

            // If a comma comes next, then we do it again, otherwise, we're good
            if (_curToken.TokenType == TokenType.COMMA) {
                NextToken();
                return ArgumentList();
            }
            return new(true);
        }
    }
}
