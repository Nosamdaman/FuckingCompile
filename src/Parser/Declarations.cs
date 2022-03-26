using System;

namespace jfc {
    /// <summary> Parses the program </summary>
    public partial class Parser {
        /// <summary> Parses a declaration and adds it to the appropriate symbol table </summary>
        /// <param name="isGlobal"> Whether or not the declaration is in the global scope. Defaults to false </param>
        /// <returns> A ParseInfo with no special data </returns>
        private ParseInfo Declaration(bool isGlobal = false) {
            // What we do will depend on the first token
            ParseInfo status;
            if (_curToken.TokenType == TokenType.GLOBAL_RW) {
                isGlobal = true;
                NextToken();
            }
            if (_curToken.TokenType == TokenType.VARIABLE_RW) {
                status = VariableDeclaration(isGlobal);
            } else if (_curToken.TokenType == TokenType.PROCEDURE_RW) {
                status = ProcedureDeclaration(isGlobal);
            } else {
                _src.Report(MsgLevel.ERROR, "\"VARIABLE\" or \"PROCEDURE\" expectin before a declaration", true);
                return new(false);
            }
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Unable to parse declaration", true);
                return new(false);
            }

            // We should be good to go
            return new(true);
        }

        /// <summary> Parses a procedure declaration and adds it to the appropriate symbol table </summary>
        /// <param name="isGlobal"> Whether or not the procedure is in the global scope. Defaults to false </param>
        /// <returns> A ParseInfo with no special data </returns>
        private ParseInfo ProcedureDeclaration(bool isGlobal = false) {
            // First we expect a procedure header
            ParseInfo status = ProcedureHeader(isGlobal);
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Procedure header expected at the start of a procedure declaration", true);
                return new(false);
            }

            // Now we need to create the scope for the procedure and populate it with the parameters
            Symbol proc = (Symbol) status.Data;
            PushScope();
            foreach (Symbol parameter in proc.Parameters) {
                _local.Peek().Add(parameter.Name, parameter);
            }

            // Now we'll begin by writing the opening part of the procedure
            _translator.StartProcedure(proc);

