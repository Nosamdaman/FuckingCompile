using System;
using System.Linq;
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

        private ParseInfo Declaration() {
            throw new NotImplementedException();
        }

        private ParseInfo ProcedureDeclaration() {
            // First we expect a procedure header
            ParseInfo status = ProcedureHeader();
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Procedure header expected at the start of a procedure declaration", true);
                return new(false);
            }

            // Then we expect a procedure body
            status = ProcedureBody();
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Procedure header expected after procedure header", true);
                return new(false);
            }

            // We should be good to go
            _src.Report(MsgLevel.DEBUG, "Parsed procedure declaration", true);
            return new(true);
        }

        private ParseInfo ProcedureHeader() {
            // First we expect the procedure keyword
            if (_curToken.TokenType != TokenType.PROCEDURE_RW) {
                _src.Report(MsgLevel.ERROR, "\"PROCEDURE\" expected at the start of a declaration", true);
                return new(false);
            }
            NextToken();

            // Then we expect an identifier
            if (_curToken.TokenType != TokenType.IDENTIFIER) {
                _src.Report(MsgLevel.ERROR, "Identifier expected after \"PROCEDURE\"", true);
                return new(false);
            }
            NextToken();

            // Then we expect a colon
            if (_curToken.TokenType != TokenType.COLON) {
                _src.Report(MsgLevel.ERROR, "\":\" expected after identifier", true);
                return new(false);
            }
            NextToken();

            // Then we expect a type mark
            ParseInfo status = TypeMark();
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Type mark expected after \":\"", true);
                return new(false);
            }

            // Then we need a left parens
            if (_curToken.TokenType != TokenType.L_PAREN) {
                _src.Report(MsgLevel.ERROR, "\"(\" expected after type mark", true);
                return new(false);
            }
            NextToken();

            // Then we need the parameter list
            if (_curToken.TokenType != TokenType.R_PAREN) {
                status = ParameterList();
                if (!status.Success) {
                    _src.Report(MsgLevel.DEBUG, "Parameter list expected after \"(\"", true);
                    return new(false);
                }
            }

            // And finally we need a right parens
            if (_curToken.TokenType != TokenType.R_PAREN) {
                _src.Report(MsgLevel.ERROR, "\")\" expected after parameter list", true);
                return new(false);
            }
            NextToken();

            // We should be good to go
            _src.Report(MsgLevel.DEBUG, "Parsed procedure header", true);
            return new(true);
        }

        private ParseInfo ProcedureBody() {
            throw new NotImplementedException();
        }

        private ParseInfo ParameterList() {
            throw new NotImplementedException();
        }

        private ParseInfo VariableDeclaration() {
            // First we expect the variable keyword
            if (_curToken.TokenType != TokenType.VARIABLE_RW) {
                _src.Report(MsgLevel.ERROR, "\"VARIABLE\" expected at the start of a declaration", true);
                return new(false);
            }
            NextToken();

            // Then we expect an identifier
            if (_curToken.TokenType != TokenType.IDENTIFIER) {
                _src.Report(MsgLevel.ERROR, "Identifier expected after \"VARIABLE\"", true);
                return new(false);
            }
            NextToken();

            // Then we expect a colon
            if (_curToken.TokenType != TokenType.COLON) {
                _src.Report(MsgLevel.ERROR, "\":\" expected after identifier", true);
                return new(false);
            }
            NextToken();

            // Then we expect a type mark
            ParseInfo status = TypeMark();
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Type mark expected after \":\"", true);
                return new(false);
            }

            // If we don't see a left bracket, then we're good to go
            if (_curToken.TokenType != TokenType.L_BRACKET) {
                _src.Report(MsgLevel.DEBUG, "Parsed variable declaration", true);
                return new(true);
            }
            NextToken();

            // Otherwise, we're looking for a number next
            if (_curToken.TokenType != TokenType.FLOAT || _curToken.TokenType != TokenType.INTEGER) {
                _src.Report(MsgLevel.ERROR, "Bound expected after \"[\"", true);
                return new(false);
            }
            NextToken();

            // Finally we're looking for a right bracket
            if (_curToken.TokenType != TokenType.R_BRACKET) {
                _src.Report(MsgLevel.ERROR, "\"]\" expected after bound", true);
                return new(false);
            }

            // We should be good to go
            _src.Report(MsgLevel.DEBUG, "Parsed variable declaration", true);
            return new(true);
        }

        private ParseInfo TypeMark() {
            // Check for a valid token
            if (_curToken.TokenType == TokenType.INTEGER_RW) {
                NextToken();
                return new(true);
            } else if (_curToken.TokenType == TokenType.FLOAT_RW) {
                NextToken();
                return new(true);
            } else if (_curToken.TokenType == TokenType.STRING_RW) {
                NextToken();
                return new(true);
            } else if (_curToken.TokenType == TokenType.BOOL_RW) {
                NextToken();
                return new(true);
            }

            // Otherwise error
            _src.Report(MsgLevel.ERROR, "Type Mark must be \"INTEGER\", \"FLOAT\", \"STRING\", or \"BOOL\"", true);
            return new(false);
        }

        private ParseInfo StatementList(TokenType[] exitTokens) {
            // Loop to look for statements
            while (!exitTokens.Contains(_curToken.TokenType)) {
                ParseInfo status = Statement();
                if (!status.Success) {
                    _src.Report(MsgLevel.DEBUG, "Expected a statement", true);
                    return new(false);
                }
                if (_curToken.TokenType != TokenType.SEMICOLON) {
                    _src.Report(MsgLevel.ERROR, "Expected \";\" after statement", true);
                    return new(false);
                }
                NextToken();
            }
            return new(true);
        }

        private ParseInfo Statement() {
            switch (_curToken.TokenType) {
            case TokenType.IDENTIFIER:
                return AssignmentStatement();
            case TokenType.IF_RW:
                return IfStatement();
            case TokenType.FOR_RW:
                return LoopStatement();
            case TokenType.RETURN_RW:
                return ReturnStatement();
            default:
                _src.Report(MsgLevel.ERROR, "Expected a statement", true);
                return new(false);
            }
        }

        private ParseInfo AssignmentStatement() {
            ParseInfo status;

            // We first need an identifier
            if (_curToken.TokenType != TokenType.IDENTIFIER) {
                _src.Report(MsgLevel.ERROR, "Expected an identifier as a destination", true);
                return new(false);
            }
            NextToken();

            // If we have a name, then we might have an indexing operation
            if (_curToken.TokenType == TokenType.L_BRACKET) {
                NextToken();
                status = Expression();
                if (!status.Success) {
                    _src.Report(MsgLevel.DEBUG, "Expression expected after \"[\"", true);
                    return new(false);
                }
                if (_curToken.TokenType != TokenType.R_BRACKET) {
                    _src.Report(MsgLevel.ERROR, "\"]\" expected after expression", true);
                    return new(false);
                }
                NextToken();
                _src.Report(MsgLevel.TRACE, "Parsed destination as name with indexing", true);
            } else {
                _src.Report(MsgLevel.TRACE, "Parsed destination as name", true);
            }

            // Next we need an assignment sign
            if (_curToken.TokenType != TokenType.ASSIGN) {
                _src.Report(MsgLevel.ERROR, "\":=\" expected after destination", true);
                return new(false);
            }
            NextToken();

            // Finally we need an expression
            status = Expression();
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Expected an expression after \":=\"", true);
                return new(false);
            }

            _src.Report(MsgLevel.DEBUG, "Parsed assignment statement", true);
            return new(true);
        }

        private ParseInfo IfStatement() {
            // First we need the if keyword
            if (_curToken.TokenType != TokenType.IF_RW) {
                _src.Report(MsgLevel.ERROR, "Expected an \"IF\"", true);
                return new(false);
            }
            NextToken();

            // Then we need the conditional statement
            if (_curToken.TokenType != TokenType.L_PAREN) {
                _src.Report(MsgLevel.ERROR, "Expected \"(\" after \"IF\"", true);
                return new(false);
            }
            NextToken();
            ParseInfo status = Expression();
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Expected an expression after \"(\"", true);
                return new(false);
            }
            NextToken();
            if (_curToken.TokenType != TokenType.R_PAREN) {
                _src.Report(MsgLevel.ERROR, "Expected \")\" after expression", true);
                return new(false);
            }
            NextToken();

            // Then we reach the then clause
            if (_curToken.TokenType != TokenType.THEN_RW) {
                _src.Report(MsgLevel.ERROR, "Expected \"THEN\" after \")\"", true);
                return new(false);
            }
            NextToken();

            // Next we'll read statements until we reach the else or end
            status = StatementList(new[] { TokenType.ELSE_RW, TokenType.END_RW, TokenType.EOF });
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Expected statement list after \"THEN\"", true);
                return new(false);
            }

            // We might have an else block
            if (_curToken.TokenType == TokenType.ELSE_RW) {
                // Next we'll read statements until we reach the end
                status = StatementList(new[] { TokenType.END_RW, TokenType.EOF });
                if (!status.Success) {
                    _src.Report(MsgLevel.DEBUG, "Expected statement list after \"ELSE\"", true);
                    return new(false);
                }
            }

            // Now we should be at the end of the statement
            if (_curToken.TokenType != TokenType.END_RW) {
                _src.Report(MsgLevel.ERROR, "Expected \"END\" after the statement list", true);
                return new(false);
            }
            NextToken();
            if (_curToken.TokenType != TokenType.IF_RW) {
                _src.Report(MsgLevel.ERROR, "Expected \"IF\" after \"END\"", true);
                return new(false);
            }
            NextToken();

            // We should be good to go
            _src.Report(MsgLevel.DEBUG, "Parsed if statement", true);
            return new(true);
        }

        private ParseInfo LoopStatement() {
            // First we expect the "for" keyword
            if (_curToken.TokenType != TokenType.FOR_RW) {
                _src.Report(MsgLevel.ERROR, "Expected \"FOR\" at the start of a loop", true);
                return new(false);
            }
            NextToken();

            // Next we look for the loop initialization
            if (_curToken.TokenType != TokenType.L_PAREN) {
                _src.Report(MsgLevel.ERROR, "Expected \"(\" after \"FOR\"", true);
                return new(false);
            }
            NextToken();
            ParseInfo status = AssignmentStatement();
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Expected an assignment statement aftet \"(\"", true);
                return new(false);
            }
            if (_curToken.TokenType != TokenType.SEMICOLON) {
                _src.Report(MsgLevel.ERROR, "Expected \";\" after the assignment statement", true);
                return new(false);
            }
            NextToken();
            status = Expression();
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Expected an expression after \";\"", true);
                return new(false);
            }
            if (_curToken.TokenType != TokenType.R_PAREN) {
                _src.Report(MsgLevel.ERROR, "Expected \")\" after expression", true);
                return new(false);
            }
            NextToken();

            // Now we accept statements until the end
            status = StatementList(new[] { TokenType.END_RW, TokenType.EOF });
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Expected a statement list after \")\"", true);
                return new(false);
            }

            // Now we expect the final two keywords
            if (_curToken.TokenType != TokenType.END_RW) {
                _src.Report(MsgLevel.ERROR, "Expected \"END\" after statement list", true);
                return new(false);
            }
            NextToken();
            if (_curToken.TokenType != TokenType.FOR_RW) {
                _src.Report(MsgLevel.ERROR, "Expected \"FOR\" after \"END\"", true);
                return new(false);
            }

            // We should be good to go
            _src.Report(MsgLevel.DEBUG, "Parsed loop statement", true);
            return new(true);
        }

        private ParseInfo ReturnStatement() {
            // First we expect the return keyword
            if (_curToken.TokenType != TokenType.RETURN_RW) {
                _src.Report(MsgLevel.ERROR, "Expected \"RETURN\"", true);
                return new(false);
            }
            NextToken();

            // Finally we expect an expression
            ParseInfo status = Expression();
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Expected expression after \"RETURN\"", true);
                return new(false);
            }

            // We should be good to go
            _src.Report(MsgLevel.DEBUG, "Parsed return statement", true);
            return new(true);
        }

        public ParseInfo ExpressionList() {
            // TEMPORARY - DELETE THIS
            if (_curToken.TokenType == TokenType.EOF) { return new(true); }
            // END DELETE THIS

            // First we expect an expression
            ParseInfo status = Expression();
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Expected an expression", true);
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
                _src.Report(MsgLevel.DEBUG, "Expected an arithmetic operation", true);
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
                _src.Report(MsgLevel.TRACE, sb.ToString(), true);
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
                _src.Report(MsgLevel.DEBUG, $"Expected an arithmetic operation after \"{symbol}\"", true);
                return new(false);
            }

            // Then we do it all over again
            return ExpressionPrime(sb);
        }

        private ParseInfo ArithOp() {
            // First we expect a relation
            ParseInfo status = Relation();
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Expected a relation", true);
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
                _src.Report(MsgLevel.TRACE, sb.ToString(), true);
                return new(true);
            }
            NextToken();
            sb.Append($" {symbol} relation");

            // Then we expect another relation
            ParseInfo status = Relation();
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, $"Expected a relation after \"{symbol}\"", true);
                return new(false);
            }

            // Then we go again
            return ArithOpPrime(sb);
        }

        private ParseInfo Relation() {
            // First we expect a term
            ParseInfo status = Term();
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Expected a term", true);
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
                _src.Report(MsgLevel.TRACE, sb.ToString(), true);
                return new(true);
            }
            NextToken();
            sb.Append($" {symbol} term");

            // If so, we expect another term
            ParseInfo status = Term();
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, $"Expected a term after \"{symbol}\"", true);
                return new(false);
            }

            // Now we go again
            return RelationPrime(sb);
        }

        private ParseInfo Term() {
            // First we expect a factor
            ParseInfo status = Factor();
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Expected a factor", true);
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
                _src.Report(MsgLevel.TRACE, sb.ToString(), true);
                return new(true);
            }
            NextToken();
            sb.Append($" {symbol} factor");

            // If so, we expect another factor
            ParseInfo status = Factor();
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, $"Expected a factor after \"{symbol}\"", true);
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
                    _src.Report(MsgLevel.DEBUG, "Expression expected after \"(\"", true);
                    return new(false);
                }
                if (_curToken.TokenType != TokenType.R_PAREN) {
                    _src.Report(MsgLevel.ERROR, "\")\" expected after expression", true);
                    return new(false);
                }
                NextToken();
                _src.Report(MsgLevel.TRACE, "Parsed factor as a nested expression", true);
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
                        _src.Report(MsgLevel.DEBUG, "Argument list expected after \"(\"", true);
                        return new(false);
                    }

                    // Either way, it needs to end with a right parens
                    if (_curToken.TokenType != TokenType.R_PAREN) {
                        _src.Report(MsgLevel.ERROR, "\")\" expected after argument list", true);
                        return new(false);
                    }
                    NextToken();
                    _src.Report(MsgLevel.TRACE, "Parsed factor as procedure call", true);
                    return new(true);
                }

                // If we have a name, then we might have an indexing operation
                if (_curToken.TokenType == TokenType.L_BRACKET) {
                    NextToken();
                    ParseInfo status = Expression();
                    if (!status.Success) {
                        _src.Report(MsgLevel.DEBUG, "Expression expected after \"[\"", true);
                        return new(false);
                    }
                    if (_curToken.TokenType != TokenType.R_BRACKET) {
                        _src.Report(MsgLevel.ERROR, "\"]\" expected after expression", true);
                        return new(false);
                    }
                    NextToken();
                    _src.Report(MsgLevel.TRACE, "Parsed factor as name with indexing", true);
                } else {
                    _src.Report(MsgLevel.TRACE, "Parsed factor as name", true);
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
                            _src.Report(MsgLevel.DEBUG, "Expression expected after \"[\"", true);
                            return new(false);
                        }
                        if (_curToken.TokenType != TokenType.R_BRACKET) {
                            _src.Report(MsgLevel.ERROR, "\"]\" expected after expression", true);
                            return new(false);
                        }
                        NextToken();
                        _src.Report(MsgLevel.TRACE, "Parsed factor as minus name with indexing", true);
                    } else {
                        _src.Report(MsgLevel.TRACE, "Parsed factor as minus name", true);
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
                _src.Report(MsgLevel.TRACE, "Parsed factor as minus number literal", true);
                return new(true);
            }

            // We can accept numbers
            if (_curToken.TokenType == TokenType.INTEGER || _curToken.TokenType == TokenType.FLOAT) {
                NextToken();
                _src.Report(MsgLevel.TRACE, "Parsed factor as number literal", true);
                return new(true);
            }

            // We can accept strings
            if (_curToken.TokenType == TokenType.STRING) {
                NextToken();
                _src.Report(MsgLevel.TRACE, "Parsed factor as string literal", true);
                return new(true);
            }

            // We can accept boolean literals
            if (_curToken.TokenType == TokenType.TRUE_RW || _curToken.TokenType == TokenType.FALSE_RW) {
                NextToken();
                _src.Report(MsgLevel.TRACE, "Parsed factor as boolean literal", true);
                return new(true);
            }

            // Anything else is unacceptable
            _src.Report(MsgLevel.ERROR, "Expected a factor", true);
            return new(false);
        }

        private ParseInfo ArgumentList() {
            // We should have an expression first
            ParseInfo status = Expression();
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Expression expected in argument list", true);
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
