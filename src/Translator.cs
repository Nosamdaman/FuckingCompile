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

        /// <summary> Casts an integer to a floating-point value </summary>
        public string IntToFloat(string reg, int arraySize) {
            StringBuilder sb = GetBuilder();
            string res = GetNextTemp();
            string dti = GetDataType(DataType.INTEGER, arraySize);
            string dtf = GetDataType(DataType.FLOAT, arraySize);
            sb.AppendLine($"\t{res} = sitofp {dti} {reg} to {dtf} ; Cast int to float");
            return res;
        }

        public string BoolToInt() {
            StringBuilder sb = GetBuilder();
            string result = GetNextTemp();
            sb.Append($"\t{result} TODO: Bool to Int");
            sb.AppendLine($" ; Cast boolean to integer");
            return result;
        }

        /// <summary> Translates a variable reference to assembly </summary>
        public string VariableReference(Symbol variable) {
            StringBuilder sb = GetBuilder();
            string dt = GetDataType(variable);
            string result = GetNextTemp();
            sb.AppendLine($"\t{result} = load {dt}, {dt}* {variable.AssemblyName} ; Reference to \"{variable.Name}\"");
            return result;
        }

        /// <summary> Translates an array index to assembly </summary>
        public string VectorIndex(string vec, string idx, DataType dataType, int vectorSize) {
            StringBuilder sb = GetBuilder();
            string dt = GetDataType(dataType, vectorSize);
            string res = GetNextTemp();
            sb.AppendLine($"\t{res} = extractelement {dt} {vec}, i32 {idx} ; Array index");
            return res;
        }

        /// <summary> Translates a procedure reference to assembly </summary>
        public string ProcedureReference(Symbol procedure, Queue<string> args) {
            // Start at the beginning
            StringBuilder sb = GetBuilder();
            string dt = GetDataType(procedure);
            string res = GetNextTemp();
            sb.Append($"\t{res} = call {dt} {procedure.AssemblyName}(");

            // Add any arguments
            if (args.Count > 0) {
                sb.Append($"{GetDataType(procedure.Parameters[0])} {args.Dequeue()}");
                for (int idx = 1; idx < procedure.Parameters.Length; idx++) {
                    string adt = GetDataType(procedure.Parameters[idx]);
                    sb.Append($", {adt} {args.Dequeue()}");
                }
            }

            // Finish the call
            sb.AppendLine($") ; Call to \"{procedure.Name}\"");
            return res;
        }

        /// <summary> Translates a constant boolean into assembly </summary>
        public static string FactorConstBool(bool value) {
            if (value) {
                return "true";
            } else {
                return "false";
            }
        }

        /// <summary> Translates a constant integer into assembly </summary>
        public static string FactorConstInt(int value) => value.ToString();

        /// <summary> Translates a constant floating-point number into assembly </summary>
        public static string FactorConstFloat(double value) => value.ToString();

        /// <summary> Translates a constant string into assembly </summary>
        public string FactorConstString(string value) {
            StringBuilder sb = GetBuilder();
            string str = GetNextTemp();
            int l = value.Length + 1;
            sb.AppendLine($"\t{str} = [{l} x i8] c\"{value}\\00\" ; String constant");
            string ptr = GetNextTemp();
            sb.AppendLine($"\t{ptr} = getelementptr [{l} x i8], [{l} x i8]* {str}, i32 0, i32 0 ; String pointer");
            return ptr;
        }

        /// <summary> Inverts an integer </summary>
        public string FactorNegInt(string reg) {
            StringBuilder sb = GetBuilder();
            string res = GetNextTemp();
            sb.AppendLine($"\t{res} = mul i32 {reg}, -1 ; Invert integer value");
            return res;
        }

        /// <summary> Inverts a floating-point number </summary>
        public string FactorNegFloat(string reg) {
            StringBuilder sb = GetBuilder();
            string res = GetNextTemp();
            sb.AppendLine($"\t{res} = fneg float {reg} ; Invert floating-point value");
            return res;
        }

        /// <summary> Multiplies or divides two numbers </summary>
        public string Term(TokenType operation, string l, string r, DataType dataType, int lSize, int rSize) {
            StringBuilder sb = GetBuilder();
            string res;
            string op = (operation, dataType) switch {
                (TokenType.TIMES, DataType.INTEGER) => "mul",
                (TokenType.DIVIDE, DataType.INTEGER) => "sdiv",
                (TokenType.TIMES, DataType.FLOAT) => "fmul",
                (TokenType.DIVIDE, DataType.FLOAT) => "fdiv",
                _ => throw new System.Exception("How the fuck did you even get here?")
            };
            if (lSize == rSize) {
                res = GetNextTemp();
                string dt = GetDataType(dataType, lSize);
                sb.AppendLine($"\t{res} = {op} {dt} {l}, {r} ; Multiply");
            } else if (lSize != 0) {
                sb.AppendLine("\t; Multiply a vector by a scalar");
                res = VectorScalarOp(l, r, op, dataType, lSize);
            } else {
                sb.AppendLine("\t; Multiply a scalar by a vector");
                res = ScalarVectorOp(r, l, op, dataType, rSize);
            }
            return res;
        }

        /// <summry> Compares two values </summary>
        public string Relation(TokenType operation, string l, string r, DataType dataType, int lSize, int rSize) {
            StringBuilder sb = GetBuilder();
            string res;
            string op = (operation, dataType) switch {
                (TokenType.EQ, DataType.BOOL) => "icmp eq",
                (TokenType.NEQ, DataType.BOOL) => "icmp ne",
                (TokenType.GT, DataType.BOOL) => "icmp ugt",
                (TokenType.LT, DataType.BOOL) => "icmp ult",
                (TokenType.GT_EQ, DataType.BOOL) => "icmp uge",
                (TokenType.LT_EQ, DataType.BOOL) => "icmp ule",
                (TokenType.EQ, DataType.INTEGER) => "icmp eq",
                (TokenType.NEQ, DataType.INTEGER) => "icmp ne",
                (TokenType.GT, DataType.INTEGER) => "icmp sgt",
                (TokenType.LT, DataType.INTEGER) => "icmp slt",
                (TokenType.GT_EQ, DataType.INTEGER) => "icmp sge",
                (TokenType.LT_EQ, DataType.INTEGER) => "icmp sle",
                (TokenType.EQ, DataType.FLOAT) => "fcmp oeq",
                (TokenType.NEQ, DataType.FLOAT) => "fcmp one",
                (TokenType.GT, DataType.FLOAT) => "fcmp ogt",
                (TokenType.LT, DataType.FLOAT) => "fcmp olt",
                (TokenType.GT_EQ, DataType.FLOAT) => "fcmp oge",
                (TokenType.LT_EQ, DataType.FLOAT) => "fcmp ole",
                _ => throw new System.Exception("How the fuck did you even get here?")
            };
            if (lSize == rSize) {
                res = GetNextTemp();
                string dt = GetDataType(dataType, lSize);
                sb.AppendLine($"\t{res} = {op} {dt} {l}, {r} ; Compare");
            } else if (lSize != 0) {
                sb.AppendLine("\t; Compare a vector to a scalar");
                res = VectorScalarOp(l, r, op, dataType, lSize);
            } else {
                sb.AppendLine("\t; Compare a scalar to a vector");
                res = ScalarVectorOp(r, l, op, dataType, rSize);
            }
            return res;
        }

        /// <summary> Compares two strings </summary>
        public string RelationString() {
            StringBuilder sb = GetBuilder();
            string result = GetNextTemp();
            sb.Append($"\t{result} TODO: String Relations");
            sb.AppendLine($" ; Relation");
            return result;
        }

        /// <summary> Adds or subracts two values </summary>
        public string ArithOp(TokenType operation, string l, string r, DataType dataType, int lSize, int rSize) {
            StringBuilder sb = GetBuilder();
            string res;
            string op = (operation, dataType) switch {
                (TokenType.PLUS, DataType.INTEGER) => "add",
                (TokenType.MINUS, DataType.INTEGER) => "sub",
                (TokenType.PLUS, DataType.FLOAT) => "fadd",
                (TokenType.MINUS, DataType.FLOAT) => "fsub",
                _ => throw new System.Exception("How the fuck did you even get here?")
            };
            if (lSize == rSize) {
                res = GetNextTemp();
                string dt = GetDataType(dataType, lSize);
                sb.AppendLine($"\t{res} = {op} {dt} {l}, {r} ; Add");
            } else if (lSize != 0) {
                sb.AppendLine("\t; Add a vector and a scalar");
                res = VectorScalarOp(l, r, op, dataType, lSize);
            } else {
                sb.AppendLine("\t; Add a scalar and a vector");
                res = ScalarVectorOp(r, l, op, dataType, rSize);
            }
            return res;
        }

        /// <summary> Inverts an integer </summary>
        public string ExpressionInv(string reg, int size) {
            StringBuilder sb = GetBuilder();
            string res = GetNextTemp();

            // If we don't have a vector, this is simple
            if (size == 0) {
                sb.AppendLine($"\t{res} = xor i32 {reg}, -1 ; Invert integer");
            } else {
                string dt = GetDataType(DataType.INTEGER, size);
                sb.Append($"\t{res} = xor {dt} {reg}, < i32 -1");
                size--;
                while (size > 0) { sb.Append(", i32 -1"); size--; }
                sb.AppendLine(" > ; Invert integers");
            }

            return res;
        }

        /// <summary> Performs a bitwise operation on two integers </summary>
        public string Expression(TokenType operation, string l, string r, int lSize, int rSize) {
            StringBuilder sb = GetBuilder();
            string res;
            string op = operation switch {
                TokenType.AND => "and",
                TokenType.OR => "or",
                _ => throw new System.Exception("How the fuck did you even get here?")
            };
            if (lSize == rSize) {
                res = GetNextTemp();
                string dt = GetDataType(DataType.INTEGER, lSize);
                sb.AppendLine($"\t{res} = {op} {dt} {l}, {r} ; Bitwise operation");
            } else if (lSize != 0) {
                sb.AppendLine("\t; Bitwise operation on a vector and a scalar");
                res = VectorScalarOp(l, r, op, DataType.INTEGER, lSize);
            } else {
                sb.AppendLine("\t; Bitwise operation a scalar and a vector");
                res = ScalarVectorOp(r, l, op, DataType.INTEGER, rSize);
            }
            return res;
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
            sb.Append(GetDataType(symbol) + " < ");
            sb.Append(baseVal);
            arraySize--;
            while (arraySize > 0) {
                sb.Append(", " + baseVal);
                arraySize--;
            }
            sb.Append(" >");
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
            return $"<{arraySize} x {dt}>";
        }

        private string VectorScalarOp(string vec, string reg, string op, DataType dataType, int arraySize) {
            StringBuilder sb = GetBuilder();
            string dt = GetDataType(dataType, 0);
            string dtv = GetDataType(dataType, arraySize);
            string res = vec;
            for (int idx = 0; idx < arraySize; idx++) {
                string tmp0 = GetNextTemp();
                sb.AppendLine($"\t{tmp0} = extractelement {dtv} {res}, i32 {idx}");
                string tmp1 = GetNextTemp();
                sb.AppendLine($"\t{tmp1} = {op} {dt} {tmp0}, {reg}");
                string tmp2 = GetNextTemp();
                sb.AppendLine($"\t{tmp2} = insertelement {dtv} {res}, i32 {tmp1}, i32 {idx}");
                res = tmp2;
            }
            return res;
        }

        private string ScalarVectorOp(string vec, string reg, string op, DataType dataType, int arraySize) {
            StringBuilder sb = GetBuilder();
            string dt = GetDataType(dataType, 0);
            string dtv = GetDataType(dataType, arraySize);
            string res = vec;
            for (int idx = 0; idx < arraySize; idx++) {
                string tmp0 = GetNextTemp();
                sb.AppendLine($"\t{tmp0} = extractelement {dtv} {res}, i32 {idx}");
                string tmp1 = GetNextTemp();
                sb.AppendLine($"\t{tmp1} = {op} {dt} {reg}, {tmp0}");
                string tmp2 = GetNextTemp();
                sb.AppendLine($"\t{tmp2} = insertelement {dtv} {res}, i32 {tmp1}, i32 {idx}");
                res = tmp2;
            }
            return res;
        }
    }
}
