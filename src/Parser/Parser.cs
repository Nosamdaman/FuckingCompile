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
