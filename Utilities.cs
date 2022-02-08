using System.Collections.Generic;

namespace jfc {
    /// <summary> Equality comparer for strings that ignores case </summary>
    public class StringNoCaseComparer : IEqualityComparer<string> {
        /// <summary> Determines if two strings are equal </summary>
        /// <param name="s0"> The first string to compare </param>
        /// <param name="s1"> The second string to compare </param>
        /// <returns> Whether or not the two strings are equivalent </returns>
        public bool Equals(string s0, string s1) {
            if (s0 == null && s1 == null) return true;
            if (s0 == null || s1 == null) return false;
            return s0.Equals(s1, System.StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary> Gets the hash code for a string </summary>
        /// <param name="s"> The string whose hash code is to be computed </param>
        /// <returns> The hash code of the string </returns>
        public int GetHashCode(string s) => s?.GetHashCode() ?? 0;
    }
}
