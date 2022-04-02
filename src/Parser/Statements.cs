namespace jfc {
    /// <summary> Parses the program </summary>
    public partial class Parser {
        /// <summary> Parses a statement </summary>
        /// <param name="returnType">
        /// The expected return type of this block. If set to null, no return statements are allowed, as we are in the
        /// global scope.
        /// </param>
        /// <returns> A ParseInfo with no special data </returns>
        private ParseInfo Statement(DataType? returnType) {
            switch (_curToken.TokenType) {
            case TokenType.IDENTIFIER:
                return AssignmentStatement();
            case TokenType.IF_RW:
                return IfStatement(returnType);
            case TokenType.FOR_RW:
                return LoopStatement(returnType);
            case TokenType.RETURN_RW:
                if (returnType is null) {
                    _src.Report(MsgLevel.ERROR, "Return statements now allowed in the global scope", true);
                    return new(false);
                }
                return ReturnStatement((DataType) returnType);
            default:
                _src.Report(MsgLevel.ERROR, "Expected a statement", true);
                return new(false);
            }
        }

        /// <summary> Parses an assignment statement </summary>
        /// <returns> A ParseInfo with no special data </returns>
        private ParseInfo AssignmentStatement() {
            ParseInfo status;

            _translator.Comment("\t; Begin assignment statement");

            // We first need an identifier
            if (_curToken.TokenType != TokenType.IDENTIFIER) {
                _src.Report(MsgLevel.ERROR, "Expected an identifier as a destination", true);
                return new(false);
            }

            // Then, we need to verify that the identifier exists and refers to a variable
            string id = (string) _curToken.TokenMark;
            if (!TryGetSymbol(id, out Symbol symbol)) {
                _src.Report(MsgLevel.ERROR, $"Reference to unkown symbol \"{id}\"", true);
                return new(false);
            }
            if (symbol.SymbolType != SymbolType.VARIABLE) {
                _src.Report(MsgLevel.ERROR, "Assignment statements must begin with a variable reference", true);
                return new(false);
            }
            status = VariableReference(symbol);
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Expected a valid variable reference", true);
                return new(false);
            }
            (DataType eDataType, int eArraySize) = ((DataType, int)) status.Data;

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

            // Now we need to verify type compatibility
            (DataType aDataType, int aArraySize) = ((DataType, int)) status.Data;
            if (eDataType != aDataType && (
                (eDataType == DataType.BOOL && aDataType != DataType.INTEGER) ||
                (eDataType == DataType.INTEGER && aDataType != DataType.BOOL && aDataType != DataType.FLOAT) ||
                (eDataType == DataType.FLOAT && aDataType != DataType.INTEGER) ||
                eDataType == DataType.STRING
            )) {
                _src.Report(MsgLevel.ERROR, $"Type mismatch between \"{eDataType}\" and \"{aDataType}\"", true);
                return new(false);
            }
            if (eArraySize != aArraySize) {
                _src.Report(MsgLevel.ERROR, $"Array size mismatch between \"{eArraySize}\" and \"{aArraySize}\"", true);
                return new(false);
            }

            // Perform any conversions
            string reg = status.Reg;
            if (eDataType == DataType.BOOL && aDataType == DataType.INTEGER) {
                reg = _translator.IntToBool(reg, aArraySize);
            } else if (eDataType == DataType.INTEGER && aDataType == DataType.BOOL) {
                reg = _translator.BoolToInt(reg, aArraySize);
            } else if (eDataType == DataType.INTEGER && aDataType == DataType.FLOAT) {
                reg = _translator.FloatToInt(reg, aArraySize);
            } else if (eDataType == DataType.FLOAT && aDataType == DataType.INTEGER) {
                reg = _translator.IntToFloat(reg, aArraySize);
            }

            // Perform the assignment
            _translator.Assignment(symbol, reg);

            _src.Report(MsgLevel.TRACE, "Parsed assignment statement", true);
            return new(true);
        }

        /// <summary> Parses an if statement </summary>
        /// <param name="returnType">
        /// The expected return type of this block. If set to null, no return statements are allowed, as we are in the
        /// global scope.
        /// </param>
        /// <returns> A ParseInfo with no special data </returns>
        private ParseInfo IfStatement(DataType? returnType) {
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
            (DataType dataType, int arraySize) = ((DataType, int)) status.Data;
            if ((dataType != DataType.BOOL && dataType != DataType.INTEGER) || arraySize != 0) {
                _src.Report(MsgLevel.ERROR, "Conditional expression must evaluate to a singular boolean", true);
                return new(false);
            }
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
            status = StatementList(new[] { TokenType.ELSE_RW, TokenType.END_RW, TokenType.EOF }, returnType);
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Expected statement list after \"THEN\"", true);
                return new(false);
            }

            // We might have an else block
            if (_curToken.TokenType == TokenType.ELSE_RW) {
                // Next we'll read statements until we reach the end
                NextToken();
                status = StatementList(new[] { TokenType.END_RW, TokenType.EOF }, returnType);
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
            _src.Report(MsgLevel.TRACE, "Parsed if statement", true);
            return new(true);
        }

        /// <summary> Parses a loop statement </summary>
        /// <param name="returnType">
        /// The expected return type of this block. If set to null, no return statements are allowed, as we are in the
        /// global scope.
        /// </param>
        /// <returns> A ParseInfo with no special data </returns>
        private ParseInfo LoopStatement(DataType? returnType) {
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
            (DataType dataType, int arraySize) = ((DataType, int)) status.Data;
            if ((dataType != DataType.BOOL && dataType != DataType.INTEGER) || arraySize != 0) {
                _src.Report(MsgLevel.ERROR, "Conditional expression must evaluate to a singular boolean", true);
                return new(false);
            }
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
            status = StatementList(new[] { TokenType.END_RW, TokenType.EOF }, returnType);
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
            NextToken();

            // We should be good to go
            _src.Report(MsgLevel.TRACE, "Parsed loop statement", true);
            return new(true);
        }

        /// <summary> Parses a return statement </summary>
        /// <param name="returnType"> The expected return type of this statement </param>
        /// <returns> A ParseInfo with no special data </returns>
        private ParseInfo ReturnStatement(DataType returnType) {
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
            (DataType dataType, int arraySize) = ((DataType, int)) status.Data;
            if (returnType != dataType && (
                (returnType == DataType.BOOL && dataType != DataType.INTEGER) ||
                (returnType == DataType.INTEGER && dataType != DataType.BOOL && dataType != DataType.FLOAT) ||
                (returnType == DataType.FLOAT && dataType != DataType.INTEGER) ||
                returnType == DataType.STRING
            )) {
                _src.Report(MsgLevel.ERROR, $"Type mismatch between \"{returnType}\" and \"{dataType}\"", true);
                return new(false);
            }
            if (arraySize != 0) {
                _src.Report(MsgLevel.ERROR, "Cannot return an array", true);
                return new(false);
            }

            // We should be good to go
            _src.Report(MsgLevel.TRACE, "Parsed return statement", true);
            return new(true);
        }
    }
}
