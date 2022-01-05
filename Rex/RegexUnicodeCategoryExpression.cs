using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.IO;

namespace RX {
#if RXLIB
    public
#endif
    class RegexUnicodeCategoryExpression : RegexExpression {
        public UnicodeCategory Category { get; set; } = default(UnicodeCategory);
		public RegexUnicodeCategoryExpression() {

        }
		public RegexUnicodeCategoryExpression(UnicodeCategory category) {
			Category = category;
        }
        public override bool ShouldGroup => false;
        public override void WriteTo(TextWriter writer) {
            writer.Write(@"\p{");
			string s;
			// TODO: make this an array lookup
			switch (unchecked((int)Category)) {
			case 21: s = "Pe"; break;
			case 18: s = "Pc"; break;
			case 14: s = "Cc"; break;
			case 26: s = "Sc"; break;
			case 19: s = "Pd"; break;
			case 8: s = "Nd"; break;
			case 7: s = "Me"; break;
			case 23: s = "Pf"; break;
			case 15: s = "Cf"; break;
			case 22: s = "Pi"; break;
			case 9: s = "Nl"; break;
			case 12: s = "Zl";break;
			case 1: s = "Ll"; break;
			case 25: s = "Sm";break;
			case 3: s = "Lm"; break;
			case 27: s = "Sk"; break;
			case 5: s = "Mn"; break;
			case 20: s = "Ps";break;
			case 4: s = "Lo";break;
			case 29: s = "Cn";break;
			case 10: s = "No";break;
			case 24: s = "Po";break;
			case 28: s = "So";break;
			case 13: s = "Zp";break;
			case 17: s = "Co";break;
			case 11: s = "Zs";break;
			case 6: s = "Mc";break;
			case 16: s = "Cs";break;
			case 2: s = "Lt";break;
			case 0: s = "Lu";break;
			default:
				s = null;
				break;
			}
			writer.Write(s);
			writer.Write("}");
        }
        public override FA ToFA(int accept = 0) {
			var pra = RegexCharacterClasses.UnicodeCategories[(int)Category];
			var result = new FA();
			var final = new FA(accept);
			for(var i = 0;i<pra.Length;++i) {
				result.AddTransition(pra[i], pra[++i], final);
            }
			return result;
        }
        public override RegexExpression Clone() {
			var result = new RegexUnicodeCategoryExpression();
			result.Category = Category;
			return result;
        }
		public bool Equals(RegexUnicodeCategoryExpression rhs) {
			if (object.ReferenceEquals(rhs, this)) return true;
			if (object.ReferenceEquals(rhs, null)) return false;
			return Category == rhs.Category;
		}
        public override bool Equals(RegexExpression rhs) {
			return Equals(rhs as RegexUnicodeCategoryExpression);
        }
		public override bool TryReduce(out RegexExpression reduced) {
			reduced = this;
			return false;
		}
	}
}
