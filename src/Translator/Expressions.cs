using System.Text;
using System.Linq;

namespace jfc {
    /// <summary> Class responsible for translating the source code to LLVM IR </summary>
    public partial class Translator {
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
        public static string FactorConstString(string value) {
            StringBuilder sb = new();
            int escapeCount = value.Count(c => c == '\\');
            int padding = 128 - (value.Length - (escapeCount * 2));
            sb.Append($"c\"{value}");
            while (padding > 0) {
                sb.Append("\\00");
                padding--;
            }
            sb.AppendLine("\"");
            return sb.ToString();
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
                res = VectorScalarOp(l, r, op, dataType, lSize, "Multiply a vector by a scalar");
            } else {
                res = ScalarVectorOp(r, l, op, dataType, rSize, "Multiply a scalar by a vector");
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
                res = VectorScalarOp(l, r, op, dataType, lSize, "Compare a vector to a scalar");
            } else {
                res = ScalarVectorOp(r, l, op, dataType, rSize, "Compare a scalar to a vector");
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
                res = VectorScalarOp(l, r, op, dataType, lSize, "Add a vector and a scalar");
            } else {
                res = ScalarVectorOp(r, l, op, dataType, rSize, "Add a scalar and a vector");
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
                res = VectorScalarOp(l, r, op, DataType.INTEGER, lSize, "Bitwise operation on a vector and a scalar");
            } else {
                res = ScalarVectorOp(r, l, op, DataType.INTEGER, rSize, "Bitwise operation a scalar and a vector");
            }
            return res;
        }
    }
}
