using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RX {
#if RXLIB
    public
#endif
    class RegexSetIntersect : RegexSetElement {
        public RegexSetExpression SetExpression { get; set; }
        public IRegexSetElement Next { get { return NextElement; } set { NextElement = value; } }
        protected override bool IsChainable => true;

        protected override IRegexSetElement NextElement { get; set ; }

        protected override IEnumerable<KeyValuePair<int, int>> GetRanges() {
            if (SetExpression != null && Next!=null) {
                var ranges = new List<KeyValuePair<int, int>>(SetExpression.GetRanges());
                foreach (var kvp in Next.GetRanges()) {
                    var kvp2 = kvp;
                    if(_Intersects(ranges,ref kvp2)) {
                        yield return kvp2;
                    }
                }
            }
        }
        static bool _Intersects(List<KeyValuePair<int,int>> ranges, ref KeyValuePair<int,int> kvp) {
            for(var i = 0; i<ranges.Count;++i) {
                var r = ranges[i];
                if (r.Key > kvp.Value) return false;
                if(r.Value>=kvp.Value) {
                    kvp = new KeyValuePair<int, int>(Math.Max(r.Key, kvp.Key), Math.Min(r.Value, kvp.Value));
                    return true;
                }
            }
            return false;
        }
        public override void WriteTo(TextWriter writer) {
            writer.Write("&&");
            if (SetExpression != null) {
                SetExpression.WriteTo(writer);
            } else {
                writer.Write("[]");
            }
        }
        protected override IRegexSetElement Clone() {
            var result = new RegexSetIntersect();
            if(SetExpression!=null) {
                result.SetExpression = (RegexSetExpression)SetExpression.Clone();
            }
            return result;
        }
        protected override bool Equals(IRegexSetElement rhs) {
            if (object.ReferenceEquals(rhs, this)) return true;
            var other = rhs as RegexSetIntersect;
            if (object.ReferenceEquals(other, null)) return false;
            if (!object.ReferenceEquals(SetExpression, other.SetExpression)) {
                if (object.ReferenceEquals(SetExpression, null)) return false;
                if (!SetExpression.Equals(other.SetExpression)) return false;
            }
            if (object.ReferenceEquals(NextElement, other.NextElement)) return true;
            if (object.ReferenceEquals(NextElement, null)) return false;
            return NextElement.Equals(other.NextElement);
        }
    }
}
