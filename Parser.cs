using System;
using System.Collections.Generic;

namespace jfc {
    /// <summary> Parses the program </summary>
    public class Parser {
        private readonly Scanner _scanner;
        private Token _curToken = null;

        /// <summary> Creates a new parser on the given file </summary>
        /// <param name="src"> The  source code </param>
        /// <exception cref="ArgumentNullException"/>
        public Parser(SourceFileReader src) {
            _scanner = new Scanner(src);
        }

        private Token NextToken() {
            _curToken = _scanner.Scan();
            return _curToken;
        }
    }
}
