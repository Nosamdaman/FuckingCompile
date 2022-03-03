using System;
using System.Collections.Generic;
using System.Linq;

namespace jfc {
    /// <summary> Structure representing the status of the parse operation </summary>
    public struct ParseInfo {
        /// <summary> Whether or not the parse was successful </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Object giving information about the parse. It's exact type will be determined by the parse method which
        /// created it.
        /// </summary>
        public object Data { get; set; }

        /// <summary> Instantiate a new ParseInfo object </summary>
        /// <param name="success"> Whether or not the parse succeeded </param>
        /// <param name="data"> Information about the parse. Defaults to null </param>
        public ParseInfo(bool success, object data = null) {
            Success = success;
            Data = data;
        }
    }

    /// <summary> Parses the program </summary>
    public partial class Parser {
        private readonly SourceFileReader _src;
        private readonly Scanner _scanner;
        private Token _curToken = null;
        private readonly Dictionary<string, Symbol> _global = new(new StringNoCaseComparer());
        private readonly Stack<Dictionary<string, Symbol>> _local = new();

        /// <summary> Creates a new parser on the given file </summary>
        /// <param name="src"> The  source code </param>
        /// <exception cref="ArgumentNullException"/>
        public Parser(SourceFileReader src) {
            // Add the scanner and get the first symbol
            _src = src;
            _scanner = new Scanner(src);
            NextToken();

            // Seed the global symbol table with reserved procedures
            Symbol getBool = Symbol.Procedure("GETBOOL", DataType.BOOL, null);
            Symbol getInteger = Symbol.Procedure("GETINTEGER", DataType.INTEGER, null);
            Symbol getFloat = Symbol.Procedure("GETFLOAT", DataType.FLOAT, null);
            Symbol getString = Symbol.Procedure("GETSTRING", DataType.STRING, null);
            Symbol putBool = Symbol.Procedure("PUTBOOL", DataType.BOOL, new[] {
                Symbol.Variable("VALUE", DataType.BOOL)
            });
            Symbol putInteger = Symbol.Procedure("PUTINTEGER", DataType.BOOL, new[] {
                Symbol.Variable("VALUE", DataType.INTEGER)
            });
            Symbol putFloat = Symbol.Procedure("PUTFLOAT", DataType.BOOL, new[] {
                Symbol.Variable("VALUE", DataType.FLOAT)
            });
            Symbol putString = Symbol.Procedure("PUTSTRING", DataType.BOOL, new[] {
                Symbol.Variable("VALUE", DataType.STRING)
            });
            Symbol sqrt = Symbol.Procedure("SQRT", DataType.FLOAT, new[] {
                Symbol.Variable("VALUE", DataType.INTEGER)
            });
            _global.Add("getBool", getBool);
            _global.Add("getInteger", getInteger);
            _global.Add("getFloat", getFloat);
            _global.Add("getString", getString);
            _global.Add("putBool", putBool);
            _global.Add("putInteger", putInteger);
            _global.Add("putFloat", putFloat);
            _global.Add("putString", putString);
            _global.Add("sqrt", sqrt);
        }

        /// <summary> Adds a new local scope to the local symbol table </summary>
        private void PushScope() {
            _local.Push(new(new StringNoCaseComparer()));
        }

        /// <summary> Pops the current local scope off of the symbol table </summary>
        /// <returns> The scope which was just popped off </returns>
        private Dictionary<string, Symbol> PopScope() {
            return _local.Pop();
        }

        /// <summary> Tries to get a symbol from the symbol table </summary>
        /// <param name="identifier"> The id of the symbol to be retrieved </param>
        /// <param name="symbol"> The symbol being retrieved </param>
        /// <returns> Whether or not the symbol was found </returns>
        private bool TryGetSymbol(string identifier, out Symbol symbol) {
            if (_local.Count > 0 && _local.Peek().ContainsKey(identifier)) {
                symbol = _local.Peek()[identifier];
                return true;
            } else if (_global.ContainsKey(identifier)) {
                symbol = _global[identifier];
                return true;
            }
            symbol = Symbol.Variable("FAIL", DataType.BOOL);
            return false;
        }

        /// <summary> Scans for the next token in the source file </summary>
        /// <returns> The next token </returns>
        private Token NextToken() {
            _curToken = _scanner.Scan();
            return _curToken;
        }

        /// <summary> Parses the top-level program of a program </summary>
        /// <returns> A ParseInfo with no special data </returns>
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
            string programName = (string) _curToken.TokenMark;
            _src.Report(MsgLevel.INFO, $"Parsing program \"{programName}\"");
            NextToken();
            if (_curToken.TokenType != TokenType.IS_RW) {
                _src.Report(MsgLevel.ERROR, "\"IS\" expected after identifier", true);
                return new(false);
            }
            NextToken();

            // Then we need the program body
            ParseInfo status = DeclarationList(new[] { TokenType.BEGIN_RW, TokenType.EOF }, true);
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
            _src.Report(MsgLevel.INFO, $"Finished parsing program \"{programName}\"");
            return new(true);
        }

        /// <summary> Parses a list of declarations </summary>
        /// <param name="exitTokens">
        /// An array of tokens which should signal the end of the declaration list. If any token in this list is seen at
        /// what could be the start of a new declaration, execution will end.
        /// </param>
        /// <param name="isGlobal">
        /// Whether or not all declarations in the list are to be treated as global. Defaults to false.
        /// </param>
        /// <returns> A ParseInfo with no special data </returns>
        private ParseInfo DeclarationList(TokenType[] exitTokens, bool isGlobal = false) {
            // Loop to look for declarations
            while (!exitTokens.Contains(_curToken.TokenType)) {
                ParseInfo status = Declaration(isGlobal);
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

        /// <summary> Parses a list of parameters </summary>
        /// <returns>
        /// A ParseInfo describing the success of the parse. If parsing succeeded, the Data parameter will be set to an
        /// array of Symbols representing the parsed parameters.
        /// </returns>
        private ParseInfo ParameterList() {
            // We'll create a new scope just for this list
            PushScope();

            // First we expect a parameter
            ParseInfo status = VariableDeclaration(false);
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Variable declaration expected at the start of a parameter list", true);
                PopScope();
                return new(false);
            }
            int count = 1;

            // Then we loop until we don't see a comma
            while (_curToken.TokenType == TokenType.COMMA) {
                NextToken();
                status = VariableDeclaration(false);
                if (!status.Success) {
                    _src.Report(MsgLevel.DEBUG, "Variable declaration expected after \",\"", true);
                    PopScope();
                    return new(false);
                }
                count++;
            }

            // Now we pop the scope and grab the variables
            Symbol[] parameters = PopScope().Values.ToArray();

            // We should be good to go
            _src.Report(MsgLevel.TRACE, $"Parsed list of {count} parameter(s)", true);
            return new(true, parameters);
        }

        /// <summary> Parses a list of statements </summary>
        /// <param name="exitTokens">
        /// An array of tokens which should signal the end of the statement list. If any token in this list is seen at
        /// what could be the start of a new statement, execution will end.
        /// </param>
        /// <returns> A ParseInfo with no special data </returns>
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

        // TODO: TYPE VALIDATION
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
