using System.Collections.Generic;
using System.IO;

namespace RX {
#if RXLIB
    public
#endif
    class RegexSetNegate : RegexSetElement {
		public IRegexSetElement Next { get { return NextElement; } set { NextElement = value; } }
		protected override bool IsChainable => true;

        protected override IRegexSetElement NextElement { get; set; }

        protected override IEnumerable<KeyValuePair<int, int>> GetRanges() {
			if (Next == null) {
				yield break;
            }
			var last = 0x10ffff;
			
			using (var e = Next.GetRanges().GetEnumerator()) {
				if (!e.MoveNext()) {
					yield return new KeyValuePair<int, int>(0x0, 0x10ffff);
					yield break;
				}
				if (e.Current.Key > 0) {
					yield return new KeyValuePair<int, int>(0, unchecked(e.Current.Key - 1));
					last = e.Current.Value;
					if (0x10ffff <= last)
						yield break;
				} else if (e.Current.Key == 0) {
					last = e.Current.Value;
					if (0x10ffff <= last)
						yield break;
				}
				while (e.MoveNext()) {
					if (0x10ffff <= last)
						yield break;
					if (unchecked(last + 1) < e.Current.Key)
						yield return new KeyValuePair<int, int>(unchecked(last + 1), unchecked((e.Current.Key - 1)));
					last = e.Current.Value;
				}
				if (0x10ffff > last)
					yield return new KeyValuePair<int, int>(unchecked((last + 1)), 0x10ffff);

			}
		}
        public override void WriteTo(TextWriter writer) {
			writer.Write("^");
        }
        protected override IRegexSetElement Clone() {
			var result = new RegexSetNegate();
			if(Next!=null) {
				result.Next = Next.Clone();
            }
			return result;
        }
		protected override bool Equals(IRegexSetElement rhs) {
			if (object.ReferenceEquals(rhs, this)) return true;
			var other = rhs as RegexSetNegate;
			if (object.ReferenceEquals(other, null)) return false;
			if (object.ReferenceEquals(NextElement, other.NextElement)) return true;
			if (object.ReferenceEquals(NextElement, null)) return false;
			return NextElement.Equals(other.NextElement);
		}
	}
}
