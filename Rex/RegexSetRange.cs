using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RX {
    public class RegexSetRange : RegexSetElement {
        public int First { get; set; }
        public int Last { get; set; }
        public IRegexSetElement Next { get { return NextElement; } set { NextElement = value; } }
        protected override bool IsChainable => true;
        protected override IRegexSetElement NextElement { get; set; }
        protected override IEnumerable<KeyValuePair<int, int>> GetRanges() {
            int first, last;
            if(First<=Last) {
                first = First;
                last = Last;
            } else {
                last = First;
                first = Last;
            }
            if (Next != null) {
                return Combine(Collate(new KeyValuePair<int, int>[] { new KeyValuePair<int, int>(first, last) }, Next.GetRanges()));
            } 
            else {
                return new KeyValuePair<int, int>[] { new KeyValuePair<int, int>(first, last) };
            }
        }
        protected override IRegexSetElement Clone() {
			var result = new RegexSetRange();
			result.First = First;
			result.Last = Last;
			if(Next!=null) {
				result.Next = Next.Clone();
            }
			return result;
        }
        static void _WriteEscapedCodepoint(int codepoint,TextWriter writer) {
			switch (codepoint) {
			case '.':
			case '[':
			case ']':
			case '^':
			case '-':
			case '\\':
				writer.Write('\\');
				writer.Write(char.ConvertFromUtf32(codepoint));
				break;
			case '\t':
				writer.Write("\\t");
				break;
			case '\n':
				writer.Write("\\n");
				break;
			case '\r':
				writer.Write("\\r");
				break;
			case '\0':
				writer.Write("\\0");
				break;
			case '\f':
				writer.Write("\\f");
				break;
			case '\v':
				writer.Write("\\v");
				break;
			case '\b':
				writer.Write("\\b");
				break;
			default:
				var s = char.ConvertFromUtf32(codepoint);
				if (!char.IsLetterOrDigit(s, 0) && !char.IsSeparator(s, 0) && !char.IsPunctuation(s, 0) && !char.IsSymbol(s, 0)) {
					if (s.Length == 1) {
						writer.Write("\\u");
						writer.Write(unchecked((ushort)codepoint).ToString("x4"));
					} else {
						writer.Write("\\U");
						writer.Write(codepoint.ToString("x8"));
					}

				} else
					writer.Write(s);
				break;
			}
		}
        public override void WriteTo(TextWriter writer) {
            if(First==Last) {
				_WriteEscapedCodepoint(First, writer);
				return;
            }
			int first, last;
			if (First <= Last) {
				first = First;
				last = Last;
			} else {
				last = First;
				first = Last;
			}
			if(first+1==last) {
				_WriteEscapedCodepoint(First, writer);
				_WriteEscapedCodepoint(Last, writer);
				return;
			}
			_WriteEscapedCodepoint(First, writer);
			writer.Write("-");
			_WriteEscapedCodepoint(Last, writer);
		}
		protected override bool Equals(IRegexSetElement rhs) {
			if (object.ReferenceEquals(rhs, this)) return true;
			var other = rhs as RegexSetRange;
			if (object.ReferenceEquals(other, null)) return false;
			if (First != other.First || Last != other.Last) return false;
			if (object.ReferenceEquals(NextElement, other.NextElement)) return true;
			if (object.ReferenceEquals(NextElement, null)) return false;
			return NextElement.Equals(other.NextElement);
		}
	}
}
