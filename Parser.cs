using System;

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
        }

        private Token NextToken() {
            _curToken = _scanner.Scan();
            return _curToken;
        }

        private ParseInfo Expression() {
            // First we expect an arithmetic operation
            ParseInfo status = ArithOp();
            if (!status.Success) {
                _src.Report(MsgLevel.ERROR, "Expected an arithmetic operation", true);
                return new(false);
            }

            // Then we see how many operations to chain together
            return ExpressionPrime();
        }

        private ParseInfo ExpressionPrime() {
            // First check if we have a logical operator
            char symbol;
            if (_curToken.TokenType == TokenType.AND) {
                symbol = '&';
            } else if (_curToken.TokenType == TokenType.OR) {
                symbol = '|';
            } else {
                return new(true);
            }
            NextToken();

            // Then we expect another arithmetic operation
            ParseInfo status = ArithOp();
            if (!status.Success) {
                _src.Report(MsgLevel.ERROR, $"Expected an arithmetic operation after \"{symbol}\"", true);
                return new(false);
            }

            // Then we do it all over again
            return ExpressionPrime();
        }

        private ParseInfo ArithOp() {
            // First we expect a relation
            ParseInfo status = Relation();
            if (!status.Success) {
                _src.Report(MsgLevel.ERROR, "Expected a relation", true);
                return new(false);
            }

            // Then we see how many relations we need to chain together
            return ArithOpPrime();
        }

        private ParseInfo ArithOpPrime() {
            // First check if we have an arithmatic operator
            char symbol;
            if (_curToken.TokenType == TokenType.PLUS) {
                symbol = '+';
            } else if (_curToken.TokenType == TokenType.MINUS) {
                symbol = '-';
            } else {
                return new(true);
            }
            NextToken();

            // Then we expect another relation
            ParseInfo status = Relation();
            if (!status.Success) {
                _src.Report(MsgLevel.ERROR, $"Expected a relation after \"{symbol}\"", true);
                return new(false);
            }

            // Then we go again
            return ArithOpPrime();
        }

        private ParseInfo Relation() {
            // First we expect a term
            ParseInfo status = Term();
            if (!status.Success) {
                _src.Report(MsgLevel.ERROR, "Expected a term", true);
                return new(false);
            }

            // Then we see how many terms we need to chain together
            return RelationPrime();
        }

        private ParseInfo RelationPrime() {
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
                return new(true);
            }
            NextToken();

            // If so, we expect another term
            ParseInfo status = Term();
            if (!status.Success) {
                _src.Report(MsgLevel.ERROR, $"Expected a term after \"{symbol}\"", true);
                return new(false);
            }

            // Now we go again
            return RelationPrime();
        }

        private ParseInfo Term() {
            // First we expect a factor
            ParseInfo status = Factor();
            if (!status.Success) {
                _src.Report(MsgLevel.ERROR, "Expected a factor", true);
                return new(false);
            }

            // Then we see how many factors we need to chain together
            return TermPrime();
        }

        private ParseInfo TermPrime() {
            // First check if we have a multiplication operation
            char symbol;
            if (_curToken.TokenType == TokenType.TIMES) {
                symbol = '*';
            } else if (_curToken.TokenType == TokenType.DIVIDE) {
                symbol = '/';
            } else {
                return new(true);
            }
            NextToken();

            // If so, we expect another factor
            ParseInfo status = Factor();
            if (!status.Success) {
                _src.Report(MsgLevel.ERROR, $"Expected a factor after \"{symbol}\"", true);
                return new(false);
            }

            // Now we go again
            return TermPrime();
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
                return new(true);
            }

            // We can accept numbers
            if (_curToken.TokenType == TokenType.INTEGER || _curToken.TokenType == TokenType.FLOAT) {
                NextToken();
                return new(true);
            }

            // We can accept strings
            if (_curToken.TokenType == TokenType.STRING) {
                NextToken();
                return new(true);
            }

            // We can accept boolean literals
            if (_curToken.TokenType == TokenType.TRUE_RW || _curToken.TokenType == TokenType.FALSE_RW) {
                NextToken();
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
