using System;
using System.IO;
using System.Collections.Generic;

namespace RX {
#if RXLIB
    public
#endif
    interface IRegexSetElement {
        bool IsChainable { get; }
        IRegexSetElement NextElement { get; set; }
        IEnumerable<KeyValuePair<int, int>> GetRanges();
        void WriteTo(TextWriter writer);
        IRegexSetElement Clone();
        bool Equals(IRegexSetElement rhs);
    }
#if RXLIB
    public
#endif
    abstract class RegexSetElement : IRegexSetElement {
        bool IRegexSetElement.IsChainable => IsChainable;
        protected abstract bool IsChainable { get; }
        IRegexSetElement IRegexSetElement.NextElement { get { return NextElement; } set { NextElement = value; } }
        public abstract void WriteTo(TextWriter writer);
        protected abstract IRegexSetElement NextElement { get; set; }
        protected abstract IRegexSetElement Clone();
        protected abstract bool Equals(IRegexSetElement rhs);
        IEnumerable<KeyValuePair<int, int>> IRegexSetElement.GetRanges() {
            return GetRanges();
        }
        bool IRegexSetElement.Equals(IRegexSetElement rhs) => Equals(rhs);
        IRegexSetElement IRegexSetElement.Clone() => Clone();
        protected abstract IEnumerable<KeyValuePair<int, int>> GetRanges();

        protected static IEnumerable<KeyValuePair<int, int>> Combine(IEnumerable<KeyValuePair<int, int>> rhs) {
            var rcur = rhs.GetEnumerator();
            var rmore = rcur.MoveNext();
            while(rmore) {
                var f = rcur.Current.Key;
                var l = rcur.Current.Value;
                var yielded = false;
                while(rmore=rcur.MoveNext()) {
                    if (l + 1 >= rcur.Current.Key) {
                        if (rcur.Current.Value > l) {
                            l = rcur.Current.Value;
                        }
                    } else {
                        yield return new KeyValuePair<int, int>(f, l);
                        yielded = true;
                        break;
                    }
                }
                if(!yielded) {
                    yield return new KeyValuePair<int, int>(f, l);
                }
                if(rmore) {
                    yield return rcur.Current;
                }

            }
        }
        protected static IEnumerable<KeyValuePair<int,int>> Collate(IEnumerable<KeyValuePair<int,int>> lhs, IEnumerable<KeyValuePair<int, int>> rhs) {
            var lcur = lhs.GetEnumerator();
            var rcur = rhs.GetEnumerator();
            var lmore = lcur.MoveNext();
            var rmore = rcur.MoveNext();
            while(true) {
                if(lmore) {
                    if(rmore) {
                        if(lcur.Current.Key==rcur.Current.Key) {
                            if(lcur.Current.Value>=rcur.Current.Value) {
                                yield return lcur.Current;
                            } else {
                                yield return rcur.Current;
                            }
                        } else if(lcur.Current.Key>rcur.Current.Key) {
                            yield return rcur.Current;
                            yield return lcur.Current;
                        } else {
                            yield return lcur.Current;
                            yield return rcur.Current;
                        }
                        rmore = rcur.MoveNext();
                    } else {
                        yield return lcur.Current;
                    }
                    lmore = lcur.MoveNext();
                } else {
                    if (rmore) {
                        yield return rcur.Current;
                        rmore = rcur.MoveNext();
                    } else
                        break;
                }
            }
        }
    }
#if RXLIB
    public
#endif
    class RegexSetExpression : RegexExpression {
        public override bool ShouldGroup => false;
        public IRegexSetElement First { get; set; } = null;
        public RegexSetExpression() {

        }
        public RegexSetExpression(IRegexSetElement first) {
            First = first;
        }
        public RegexSetExpression(IEnumerable<KeyValuePair<int,int>> ranges,bool negate=false) {
            IRegexSetElement cur = null;
            if(negate) {
                var neg = new RegexSetNegate();
                First = neg;
                cur = neg;
            }
            foreach(var range in ranges) {
                var r = new RegexSetRange();
                r.First = range.Key;
                r.Last = range.Value;
                if(cur==null) {
                    cur = r;
                    First = r;
                } else {
                    cur.NextElement = r;
                    cur = r;
                }
            }
        }
        public IEnumerable<KeyValuePair<int, int>> GetRanges() {
            if(First!=null) {
                return First.GetRanges();
            }
            return new KeyValuePair<int, int>[0];
        }
        public override void WriteTo(TextWriter writer) {
            var current = First;
            if(current!=null && current.NextElement==null || (current is RegexSetNegate && current.NextElement!=null && current.NextElement.NextElement==null)) {
                var c = current;
                var n = false;
                if (current is RegexSetNegate) {
                    n = true;
                    c = current.NextElement;
                }
                var cls = c as RegexSetCharacterClass;
                if (cls!=null) {
                    switch(cls.Class) {
                    case "digit":
                        writer.Write(!n ? @"\d" : @"\D");
                        break;
                    case "word":
                        writer.Write(!n ? @"\w" : @"\W");
                        break;
                    case "space":
                        writer.Write(!n ? @"\s" : @"\S");
                        break;
                    }
                    return;
                }
                var rng = c as RegexSetRange;
                if(rng!=null) {
                    if (rng.First == 0 && rng.Last == 0x10FFFF) {
                        writer.Write(".");
                    } else if(rng.First==rng.Last) {
                        WriteEscapedCodepoint(rng.First, writer);
                    }
                    return;
                }
            }
            writer.Write("[");

            while (current!=null) {
                current.WriteTo(writer);
                if (current.IsChainable) {
                    current = current.NextElement;
                } else
                    current = null;
            }
            writer.Write("]");
        }
        public override FA ToFA(int accept = 0) {
            var result = new FA();
            var final = new FA(accept);
            var moved = false;
            foreach(var rg in GetRanges()) {
                moved = true;
                result.AddTransition(rg.Key, rg.Value, final);
            }
            if (!moved) return final;
            return result;
            
        }
        public override RegexExpression Clone() {
            var result = new RegexSetExpression();
            if(First!=null) {
                result.First = First.Clone();
            }
            return result;
        }
        public bool Equals(RegexSetExpression rhs) {
            if (object.ReferenceEquals(rhs, this)) return true;
            if (object.ReferenceEquals(rhs, null)) return false;
            if(First!=null) {
                return First.Equals(rhs.First);
            }
            return rhs.First == null;
        }
        public override bool Equals(RegexExpression rhs) {
            return Equals(rhs as RegexSetExpression);
        }
        
        public override bool TryReduce(out RegexExpression reduced) {
            if(First==null || First is RegexSetNegate && First.NextElement==null) {
                reduced = null;
                return true;
            }
            var cur = First;
            var c = 0;
            while(cur!=null) {
                cur = cur.NextElement;
                ++c;
            }
            var rngs = new List<KeyValuePair<int, int>>(First.GetRanges());
            if(rngs.Count==1) {
                if(rngs[0].Key==rngs[0].Value) {
                    reduced = new RegexLiteralExpression(rngs[0].Key);
                    return true;
                }
            }
            if(c<=rngs.Count) {
                reduced = this;
                return false;
            }
            cur = null;
            var sx = new RegexSetExpression();
            for(var i = 0;i<rngs.Count;++i) {
                var rng = rngs[i];
                var r = new RegexSetRange();
                r.First = rng.Key;
                r.Last = rng.Value;
                if(cur==null) {
                    cur = r;
                    sx.First = r;
                } else {
                    cur.NextElement = r;
                    cur = r;
                }
            }
            reduced = sx;
            return true;
        }
    }
}
