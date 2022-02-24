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

        public ParseInfo Program() {
            // First we need the program header
            if (_curToken.TokenType != TokenType.PROGRAM_RW) {
                _src.Report(MsgLevel.ERROR, "\"PROGRAM\" expected at the start of the program", true);
                return new(false);
            }
            NextToken();
            if (_curToken.TokenType != TokenType.IDENTIFIER) {
                _src.Report(MsgLevel.ERROR, "Identifier expected after \"PROGRAM\"", true);
                return new(false);
            }
            NextToken();
            if (_curToken.TokenType != TokenType.IS_RW) {
                _src.Report(MsgLevel.ERROR, "\"IS\" expected after identifier", true);
                return new(false);
            }
            NextToken();

            // Then we need the program body
            ParseInfo status = DeclarationList(new[] { TokenType.BEGIN_RW, TokenType.EOF });
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Expected declaration list at the start of program body", true);
                return new(false);
            }
            if (_curToken.TokenType != TokenType.BEGIN_RW) {
                _src.Report(MsgLevel.ERROR, "\"BEGIN\" expected after declaration list", true);
                return new(false);
            }
            NextToken();
            status = StatementList(new[] { TokenType.END_RW, TokenType.EOF });
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Statement list expected after \"BEGIN\"", true);
                return new(false);
            }
            if (_curToken.TokenType != TokenType.END_RW) {
                _src.Report(MsgLevel.ERROR, "\"END\" expected after statement list", true);
                return new(false);
            }
            NextToken();
            if (_curToken.TokenType != TokenType.PROGRAM_RW) {
                _src.Report(MsgLevel.ERROR, "\"PROGRAM\" expected after \"END\"", true);
                return new(false);
            }
            NextToken();

            // Finally we end with a period
            if (_curToken.TokenType != TokenType.PERIOD) {
                _src.Report(MsgLevel.ERROR, "\".\" expected after \"PROGRAM\"", true);
                return new(false);
            }
            NextToken();

            // Now we should be at the end of the file
            if (_curToken.TokenType != TokenType.EOF) {
                _src.Report(MsgLevel.WARN, "Skipping anything past here", true);
            }
            return new(true);
        }

        private Token NextToken() {
            _curToken = _scanner.Scan();
            return _curToken;
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
