using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

    /// <summary> Reporting Levels </summary>
    public enum MsgLevel {
        DEBUG,
        INFO,
        WARN,
        ERROR
    }

    /// <summary> Class for reading from the source code file </summary>
    public class SourceFileReader : IDisposable {
        private readonly StreamReader _sr;
        private readonly string _fName;
        private int _lineCount = 1;
        private int _charCount = 0;

        /// <summary> The current line count </summary>
        public int LineCount { get => _lineCount; }

        /// <summary> The current character counts </summary>
        public int CharCount { get => _charCount; }

        /// <summary> Releases all managed resources </summary>
        public void Dispose() {
            _sr.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary> Opens the reader on the specified file </summary>
        /// <param name="sourceFile"> The file to be opened </param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="FileNotFoundException"/>
        /// <exception cref="DirectoryNotFoundException"/>
        /// <exception cref="IOException"/>
        public SourceFileReader(string sourceFile) {
            _sr = new(sourceFile);
            _fName = Path.GetFileName(sourceFile);
        }

        /// <summary> Reads the next character </summary>
        public int Read() {
            int c = _sr.Read();
            if (c == '\n') {
                _lineCount++;
                _charCount = 0;
            } else if (c != -1) {
                _charCount++;
            }
            return c;
        }

        /// <summary> Peeks at the next character </summary>
        public int Peek() => _sr.Peek();

        /// <summary> Reports a message </summary>
        /// <param name="lvl"> The severity of the message </param>
        /// <param name="msg"> The message to be printed </param>
        /// <param name="showFileInfo"> Whether or not to show the current file information </param>
        public void Report(MsgLevel lvl, string msg, bool showFileInfo = false) {
            StringBuilder sb = new();
            sb.Append(lvl switch {
                MsgLevel.DEBUG => "DEBUG",
                MsgLevel.INFO => " INFO",
                MsgLevel.WARN => " WARN",
                MsgLevel.ERROR => "ERROR",
                _ => throw new ArgumentException("Invalid enum", nameof(lvl))
            });
            if (showFileInfo) {
                sb.Append($" ({_fName}:{_lineCount}:{_charCount}): ");
            } else {
                sb.Append(": ");
            }
            sb.Append(msg);
            Console.WriteLine(sb.ToString());
        }
    }
}