            // Then we expect a procedure body
            status = ProcedureBody(proc.DataType);
            PopScope();
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Procedure header expected after procedure header", true);
                return new(false);
            }

            // We can now finish the procedure
            _translator.FinishProcedure(proc);

            // We should be good to go
            _src.Report(MsgLevel.INFO, $"Procedure \"{proc.Name}\" of type \"{proc.DataType}\" parsed");
            return new(true);
        }

        /// <summary> Parses a procedure header </summary>
        /// <param name="isGlobal"> Whether or not the procedure is in the global scope </param>
        /// <returns>
        /// A ParseInfo describing the success of the parse. If parsing succeeded, the Data parameter will be set to a
        /// Symbol representing the parsed procedure.
        /// </returns>
        private ParseInfo ProcedureHeader(bool isGlobal) {
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

            // The ID should be unique
            string id = (string) _curToken.TokenMark;
            if ((!isGlobal && _local.Peek().ContainsKey(id)) || (isGlobal && _global.ContainsKey(id))) {
                string scope = isGlobal ? "global" : "local";
                _src.Report(MsgLevel.ERROR, $"Identifier \"{id}\" already exists in the {scope} scope.", true);
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
            DataType dataType = (DataType) status.Data;

            // Then we need a left parens
            if (_curToken.TokenType != TokenType.L_PAREN) {
                _src.Report(MsgLevel.ERROR, "\"(\" expected after type mark", true);
                return new(false);
            }
            NextToken();

            // Then we need the parameter list
            Symbol[] parameters = Array.Empty<Symbol>();
            if (_curToken.TokenType != TokenType.R_PAREN) {
                status = ParameterList();
                if (!status.Success) {
                    _src.Report(MsgLevel.DEBUG, "Parameter list expected after \"(\"", true);
                    return new(false);
                }
                parameters = (Symbol[]) status.Data;
            }

            // And finally we need a right parens
            if (_curToken.TokenType != TokenType.R_PAREN) {
                _src.Report(MsgLevel.ERROR, "\")\" expected after parameter list", true);
                return new(false);
            }
            NextToken();

            // We should be good to go
            Symbol procedure = Symbol.Procedure(id, dataType, parameters);
            if (isGlobal) {
                _global.Add(id, procedure);
            } else {
                _local.Peek().Add(id, procedure);
            }
            _src.Report(MsgLevel.DEBUG, "Parsed procedure header", true);
            return new(true, procedure);
        }

        /// <summary> Parses a procedure body </summary>
        /// <param name="returnType"> The return type of the procedure </param>
        /// <returns> A ParseInfo with no special data </returns>
        private ParseInfo ProcedureBody(DataType returnType) {
            // First we expect a declaration list
            ParseInfo status = DeclarationList(new[] { TokenType.BEGIN_RW, TokenType.EOF });
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Expected declaration list at the start of procedure body", true);
                return new(false);
            }

            // Then we look for begin
            if (_curToken.TokenType != TokenType.BEGIN_RW) {
                _src.Report(MsgLevel.ERROR, "\"BEGIN\" expected after declaration list", true);
                return new(false);
            }
            NextToken();

            // Then we need a statement list
            status = StatementList(new[] { TokenType.END_RW, TokenType.EOF }, returnType);
            if (!status.Success) {
                _src.Report(MsgLevel.DEBUG, "Statement list expected after \"BEGIN\"", true);
                return new(false);
            }

            // Then we need end procedure
            if (_curToken.TokenType != TokenType.END_RW) {
                _src.Report(MsgLevel.ERROR, "\"END\" expected after statement list", true);
                return new(false);
            }
            NextToken();
            if (_curToken.TokenType != TokenType.PROCEDURE_RW) {
                _src.Report(MsgLevel.ERROR, "\"PROCEDURE\" expected after \"END\"", true);
                return new(false);
            }
            NextToken();

            // We should be good to go
            _src.Report(MsgLevel.DEBUG, "Parsed procedure body", true);
            return new(true);
        }

        /// <summary> Parses a variable declaration and adds it to the appropriate symbol table </summary>
        /// <param name="isGlobal"> Whether or not the variable is in the global scope </param>
        /// <param name="translate"> Whether or not to translate the declaration in assembly </param>
        /// <returns> A ParseInfo with no special data </returns>
        private ParseInfo VariableDeclaration(bool isGlobal, bool translate=true) {
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

            // The identifier must be unique
            string id = (string) _curToken.TokenMark;
            if ((!isGlobal && _local.Peek().ContainsKey(id)) || (isGlobal && _global.ContainsKey(id))) {
                string scope = isGlobal ? "global" : "local";
                _src.Report(MsgLevel.ERROR, $"Identifier \"{id}\" already exists in the {scope} scope.", true);
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
            DataType dataType = (DataType) status.Data;

            // If we don't see a left bracket, then we're good to go
            Symbol variable;
            if (_curToken.TokenType != TokenType.L_BRACKET) {
                variable = Symbol.Variable(id, dataType);
                if (isGlobal) {
                    _global.Add(variable.Name, variable);
                } else {
                    _local.Peek().Add(variable.Name, variable);
                }
                _src.Report(MsgLevel.DEBUG, $"Variable \"{variable.Name}\" declared as \"{dataType}\"", true);
                if (translate) _translator.DeclareVariable(variable, isGlobal);
                return new(true);
            } else if (dataType == DataType.STRING) {
                _src.Report(MsgLevel.DEBUG, $"Strings cannot be arrays", true);
                return new(false);
            }
            NextToken();

            // Otherwise, we're looking for an integer next
            if (_curToken.TokenType != TokenType.INTEGER) {
                _src.Report(MsgLevel.ERROR, "Bound expected after \"[\"", true);
                return new(false);
            }
            int arraySize = (int) _curToken.TokenMark;
            NextToken();

            // Finally we're looking for a right bracket
            if (_curToken.TokenType != TokenType.R_BRACKET) {
                _src.Report(MsgLevel.ERROR, "\"]\" expected after bound", true);
                return new(false);
            }
            NextToken();

            // We should be good to go
            variable = Symbol.VariableArray(id, dataType, arraySize);
            if (isGlobal) {
                _global.Add(variable.Name, variable);
            } else {
                _local.Peek().Add(variable.Name, variable);
            }
            _src.Report(MsgLevel.DEBUG, $"Variable \"{variable.Name}\" declared as \"{dataType}\"", true);
            if (translate) _translator.DeclareVariable(variable, isGlobal);
            return new(true);
        }

        /// <summary> Parses a type mark </summary>
        /// <returns>
        /// A ParseInfo describing the success of the parse. If parsing succeeded, the Data parameter will be set to a
        /// DataType giving the type of the type mark.
        /// </returns>
        private ParseInfo TypeMark() {
            // Check for a valid token
            if (_curToken.TokenType == TokenType.INTEGER_RW) {
                NextToken();
                return new(true, DataType.INTEGER);
            } else if (_curToken.TokenType == TokenType.FLOAT_RW) {
                NextToken();
                return new(true, DataType.FLOAT);
            } else if (_curToken.TokenType == TokenType.STRING_RW) {
                NextToken();
                return new(true, DataType.STRING);
            } else if (_curToken.TokenType == TokenType.BOOL_RW) {
                NextToken();
                return new(true, DataType.BOOL);
            }

            // Otherwise error
            _src.Report(MsgLevel.ERROR, "Type Mark must be \"INTEGER\", \"FLOAT\", \"STRING\", or \"BOOL\"", true);
            return new(false);
        }
    }
}
