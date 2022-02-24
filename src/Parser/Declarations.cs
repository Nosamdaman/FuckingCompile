using System;

namespace jfc {
    public partial class Parser {
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
            // First we expect a declaration list
            ParseInfo status = DeclarationList(new[] { TokenType.BEGIN_RW, TokenType.EOF });
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Expected declaration list at the start of procedure body", true);
                return new(false);
            }
        }

        private ParseInfo ParameterList() {
            // First we expect a parameter
            ParseInfo status = VariableDeclaration();
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Variable declaration expected at the start of a parameter list", true);
                return new(false);
            }
            int count = 1;

            // Then we loop until we don't see a comma
            while (_curToken.TokenType == TokenType.COMMA) {
                NextToken();
                status = VariableDeclaration();
                if (!status.Success) {
                    _src.Report(MsgLevel.DEBUG, "Variable declaration expected after \",\"", true);
                    return new(false);
                }
                count++;
            }

            // We should be good to go
            _src.Report(MsgLevel.TRACE, $"Parsed list of {count} parameter(s)", true);
            return new(true);
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
    }
}
