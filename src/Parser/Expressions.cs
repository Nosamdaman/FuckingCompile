using System.Linq;
using System.Text;

namespace jfc {
    /// <summary> Parses the program </summary>
    public partial class Parser {
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

        /// <summary> Parses a factor </summary>
        /// <returns>
        /// A ParseInfo describing the success of the parse. If parsing succeeded, the Data parameter will be set to the
        /// DataType of the factor.
        /// </returns>
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
                return new(true, status.Data);
            }

            // If that fails, we'll see if we have a procedure or name. If either is the case, then we'll need to see an
            // identifier first
            if (_curToken.TokenType == TokenType.IDENTIFIER) {
                // First we'll we'll ensure that our symbol exists
                string id = (string) _curToken.TokenMark;
                if (!TryGetSymbol(id, out Symbol symbol)) {
                    _src.Report(MsgLevel.ERROR, $"Reference to unkown symbol \"{id}\"", true);
                    return new(false);
                }

                // Parse the symbol based on its type
                ParseInfo status;
                if (symbol.SymbolType == SymbolType.VARIABLE) {
                    status = VariableReference(symbol);
                } else if (symbol.SymbolType == SymbolType.PROCEDURE) {
                    status = ProcedureReference(symbol);
                } else {
                    throw new System.Exception("How the fuck did you even get here?");
                }
                if (!status.Success) {
                    _src.Report(MsgLevel.DEBUG, $"Unable to parse reference to symbol \"{symbol.Name}\"", true);
                    return new(false);
                }

                // We should be good here
                _src.Report(MsgLevel.TRACE, "Parsed factor as symbol reference");
                return new(true, status.Data);
            }

            // If we have a minus symbol, then we could have either a name or a number
            if (_curToken.TokenType == TokenType.MINUS) {
                NextToken();

                // If we see an identifier, it's a name
                if (_curToken.TokenType == TokenType.IDENTIFIER) {
                    // First ensure that the symbol exists and is a variable
                    string id = (string) _curToken.TokenMark;
                    if (!TryGetSymbol(id, out Symbol symbol)) {
                        _src.Report(MsgLevel.ERROR, $"Reference to unkown symbol \"{id}\"", true);
                        return new(false);
                    } else if (symbol.SymbolType != SymbolType.VARIABLE) {
                        _src.Report(MsgLevel.ERROR, "Variable expected after \"-\"", true);
                        return new(false);
                    }

                    // Then ensure that the symbol is properly referenced
                    ParseInfo status = VariableReference(symbol);
                    if (!status.Success) {
                        _src.Report(MsgLevel.DEBUG, $"Unable to parse reference to symbol \"{symbol.Name}\"", true);
                        return new(false);
                    }

                    // The minus sign is an implicit converter, so we need to get our types straight
                    (DataType dataType, int arraySize) = ((DataType, int)) status.Data;
                    if (dataType == DataType.STRING) {
                        _src.Report(MsgLevel.ERROR, $"Type \"{DataType.STRING}\" cannot be inverted", true);
                        return new(false);
                    }
                    Symbol.TryGetCompatibleType(DataType.INTEGER, dataType, out DataType result);

                    // We should be good to go
                    _src.Report(MsgLevel.TRACE, $"Parsed factor as minus \"{symbol.Name}\"", true);
                    return new(true, (result, arraySize));
                }

                // Otherwise, it should be a number
                DataType d;
                if (_curToken.TokenType == TokenType.INTEGER ||
                    _curToken.TokenType == TokenType.TRUE_RW ||
                    _curToken.TokenType == TokenType.FALSE_RW
                ) {
                    d = DataType.INTEGER;
                } else if (_curToken.TokenType == TokenType.FLOAT) {
                    d = DataType.FLOAT;
                } else {
                    _src.Report(MsgLevel.ERROR, "Name or number expected after \"-\"", true);
                    return new(false);
                }
                NextToken();

                // We should be goood to go
                _src.Report(MsgLevel.TRACE, "Parsed factor as minus number literal", true);
                return new(true, (d, 0));
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

        /// <summary> Parses a variable reference </summary>
        /// <param name="variable"> The whose reference is to be parsed </param>
        /// <returns>
        /// A ParseInfo describing the success of the parse. If successful, it will describe the data-type to be
        /// returned.
        /// </returns>
        private ParseInfo VariableReference(Symbol variable) {
            // We'll assume that we're on the current variable and just move on
            NextToken();

            // Get information about the variable
            DataType dataType = variable.DataType;
            int arraySize = 0;
            if (variable.IsArray) arraySize = variable.ArraySize;

            // Now we'll check for indexing
            if (_curToken.TokenType != TokenType.L_BRACKET) {
                return new(true, (dataType, arraySize));
            }

            // We'll error if the variable isn't an array
            if (!variable.IsArray) {
                _src.Report(MsgLevel.ERROR, "Cannot index a non-array variable", true);
                return new(false);
            }
            NextToken();

            // Now we check the bounds
            ParseInfo status = Expression();
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Expression expected after \"[\"", true);
                return new(false);
            }
            (DataType boundDataType, int boundArraySize) = ((DataType, int)) status.Data;
            if (boundDataType == DataType.STRING || boundArraySize != 0) {
                _src.Report(MsgLevel.ERROR, "Bounds must be scalar values that can be converted to integers", true);
                return new(false);
            }

