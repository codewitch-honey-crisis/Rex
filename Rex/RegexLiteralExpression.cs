using System.IO;

namespace RX {
#if RXLIB
    public
#endif
    class RegexLiteralExpression : RegexExpression {
        public override bool ShouldGroup => false;
        /// <summary>
        /// Indicates the codepoint to be matched
        /// </summary>
        public int Codepoint { get; set; } = 0;
        public RegexLiteralExpression() { }
        public RegexLiteralExpression(int codepoint) {
            Codepoint = codepoint;
        }
        public override void WriteTo(TextWriter writer) {
            WriteEscapedCodepoint(Codepoint,writer);
        }
        public override FA ToFA(int accept = 0) {
            var result = new FA();
            var final = new FA();
            final.AcceptSymbolId = accept;
            result.AddTransition(Codepoint, Codepoint, final);
            return result;
        }
        public override bool Equals(RegexExpression rhs) {
            return Equals(rhs as RegexLiteralExpression);
        }
        public bool Equals(RegexLiteralExpression rhs) {
            if (object.ReferenceEquals(this, rhs)) return true;
            if (object.ReferenceEquals(rhs, null)) return false;
            return Codepoint == rhs.Codepoint;
        }
        public override RegexExpression Clone() {
            var result = new RegexLiteralExpression();
            result.Codepoint = Codepoint;
            return result;
        }
        public override bool TryReduce(out RegexExpression reduced) {
            reduced = this;
            return false;
        }
    }
}
