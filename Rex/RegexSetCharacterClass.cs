using System.Collections.Generic;
using System.IO;

namespace RX {
#if RXLIB
    public
#endif
    class RegexSetCharacterClass : RegexSetElement {
        public string Class { get; set; }
        public IRegexSetElement Next { get { return NextElement; } set { NextElement = value; } }
        protected override bool IsChainable => true;

        protected override IRegexSetElement NextElement { get; set; }

        protected override IEnumerable<KeyValuePair<int, int>> GetRanges() {
            int[] pairs;
            if(RegexCharacterClasses.Known.TryGetValue(Class, out pairs)) {
                if (Next == null) {
                    return _Pairs(pairs);
                }
                return Combine(Collate(_Pairs(pairs),Next.GetRanges()));
            } else {
                return Next.GetRanges();
            }
        }
        IEnumerable<KeyValuePair<int,int>> _Pairs(int[] pairs) {
            for(var i = 0;i<pairs.Length;++i) {
                yield return new KeyValuePair<int, int>(pairs[i], pairs[++i]);
            }
        }
        public override void WriteTo(TextWriter writer) {
            writer.Write("[:");
            writer.Write(Class);
            writer.Write(":]");
        }
        protected override IRegexSetElement Clone() {
            var result = new RegexSetCharacterClass();
            result.Class = Class;
            if(Next!=null) {
                result.Next = Next.Clone();
            }
            return result;
        }
        protected override bool Equals(IRegexSetElement rhs) {
            if (object.ReferenceEquals(rhs, this)) return true;
            var other = rhs as RegexSetCharacterClass;
            if (object.ReferenceEquals(other, null)) return false;
            if(other.Class == Class) {
                if (object.ReferenceEquals(NextElement, other.NextElement)) return true;
                if (object.ReferenceEquals(NextElement, null)) return false;
                return NextElement.Equals(other.NextElement);
            }
            return false;
        }
    }
}
