using System.Collections.Generic;
using System.Text;

namespace jfc {
    /// <summary> Class responsible for translating the source code to LLVM IR </summary>
    public class Translator {
        private int _tempCount = 0;
        private int _globalCount = 0;
        private int _localCount = 0;
        private int _procedureCount = 0;
        private readonly StringBuilder _globals = new();
        private readonly StringBuilder _mainHeader = new();
        private readonly StringBuilder _mainBody = new();
        private readonly StringBuilder _finishedProcedures = new();
        private readonly Stack<StringBuilder> _procedures = new();

        /// <summary> Instantiates the class </summary>
        public Translator() {
            // Set up a bunch of shit
            _globals.AppendLine("; The following LLVM Assembly was automatically generated by jfc");
            _globals.AppendLine("target triple = \"x86_64-pc-linux-gnu\"\n");
            _globals.AppendLine("; Global Variable Declarations");
            _mainHeader.AppendLine("; Main Enty-Point Function");
            _mainHeader.AppendLine("define i32 @main() {");
        }

        /// <summary> Finishes building the output </summary>
        /// <returns> The finished assembly translation </returns>
        public string Finish() {
            // Smash everything together
            StringBuilder sb = new();
            sb.AppendLine(_globals.ToString());
            sb.Append(_mainHeader);
            _mainBody.AppendLine("\t; Return successfully");
            _mainBody.AppendLine("\tret i32 0");
            _mainBody.AppendLine("}\n");
            sb.Append(_mainBody);
            sb.Append(_finishedProcedures);
            return sb.ToString();
        }

        /// <summary> Declares a new variable </summary>
        /// <param name="variable"> The variable to be declared </param>
        /// <param name="isGlobal"> Whether or not the variable is in the global scope </param>
        public void DeclareVariable(Symbol variable, bool isGlobal) {
            if (isGlobal) {
                variable.AssemblyName = GetNextGlobal() + variable.Name;
                _globals.AppendLine($"{variable.AssemblyName} = private global {GetDefaultValue(variable)}");
            } else {
                variable.AssemblyName = GetNextLocal() + variable.Name;
                StringBuilder sb = _procedures.Peek();
                string dt = GetDataType(variable);
                sb.AppendLine($"\t{variable.AssemblyName} = alloca {dt}");
                sb.AppendLine($"\tstore {GetDefaultValue(variable)}, {dt}* {variable.AssemblyName}\n");
            }
        }

        /// <summary> Starts the translation of a procedure </summary>
        /// <param name="procedure"> The procedure to be translated </param>
        public void StartProcedure(Symbol procedure) {
            // First we'll create a string builder for the procedure
            StringBuilder sb = new();
            _procedures.Push(sb);

            // Set the procedure name
            procedure.AssemblyName = GetNextProcedure() + procedure.Name;

            // Now we'll start to build up the first line
            sb.Append("define ");
            sb.Append(GetDataType(procedure));
            sb.Append(" " + procedure.AssemblyName + "(");

            // Now add the arguments if there are any
            if (procedure.Parameters.Length > 0) {
                sb.Append(GetDataType(procedure.Parameters[0]));
                sb.Append(" %arg0");
                for (int idx = 1; idx < procedure.Parameters.Length; idx++) {
                    sb.Append($", {GetDataType(procedure.Parameters[idx])} %arg{idx}");
                }
            }

            // Now finish the header
            sb.AppendLine(") {");

            // Now we need to convert our arguments to memory locations
            for (int idx = 0; idx < procedure.Parameters.Length; idx++) {
                Symbol arg = procedure.Parameters[idx];
                arg.AssemblyName = $"%a{idx}-{arg.Name}";
                string dt = GetDataType(arg);
                sb.AppendLine($"\t{arg.AssemblyName} = alloca {dt}");
                sb.AppendLine($"\tstore {dt} %arg{idx}, {dt}* {arg.AssemblyName}\n");
            }
        }

        /// <summary> Finishes building the current procedure </summary>
        /// <param name="procedure"> The procedure to be translated </param>
        public void FinishProcedure(Symbol procedure) {
            StringBuilder sb = _procedures.Pop();
            sb.AppendLine($"\tret {GetDefaultValue(procedure)}");
            sb.AppendLine("}");
            _finishedProcedures.AppendLine(sb.ToString());
        }

        public string VariableReference(Symbol variable) {
            StringBuilder sb = GetBuilder();
            string result = GetNextTemp();
            sb.Append($"\t{result} TODO: VARIABLE REFERENCE");
            sb.AppendLine($" ; Reference to \"{variable.Name}\"");
            return result;
        }

        public string ProcedureReference(Symbol procedure) {
            StringBuilder sb = GetBuilder();
            string result = GetNextTemp();
            sb.Append($"\t{result} TODO: Procedure REFERENCE");
            sb.AppendLine($" ; Reference to \"{procedure.Name}\"");
            return result;
        }

