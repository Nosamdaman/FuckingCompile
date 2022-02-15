using System;
using System.Collections.Generic;
using System.IO;

namespace jfc {
    public class ParseTree {
        public string Title { get; set; }
        public List<Token> Children { get; } = new();
        public ParseTree(string title) {
            Title = title;
        }
    }

    /// <summary> Parses the program </summary>
    public class Parser {
        private readonly Scanner _scanner;
        private Token _curToken = null;

        /// <summary> Creates a new parser on the given file </summary>
        /// <param name="src"> The  source code </param>
        /// <exception cref="ArgumentNullException"/>
        public Parser(SourceFileReader src) {
            _scanner = new Scanner(src);
        }

        private Token NextToken() {
            _curToken = _scanner.Scan();
            return _curToken;
        }

        private Token Factor() {
            // For now we'll just worry about strings, trues, and falses
            if (_curToken.TokenType != TokenType.STRING ||
                _curToken.TokenType != TokenType.TRUE_RW ||
                _curToken.TokenType != TokenType.FALSE_RW) {
                return new(TokenType.ERROR, "Invalid factor");
            }
            NextToken();
            return _curToken;
        }

        private Token Term() {
            // Build our tree
            Token token = new(TokenType.TREE, new ParseTree("Factor"));
            ParseTree tree = (ParseTree) token.TokenMark;

            // First we expect a factor
            Token factor = Factor();
            tree.Children.Add(factor);
            if (factor.TokenType == TokenType.ERROR) {
                tree.Children.Add(new(TokenType.ERROR, "Expected a factor at the start of a term"));
                return token;
            }

            // Then we need to see how many operation are chained together
            Token termPrime = TermPrime();
            if (termPrime is not null) tree.Children.Add(termPrime);
            return token;
        }

        private Token TermPrime() {
            // Build our tree
            Token token = new(TokenType.TREE, new ParseTree("Term Prime"));
            ParseTree tree = (ParseTree) token.TokenMark;

            // If we don't have a "*" or a "/", then we're good to go
            char symbol;
            if (_curToken.TokenType == TokenType.TIMES) {
                symbol = '*';
            } else if (_curToken.TokenType == TokenType.DIVIDE) {
                symbol = '/';
            } else {
                return null;
            }
            tree.Children.Add(_curToken);

            // Otherwise, we need the next token(s) to be a factor
            NextToken();
            Token factor = Factor();
            tree.Children.Add(factor);
            if (factor.TokenType == TokenType.ERROR) {
                tree.Children.Add(new(TokenType.ERROR, $"Expected a factor to follow the \"{symbol}\" symbol"));
                return token;
            }

            // Now we try again
            Token termPrime = TermPrime();
            if (termPrime is not null) tree.Children.Add(termPrime);
            return token;
        }
    }
}
