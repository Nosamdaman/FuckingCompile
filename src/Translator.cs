using System.Collections.Generic;
using System.Text;

namespace jfc {
    /// <summary> Class responsible for translating the source code to LLVM IR </summary>
    public class Translator {
        private int _tempCount = 1;
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
            sb.Append(_lib);
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

        /// <summary> Writes a comment </summary>
        public void Comment(string msg) {
            StringBuilder sb = GetBuilder();
            sb.AppendLine(msg);
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

        /// <summary> Casts a floating-point to an integer </summary>
        public string FloatToInt(string reg, int size) {
            StringBuilder sb = GetBuilder();
            string res = GetNextTemp();
            string dtf = GetDataType(DataType.FLOAT, size);
            string dti = GetDataType(DataType.INTEGER, size);
            sb.AppendLine($"\t{res} = fptosi {dtf} {reg} to {dti} ; Cast float to int");
            return res;
        }

        /// <summary> Casts a boolean to an integer </summary>
        public string BoolToInt(string reg, int size) {
            StringBuilder sb = GetBuilder();
            string res = GetNextTemp();
            string dtb = GetDataType(DataType.BOOL, size);
            string dti = GetDataType(DataType.INTEGER, size);
            sb.AppendLine($"\t{res} = zext {dtb} {reg} to {dti} ; Cast bool to int");
            return res;
        }

        /// <summary> Casts an integer to a boolean </summary>
        public string IntToBool(string reg, int size) {
            StringBuilder sb = GetBuilder();
            string res = GetNextTemp();
            if (size == 0) {
                sb.AppendLine($"\t{res} = icmp ne i32 {reg}, 0 ; Cast bool to int");
            } else {
                string dt = GetDataType(DataType.INTEGER, size);
                sb.Append($"\t{res} = icmp ne {dt} {reg}, < i32 0");
                size--;
                while (size > 0) { sb.Append(", i32 0"); size--; }
                sb.AppendLine(" > ; Cast bool to int");
            }
            return res;
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
        public static string FactorConstFloat(double value) {
            long l = System.BitConverter.DoubleToInt64Bits(value);
            string hex = l.ToString("x16")[0..9];
            return "0x" + hex + "0000000";
        }

        /// <summary> Translates a constant string into assembly </summary>
        public string FactorConstString(string value) {
            StringBuilder sb = GetBuilder();
            string str = GetNextTemp();
            int l = value.Length + 1;
            sb.AppendLine($"\t{str} = alloca [{l} x i8] ; String constant");
            sb.AppendLine($"\tstore [{l} x i8] c\"{value}\\00\", [{l} x i8]* {str} ; String value");
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

        /// <summary> Updates the value for a variable </summary>
        public void Assignment(Symbol target, string reg) {
            StringBuilder sb = GetBuilder();
            string dt = GetDataType(target);
            string src = target.AssemblyName;
            sb.AppendLine($"\tstore {dt} {reg}, {dt}* {src} ; Update variable \"{target.Name}\"");
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
                DataType.STRING => "i8*",
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
                sb.AppendLine($"\t{tmp2} = insertelement {dtv} {res}, {dt} {tmp1}, i32 {idx}");
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
                sb.AppendLine($"\t{tmp2} = insertelement {dtv} {res}, {dt} {tmp1}, i32 {idx}");
                res = tmp2;
            }
            return res;
        }

        private readonly string _lib =
        "; The following is an implementation of the language's standard library \n\n" +
        "; Import some functions from the standard libraries\n" +
        "declare i32 @printf(i8* nocapture, ...)\n" +
        "declare i32 @getchar()\n" +
        "\n" +
        "; Global strings\n" +
        "@str.true = private constant [5 x i8] c\"True\\00\"\n" +
        "@str.false = private constant [6 x i8] c\"False\\00\"\n" +
        "@str.int = private constant [3 x i8] c\"%i\\00\"\n" +
        "@str.floatf = private constant [3 x i8] c\"%f\\00\"\n" +
        "@str.floate = private constant [3 x i8] c\"%e\\00\"\n" +
        "\n" +
        "; Writes a boolean\n" +
        "define private i1 @putBool(i1 %bool) {\n" +
        "    ; Determine which string to print\n" +
        "    br i1 %bool, label %true, label %false\n" +
        "\n" +
        "    ; Flag is true\n" +
        "    true:\n" +
        "    %ptr1 = getelementptr [5 x i8], [5 x i8]* @str.true, i32 0, i32 0\n" +
        "    %retInt1 = call i32 (i8*, ...) @printf(i8* %ptr1)\n" +
        "    %ret1 = icmp sge i32 %retInt1, 0\n" +
        "    ret i1 %ret1\n" +
        "\n" +
        "    ; Flag is false\n" +
        "    false:\n" +
        "    %ptr0 = getelementptr [6 x i8], [6 x i8]* @str.false, i32 0, i32 0\n" +
        "    %retInt0 = call i32 (i8*, ...) @printf(i8* %ptr0)\n" +
        "    %ret0 = icmp sge i32 %retInt0, 0\n" +
        "    ret i1 %ret0\n" +
        "}\n" +
        "\n" +
        "; Writes an integer\n" +
        "define private i1 @putInteger(i32 %int) {\n" +
        "    %ptr = getelementptr [3 x i8], [3 x i8]* @str.int, i32 0, i32 0\n" +
        "    %retInt = call i32 (i8*, ...) @printf(i8* %ptr, i32 %int)\n" +
        "    %ret = icmp sge i32 %retInt, 0\n" +
        "    ret i1 %ret\n" +
        "}\n" +
        "\n" +
        "; Writes a floating-point number\n" +
        "define private i1 @putFloat(float %float) {\n" +
        "    ; If the number is 6 orders of magnitude above or below 1, we'll use scientific notation\n" +
        "    %min = fdiv float 1.0, 1.0e+6\n" +
        "    %cond0 = fcmp ole float %float, %min\n" +
        "    %cond1 = fcmp oge float %float, 1.0e+6\n" +
        "    %cond = or i1 %cond0, %cond1\n" +
        "    %ptrf = getelementptr [3 x i8], [3 x i8]* @str.floatf, i32 0, i32 0\n" +
        "    %ptre = getelementptr [3 x i8], [3 x i8]* @str.floate, i32 0, i32 0\n" +
        "    %ptr = select i1 %cond, i8* %ptre, i8* %ptrf\n" +
        "\n" +
        "    ; Print the number\n" +
        "    %double = fpext float %float to double\n" +
        "    %retInt = call i32 (i8*, ...) @printf(i8* %ptr, double %double)\n" +
        "    %ret = icmp sge i32 %retInt, 0\n" +
        "    ret i1 %ret\n" +
        "}\n" +
        "\n" +
        "; Writes a string\n" +
        "define private i1 @putString(i8* %str) {\n" +
        "    %retInt = call i32 (i8*, ...) @printf(i8* %str)\n" +
        "    %ret = icmp sge i32 %retInt, 0\n" +
        "    ret i1 %ret\n" +
        "}\n" +
        "\n" +
        "; Gets an integer\n" +
        "define private i32 @getInteger() {\n" +
        "    ; We'll start by writing some constants\n" +
        "    %errMsgStr = alloca [31 x i8]\n" +
        "    store [31 x i8] c\"Error reading int, try again: \\00\", [31 x i8]* %errMsgStr\n" +
        "    %errMsgPtr = getelementptr [31 x i8], [31 x i8]* %errMsgStr, i32 0, i32 0\n" +
        "\n" +
        "    ; We'll allocate for an integer\n" +
        "    %ptr = alloca i32\n" +
        "    store i32 0, i32* %ptr\n" +
        "\n" +
        "    ; This block is essentially the same as the main block, accept we'll handle the first character abit          differently\n" +
        "    %signPtr = alloca i32\n" +
        "    %int0 = call i32 @getchar()\n" +
        "    %char0 = trunc i32 %int0 to i8\n" +
        "    %condearlynewline = icmp eq i8 %char0, 10\n" +
        "    br i1 %condearlynewline, label %errorend, label %getsign\n" +
        "\n" +
        "    ; Determine the sign of the integer\n" +
        "    getsign:\n" +
        "    %condnegative = icmp eq i8 %char0, 45\n" +
        "    br i1 %condnegative, label %negative, label %positive\n" +
        "\n" +
        "    ; The integer is negative\n" +
        "    negative:\n" +
        "    store i32 -1, i32* %signPtr\n" +
        "    br label %main\n" +
        "\n" +
        "    ; The integer is positive\n" +
        "    positive:\n" +
        "    store i32 1, i32* %signPtr\n" +
        "    %condgt0 = icmp ugt i8 %char0, 47\n" +
        "    %condlt0 = icmp ult i8 %char0, 58\n" +
        "    %condinrange0 = and i1 %condgt0, %condlt0\n" +
        "    br i1 %condinrange0, label %continue0, label %error\n" +
        "\n" +
        "    ; Continue on\n" +
        "    continue0:\n" +
        "    %num0 = call i32 @charToInt(i8 %char0)\n" +
        "    %old0 = load i32, i32* %ptr\n" +
        "    %update0 = mul i32 %old0, 10\n" +
        "    %new0 = add i32 %num0, %update0\n" +
        "    store i32 %new0, i32* %ptr\n" +
        "    br label %main\n" +
        "\n" +
        "    ; Now we'll read a character\n" +
        "    main:\n" +
        "    %int = call i32 @getchar()\n" +
        "    %char = trunc i32 %int to i8\n" +
        "    %condnewline = icmp eq i8 %char, 10\n" +
        "    br i1 %condnewline, label %end, label %inrange\n" +
        "\n" +
        "    ; Newline was read, return\n" +
        "    end:\n" +
        "    %val = load i32, i32* %ptr\n" +
        "    %sign = load i32, i32* %signPtr\n" +
        "    %finalval = mul i32 %val, %sign\n" +
        "    ret i32 %finalval\n" +
        "\n" +
        "    ; Now ensure the character is a number\n" +
        "    inrange:\n" +
        "    %condgt = icmp ugt i8 %char, 47\n" +
        "    %condlt = icmp ult i8 %char, 58\n" +
        "    %condinrange = and i1 %condgt, %condlt\n" +
        "    br i1 %condinrange, label %continue, label %error\n" +
        "\n" +
        "    ; Continue on\n" +
        "    continue:\n" +
        "    %num = call i32 @charToInt(i8 %char)\n" +
        "    %old = load i32, i32* %ptr\n" +
        "    %update = mul i32 %old, 10\n" +
        "    %new = add i32 %num, %update\n" +
        "    store i32 %new, i32* %ptr\n" +
        "    br label %main\n" +
        "\n" +
        "    ; Error block for when we read a bad symbol\n" +
        "    error:\n" +
        "    %errint = call i32 @getchar()\n" +
        "    %errchar = trunc i32 %errint to i8\n" +
        "    %conderr = icmp eq i8 %errchar, 10\n" +
        "    br i1 %conderr, label %errorend, label %error\n" +
        "\n" +
        "    ; Print a message and try again\n" +
        "    errorend:\n" +
        "    call i32 (i8*, ...) @printf(i8* %errMsgPtr)\n" +
        "    %res = call i32 @getInteger()\n" +
        "    ret i32 %res\n" +
        "}\n" +
        "\n" +
        "; Converts a char to an int\n" +
        "define private i32 @charToInt(i8 %char) {\n" +
        "    %norm = sub i8 %char, 48\n" +
        "    %int = zext i8 %norm to i32\n" +
        "    ret i32 %int\n" +
        "}\n" +
        "\n" +
        "define float @makef(i32 %l, i32 %r, i32 %div) {\n" +
        "    %lf = sitofp i32 %l to float\n" +
        "    %rf = sitofp i32 %r to float\n" +
        "    %rdiv = sitofp i32 %div to float\n" +
        "    %dec = fdiv float %rf, %rdiv\n" +
        "    %num = fadd float %lf, %dec\n" +
        "    ret float %num\n" +
        "}\n";
    }
}
