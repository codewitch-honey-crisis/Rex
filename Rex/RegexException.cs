using System;
using System.Text;

namespace RX {
    [Serializable]
#if RXLIB
    public
#endif
    class RegexException : Exception {
        public RegexException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) {

        }
        public RegexException(int[] expecting, long position, int line, int column, string fileOrUrl) : base(_ExpectingMessage(_Expecting(expecting))) {
            Expecting = _Expecting(expecting);
            Position = position;
            Line = line;
            Column = column;
            FileOrUrl = fileOrUrl;
        }
        public RegexException(string[] expecting,long position, int line, int column, string fileOrUrl) : base(_ExpectingMessage(expecting)) {
            Expecting = expecting;
            Position = position;
            Line = line;
            Column = column;
            FileOrUrl = fileOrUrl;
        }
        public RegexException(string message, long position, int line, int column, string fileOrUrl) : base(message) {
            Expecting = null;
            Position = position;
            Line = line;
            Column = column;
            FileOrUrl = fileOrUrl;
        }
        static string[] _Expecting(int[] expecting) {
            var result = new string[expecting.Length];
            for(var i = 0; i < expecting.Length; ++i) {
                result[i] = char.ConvertFromUtf32(expecting[i]);
            }
            return result;
        }
        static string _ExpectingMessage(string[] expecting) {
            StringBuilder sb = new StringBuilder();
            string delim = "Expecting \"";
            for(var i = 0;i<expecting.Length;++i) {
                sb.Append(delim);
                sb.Append(expecting[i]);
                sb.Append("\"");
                if(i==expecting.Length-2) {
                    delim = ", or \"";
                } else
                    delim = ", \"";
            }
            return sb.ToString();
        }
        public string[] Expecting { get; private set; }
        public int Line { get; private set; }
        public int Column { get; private set; }
        public long Position { get; private set; }
        public string FileOrUrl { get; private set; }
    }
}
