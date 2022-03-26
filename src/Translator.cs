using System.Collections.Generic;
using System.Text;

namespace jfc {
    /// <summary> Class responsible for translating the source code to LLVM IR </summary>
    public class Translator {
        private int _procedureCount = 0;
        private readonly StringBuilder _output = new();
        private readonly Stack<StringBuilder> _procedures = new();

        /// <summary> Finishes building the output </summary>
        /// <returns> The finished assembly translation </returns>
        public string Finish() {
            return _output.ToString();
        }

        /// <summary> Gets a prefix for the name of a new procedure </summary>
        /// <returns> The prefix for the name of a new procedure </summary>
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
                DataType.BOOL => "i8",
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

        /// <summary> Starts the translation of a procedure </summary>
        /// <param name="procedure"> The procedure to be translated </param>
        public void StartProcedure(Symbol procedure) {
            // First we'll create a string builder for the procedure
            StringBuilder sb = new();
            _procedures.Push(sb);

            // Set the procedure name
            procedure.IrVariable = GetNextProcedure() + procedure.Name;

            // Now we'll start to build up the first line
            sb.Append("declare ");
            sb.Append(GetDataType(procedure));
            sb.Append(" " + procedure.IrVariable + "(");

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
                arg.IrVariable = $"%a{idx}-{arg.Name}";
                string dt = GetDataType(arg);
                sb.AppendLine($"\t{arg.IrVariable} = alloca {dt}");
                sb.AppendLine($"\tstore {dt} %arg{idx}, {dt}* {arg.IrVariable}\n");
            }
        }

        /// <summary> Finishes building the current procedure </summary>
        public void FinishProcedure() {
            StringBuilder sb = _procedures.Pop();
            sb.AppendLine("\tTODO: DEFAULT RETURN");
            sb.AppendLine("}");
            _output.AppendLine(sb.ToString());
        }
    }
}
