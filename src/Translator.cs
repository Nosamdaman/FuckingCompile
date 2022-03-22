using System.Text;

namespace jfc {
    /// <summary> Class responsible for translating the source code to LLVM IR </summary>
    public class Translator {
        private readonly StringBuilder _sb = new();
        private int _curVariable = 0;

        /// <summary> Class constructor </summary>
        public Translator() {
            // Add the main function definition
            _sb.AppendLine("define i32 @main() {");
        }

        /// <summary> Gets the next available variable name </summary>
        private string GetNextVariable(bool isGlobal) {
            string prefix = isGlobal ? "@" : "%";
            string next = _curVariable.ToString();
            _curVariable++;
            return prefix + next;
        }

        /// <summary> Gets the type of a symbol </summary>
        private static string GetSymbolType(Symbol symbol) {
            // Get the string representation of the symbol's type
            string type = symbol.DataType switch {
                DataType.BOOL => "i1",
                DataType.INTEGER => "i32",
                DataType.FLOAT => "float",
                DataType.STRING => "i8*",
                _ => throw new System.Exception("How the fuck did you get here?")
            };

            // Determine whether or not the symbol is an array
            bool isArray = false;
            if (symbol.SymbolType != SymbolType.PROCEDURE) { isArray = symbol.IsArray; }
            if (!isArray) return type;
            string size = symbol.ArraySize.ToString();
            return "[" + size + " x " + type + "]";
        }

        /// <summary> Initializes a variable </summary>
        public void InitializeVariable(Symbol variable, bool isGlobal) {
            // First get the name of the variable
            string name = GetNextVariable(isGlobal);
            variable.IrVariable = name;
            _sb.Append(name + " = ");

            // Then get the type
            _sb.Append(GetSymbolType(variable) + " ");

            // Get the default value for the data type
            string defaultValue = variable.DataType switch {
                DataType.BOOL => "1",
                DataType.INTEGER => "0",
                DataType.FLOAT => "0.0",
                DataType.STRING => "null",
                _ => throw new System.Exception("How the fuck did you get here?")
            };

            // If it's not an array, then the rest is simple
            if (!variable.IsArray) {
                _sb.AppendLine(defaultValue);
                return;
            }

            // If it is an array, then we need to get a little creative
            string type = variable.DataType switch {
                DataType.BOOL => "i1",
                DataType.INTEGER => "i32",
                DataType.FLOAT => "float",
                _ => throw new System.Exception("How the fuck did you get here?")
            };
            _sb.Append("[ " + type + " " + defaultValue);
            for (int idx = 0; idx < variable.ArraySize - 1; idx++) {
                _sb.Append(", " + type + " " + defaultValue);
            }
            _sb.AppendLine(" ]");
        }

        /// <summary> Returns the final LLVM IR as a string </summary>
        public string Finish() {
            _sb.AppendLine("ret i32 0");
            _sb.AppendLine("}");
            return _sb.ToString();
        }
    }
}