        public string Factor() {
            StringBuilder sb = GetBuilder();
            string result = GetNextTemp();
            sb.Append($"\t{result} TODO: Factors");
            sb.AppendLine($" ; Factor");
            return result;
        }

        public string Term() {
            StringBuilder sb = GetBuilder();
            string result = GetNextTemp();
            sb.Append($"\t{result} TODO: Terms");
            sb.AppendLine($" ; Term");
            return result;
        }

        public string Relation() {
            StringBuilder sb = GetBuilder();
            string result = GetNextTemp();
            sb.Append($"\t{result} TODO: Relations");
            sb.AppendLine($" ; Relation");
            return result;
        }

        public string ArithOp() {
            StringBuilder sb = GetBuilder();
            string result = GetNextTemp();
            sb.Append($"\t{result} TODO: Aritmetic operations");
            sb.AppendLine($" ; Aritmetic operation");
            return result;
        }

        public string Expression() {
            StringBuilder sb = GetBuilder();
            string result = GetNextTemp();
            sb.Append($"\t{result} TODO: Expression");
            sb.AppendLine($" ; Expression");
            return result;
        }

        /// <summary> Gets the currently active string builder </summary>
        /// <returns> The currently active string builder </returns>
        private StringBuilder GetBuilder() {
            if (_procedures.Count != 0)
                return _procedures.Peek();
            else
                return _mainBody;
        }

        /// <summary> Gets the default value for a symbol </summary>
        /// <param name="symbol"> The symbol whose default value is to be retrieved </param>
        /// <returns> The default value for the symbol </returns>
        private static string GetDefaultValue(Symbol symbol) {
            // Get the data type and array size of the symbol
            DataType dt = symbol.DataType;
            int arraySize = 0;
            if (symbol.SymbolType == SymbolType.VARIABLE && symbol.IsArray) {
                arraySize = symbol.ArraySize;
            }

            // Get the base default value
            string baseVal = GetDataType(dt, 0) + " " + dt switch {
                DataType.BOOL => "true",
                DataType.INTEGER => "0",
                DataType.FLOAT => "0.0",
                DataType.STRING => "TODO: STRING DEFAULT",
                _ => throw new System.Exception("How the fuck did you even get here?")
            };

            // If we don't have an array, then we're good to go
            if (arraySize == 0) return baseVal;

            // Otherwise we'll build the array
            StringBuilder sb = new();
            sb.Append(GetDataType(symbol) + " [ ");
            sb.Append(baseVal);
            arraySize--;
            while (arraySize > 0) {
                sb.Append(", " + baseVal);
                arraySize--;
            }
            sb.Append(" ]");
            return sb.ToString();
        }

        /// <summary> Gets a new temporary register </summary>
        /// <returns> The name of the new register </returns>
        private string GetNextTemp() {
            string register = $"%{_tempCount}";
            _tempCount++;
            return register;
        }

        /// <summary> Gets a prefix for the name of a new global variable </summary>
        /// <returns> The prefix for the name of a new global variable</returns>
        private string GetNextGlobal() {
            string prefix = $"@g{_globalCount}-";
            _globalCount++;
            return prefix;
        }

        /// <summary> Gets a prefix for the name of a new local variable </summary>
        /// <returns> The prefix for the name of a new local variable </returns>
        private string GetNextLocal() {
            string prefix = $"%l{_localCount}-";
            _localCount++;
            return prefix;
        }

        /// <summary> Gets a prefix for the name of a new procedure </summary>
        /// <returns> The prefix for the name of a new procedure </returns>
        private string GetNextProcedure() {
            string prefix = $"@p{_procedureCount}-";
            _procedureCount++;
            return prefix;
        }

        /// <summary> Converts a data type to LLVM assymbly </summary>
        /// <param name="symbol"> The symbol who's type is to be converted </param>
        /// <returns> The data type in LLVM assembly </returns>
        private static string GetDataType(Symbol symbol) {
            int arraySize = 0;
            if (symbol.SymbolType == SymbolType.VARIABLE && symbol.IsArray) {
                arraySize = symbol.ArraySize;
            }
            return GetDataType(symbol.DataType, arraySize);
        }

        /// <summary> Converts a data type to LLVM assembly </summary>
        /// <param name="dataType"> The data type </param>
        /// <param name="arraySize"> The array size </param>
        /// <returns> The data type in LLVM assembly </returns>
        private static string GetDataType(DataType dataType, int arraySize) {
            // First convert the data type
            string dt = dataType switch {
                DataType.BOOL => "i1",
                DataType.INTEGER => "i32",
                DataType.FLOAT => "float",
                DataType.STRING => "TODO",
                _ => throw new System.Exception("How the fuck did you get here?")
            };

            // If we don't have an array, then we're good to go
            if (arraySize == 0) return dt;

            // Otherwise build the array identifier
            return $"[{arraySize} x {dt}]";
        }
    }
}
