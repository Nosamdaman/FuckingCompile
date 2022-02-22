namespace jfc {
    public partial class Parser {
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
    }
}
