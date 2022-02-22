using System;
using System.Linq;

namespace jfc {
    public struct ParseInfo {
        public bool Success { get; set; }

        public ParseInfo(bool success) {
            Success = success;
        }
    }

    /// <summary> Parses the program </summary>
    public partial class Parser {
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

        private ParseInfo DeclarationList(TokenType[] exitTokens) {
            // Loop to look for declarations
            while (!exitTokens.Contains(_curToken.TokenType)) {
                ParseInfo status = Declaration();
                if (!status.Success) {
                    _src.Report(MsgLevel.DEBUG, "Expected a declaration", true);
                    return new(false);
                }
                if (_curToken.TokenType != TokenType.SEMICOLON) {
                    _src.Report(MsgLevel.ERROR, "\";\" expected after declaration", true);
                    return new(false);
                }
                NextToken();
            }
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
