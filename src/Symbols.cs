using System;

namespace jfc {
    /// <summary> The types of symbols </summary>
    public enum SymbolType { VARIABLE, PROCEDURE }

    /// <summary> Data Types </summary>
    public enum DataType {
        STRING = 0,
        BOOL = 1,
        INTEGER = 2,
        FLOAT = 3
    }

    /// <summary> Class representing a symbol </summary>
    public class Symbol {
        private readonly string _name;
        private readonly SymbolType _symbolType;
        private readonly DataType _dataType;
        private readonly bool _isArray;
        private readonly int _arraySize;
        private readonly Symbol[] _parameters;

        /// <summary> The name of the symbol </summary>
        public string Name { get => _name; }

        /// <summary> The type of the symbol </summary>
        public SymbolType SymbolType { get => _symbolType; }

        /// <summary> The data type of the symbol </summary>
        public DataType DataType { get => _dataType; }

        /// <summary> Whether or not the variable is an array </summary>
        public bool IsArray {
            get {
                if (SymbolType == SymbolType.PROCEDURE) {
                    throw new NotSupportedException("Not valid for procedures");
                }
                return _isArray;
            }
        }

        /// <summary> The size of the array </summary>
        public int ArraySize {
            get {
                if (SymbolType == SymbolType.PROCEDURE) {
                    throw new NotSupportedException("Not valid for procedures");
                }
                if (!IsArray) {
                    throw new NotSupportedException("Not valid for non-array variables");
                }
                return _arraySize;
            }
        }

        /// <summary> The parameters of the procedure </summary>
        public Symbol[] Parameters {
            get {
                if (SymbolType == SymbolType.VARIABLE) {
                    throw new NotSupportedException("Not valid for variables");
                }
                return (Symbol[]) _parameters.Clone();
            }
        }

        /// <summary> The assembly name for the symbol </summary>
        public string AssemblyName { get; set; } = null;

        private Symbol(string name,
                       SymbolType symbolType,
                       DataType dataType,
                       bool isArray,
                       int arraySize,
                       Symbol[] parameters) {
            // Validate the inputs
            if (string.IsNullOrEmpty(name)) {
                throw new ArgumentNullException(nameof(name));
            }
            if ((name[0] < 'A' || name[0] > 'Z') && (name[0] < 'a' || name[0] > 'z')) {
                throw new ArgumentException("Invalid identifier", nameof(name));
            }
            foreach (char cur in name) {
                if ((cur < 'A' || cur > 'Z') && (cur < 'a' || cur > 'z') && (cur < '0' || cur > '9') && cur != '_') {
                    throw new ArgumentException("Invalid identifier", nameof(name));
                }
            }
            if (dataType == DataType.STRING && isArray) {
                throw new ArgumentException("String arrays are not allowed", nameof(arraySize));
            }
            if (symbolType == SymbolType.VARIABLE && isArray && arraySize < 1) {
                throw new ArgumentException("Array size must be greater than 0", nameof(arraySize));
            }
            if (symbolType == SymbolType.PROCEDURE) {
                if (parameters is null) parameters = Array.Empty<Symbol>();
                foreach (Symbol parameter in parameters) {
                    if (parameter.SymbolType == SymbolType.PROCEDURE) {
                        throw new ArgumentException("Parameters must be variables", nameof(parameters));
                    }
                }
            }

            // Assign the parameters
            _name = name;
            _symbolType = symbolType;
            _dataType = dataType;
            _isArray = isArray;
            _arraySize = arraySize;
            _parameters = parameters;
        }

        public static Symbol Variable(string name, DataType dataType) {
            return new(name, SymbolType.VARIABLE, dataType, false, 0, null);
        }

        public static Symbol VariableArray(string name, DataType dataType, int arraySize) {
            return new(name, SymbolType.VARIABLE, dataType, true, arraySize, null);
        }

        public static Symbol Procedure(string name, DataType dataType, Symbol[] parameters) {
            return new(name, SymbolType.PROCEDURE, dataType, false, 0, parameters);
        }

        public override int GetHashCode() => Name.GetHashCode();
    }
}
