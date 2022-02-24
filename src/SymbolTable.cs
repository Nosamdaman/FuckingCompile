using System;
using System.Collections.Generic;

namespace jfc {
    /// <summary> The types of symbols </summary>
    public enum SymbolType { VARIABLE, PROCEDURE }

    /// <summary> Structure representing a symbol </summary>
    public readonly struct Symbol : IEquatable<Symbol> {
        private readonly string _name;
        private readonly SymbolType _type;

        /// <summary> The name of the symbol </summary>
        public string Name { get => _name; }

        /// <summary> The type of the symbol </summary>
        public SymbolType Type { get => _type; }

        public Symbol(string name, SymbolType type) {
            _name = name;
            _type = type;
        }

        public bool Equals(Symbol other) {
            return string.Equals(Name, other.Name, StringComparison.CurrentCultureIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            if (obj is Symbol other) {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode() => Name.GetHashCode();

        public static bool operator ==(Symbol lhs, Symbol rhs) => lhs.Equals(rhs);

        public static bool operator !=(Symbol lhs, Symbol rhs) => !lhs.Equals(rhs);
    }

    /// <summary> Table for managing symbols </summary>
    public class SymbolTable {
        private readonly Dictionary<Symbol, object> _global = new();
        private readonly Stack<Dictionary<Symbol, object>> _local = new();
    }
}
