using System;
using System.IO;

namespace jfc
{
    /// <summary> Scans the document for tokens </summary>
    public class Scanner
    {
        private readonly FileStream _fs;
        private int lineCount = 1;

        /// <summary> Creates a new scanner on the given file </summary>
        /// <param name="fs"> The file stream containing the source code </param>
        /// <exception cref="ArgumentNullException"/>
        public Scanner(FileStream fs)
        {
            if (fs is null) throw new ArgumentNullException(nameof(fs));
            _fs = fs;
        }

        /// <summary> Reads until resolving the next token and returns it </summary>
        /// <returns> The next token in the program </summary>
        public Token Scan()
        {
            throw new NotImplementedException();
        }
    }
}