            // Finally we expect a closing bracket
            if (_curToken.TokenType != TokenType.R_BRACKET) {
                _src.Report(MsgLevel.ERROR, "\"]\" expected after expression", true);
                return new(false);
            }
            NextToken();

            // We should be good to go
            return new(true, (dataType, 0));
        }

        /// <summary> Parses a procedure reference </summary>
        /// <param name="procedure"> Reference to the procedure to be parsed </param>
        /// <returns>
        /// A ParseInfo describing the success of the parse. If parsing succeeded, the Data parameter will be set to the
        /// DataType of the procedure.
        /// </returns>
        private ParseInfo ProcedureReference(Symbol procedure) {
            // We'll assume that we're on the current procedure and just move on
            NextToken();

            // Now we expect a left parens
            if (_curToken.TokenType != TokenType.L_PAREN) {
                _src.Report(MsgLevel.ERROR, "\"(\" expected after procedure name", true);
                return new(false);
            }
            NextToken();

            // Parse the arguments if there are any
            if (procedure.Parameters.Any()) {
                // Parse the first argument
                ParseInfo status = ProcedureArgument(procedure.Parameters.First());
                if (!status.Success) {
                    Symbol argument = procedure.Parameters.First();
                    _src.Report(MsgLevel.DEBUG, $"Failed to parse argument \"{argument.Name}\"", true);
                    return new(false);
                }

                // Parse the remaining arguments
                foreach (Symbol argument in procedure.Parameters.Skip(1)) {
                    // First we expect a comma
                    if (_curToken.TokenType != TokenType.COMMA) {
                        _src.Report(MsgLevel.ERROR, "\",\" expected after argument", true);
                        return new(false);
                    }
                    NextToken();

                    // Then we expect the argument
                    status = ProcedureArgument(argument);
                    if (!status.Success) {
                        _src.Report(MsgLevel.DEBUG, $"Failed to parse argument \"{argument.Name}\"", true);
                        return new(false);
                    }
                }
            }

            // Finally we expect the closing parens
            if (_curToken.TokenType != TokenType.R_PAREN) {
                _src.Report(MsgLevel.ERROR, "\")\" expected after argument list", true);
                return new(false);
            }
            NextToken();

            // We should be good to go
            return new(true, (procedure.DataType, 0));
        }

        /// <summary> Parses a single argument in a procedure call </summary>
        /// <param name="symbol"> The expected type of the argument </param>
        /// <returns> A ParseInfo describing the success of the parse </returns>
        private ParseInfo ProcedureArgument(Symbol symbol) {
            // First parse the expression for the argument
            ParseInfo status = Expression();
            if (!status.Success) { return new(false); }

            // Then compare the types
            (DataType actualDataType, int actualArraySize) = ((DataType, int)) status.Data;
            int expectedArraySize = 0;
            if (symbol.IsArray) { expectedArraySize = symbol.ArraySize; }
            if (!Symbol.TryGetCompatibleType(symbol.DataType, actualDataType, out DataType _)) {
                _src.Report(MsgLevel.ERROR, $"Type \"{actualDataType}\" is not castable to \"{symbol.DataType}", true);
                return new(false);
            }

            // Finally compare the array sizes
            if (expectedArraySize != actualArraySize) {
                _src.Report(MsgLevel.ERROR, "Array size mismatch", true);
                return new(false);
            }

            // We should be good to go
            return new(true);
        }
    }
}
