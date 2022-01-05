using System.Collections.Generic;
using System.IO;

namespace RX {
#if RXLIB
    public
#endif
    class RegexOrExpression : RegexExpression {
        public override bool ShouldGroup => Expressions.Count>1 || (Expressions[0]!=null && Expressions[0].ShouldGroup);
        public List<RegexExpression> Expressions { get; } = new List<RegexExpression>();
        public RegexOrExpression(params RegexExpression[] exprs) {
            foreach (var expr in exprs) {
                var or = expr as RegexOrExpression;
                if (or != null) {
                    Expressions.AddRange(or.Expressions);
                } else
                    Expressions.Add(expr);
            }
        }
        public override void WriteTo(TextWriter writer) {
            if (Expressions.Count > 0) {
                var e = Expressions[0];
                if (e != null) {
                    e.WriteTo(writer);
                }
                for (var i = 1; i < Expressions.Count; ++i) {
                    writer.Write("|");
                    e = Expressions[i];
                    if(e!=null)
                        e.WriteTo(writer);
                }
            }
        }
        public override bool Equals(RegexExpression rhs) {
            return Equals(rhs as RegexOrExpression);
        }
         
        public bool Equals(RegexOrExpression rhs) {
            if (object.ReferenceEquals(this, rhs)) return true;
            if (object.ReferenceEquals(null, rhs)) return false;
            if (rhs.Expressions.Count != Expressions.Count) return false;
            for (var i = 0; i < Expressions.Count; ++i) {
                var l = Expressions[i];
                var r = rhs.Expressions[i];
                if (object.ReferenceEquals(l, r)) continue;
                if (object.ReferenceEquals(l, null)) return false;
                if (!l.Equals(r)) return false;
            }
            return true;
        }
        public override FA ToFA(int accept = 0) {
            var result = new FA();
            if(Expressions.Count==0) {
                result.AcceptSymbolId = accept;
                return result;
            } 
            if(Expressions.Count==1) {
                var ee = Expressions[0];
                if(ee!=null) {
                    return ee.ToFA(accept);
                }
            }
            var final = new FA(accept);
            for (var i = 0;i<Expressions.Count;++i) {
                var e = Expressions[i];
                if(e!=null) {
                    var fa = e.ToFA(accept);
                    result.AddEpsilon(fa);
                    var fas = fa.FirstAcceptingState;
                    fas.AddEpsilon(final);
                    fas.AcceptSymbolId = -1;
                } else {
                    result.AcceptSymbolId = accept;
                }
            }
            return result;
        }
        public override RegexExpression Clone() {
            var result = new RegexOrExpression();
            foreach(var exp in Expressions) {
                result.Expressions.Add(exp != null ? exp.Clone() : null);
            }
            return result;
        }
        private bool _AddReduced(RegexExpression e, ref bool hasnull) {
            if (e == null) return hasnull;
            var r = false;
            while (e!=null && e.TryReduce(out e)) r = true;
            if (e == null) return true;
            var o = e as RegexOrExpression;
            if (null != o) {
                for (var i = 0; i < o.Expressions.Count; ++i) {
                    var oe = o.Expressions[i];
                    if (oe != null) {
                        _AddReduced(oe, ref hasnull);
                    } else
                        hasnull = true;
                }
                return true;
            }
            Expressions.Add(e);
            return r;
        }
        public override bool TryReduce(out RegexExpression reduced) {
            var result = false;
            var or = new RegexOrExpression();
            var hasnull = false;
            for (var i = 0;i<Expressions.Count;++i) {
                var e = Expressions[i];
                if(e==null) {
                    if(hasnull) {
                        result = true;
                    }
                    hasnull = true;
                } else {
                    if(or._AddReduced(e, ref hasnull)) {
                        result = true;
                    }
                } 
            }
            if(!result) {
                reduced = this;
                return false;
            }
            switch (or.Expressions.Count) {
            case 0:
                reduced = null;
                return true;
            case 1:
                if (!hasnull) {
                    reduced = or.Expressions[0];
                    return true;
                }
                reduced = new RegexRepeatExpression(or.Expressions[0], 0, 1);
                while (reduced!=null && reduced.TryReduce(out reduced)) ;
                return true;
            default:
                RegexSetExpression s = null;
                IRegexSetElement c = null;
                for(var i = 0;i<or.Expressions.Count;++i) {
                    var e = or.Expressions[i];
                    var lit = e as RegexLiteralExpression;
                    var st = e as RegexSetExpression;
                    if(lit!=null) {
                        var r = new RegexSetRange();
                        r.First = r.Last = lit.Codepoint;
                        if (c == null) {
                            c = r;
                            if(s==null) {
                                s = new RegexSetExpression();
                            }
                            s.First = c;
                        } else {
                            c.NextElement = r;
                            c = r;
                        }
                        or.Expressions.RemoveAt(i);
                        --i;
                    } else if(st!=null) {
                        if(st.First is RegexSetNegate) {
                            foreach(var kvp in st.GetRanges()) {
                                var r = new RegexSetRange();
                                r.First = kvp.Key;
                                r.Last = kvp.Value;
                                if (c == null) {
                                    c = r;
                                    if (s == null) {
                                        s = new RegexSetExpression();
                                    }
                                    s.First = c;
                                } else {
                                    c.NextElement = r;
                                    c = r;
                                }
                            }
                        } else {
                            if (c == null) {
                                c = st.First.Clone();
                                if (s == null) {
                                    s = new RegexSetExpression();
                                }
                                s.First = c;
                            } else {
                                c.NextElement = st.First.Clone();
                                c = c.NextElement;
                            }
                        }
                        or.Expressions.RemoveAt(i);
                        --i;
                    } 
                }
                if(s!=null) {
                    RegexExpression se = s;
                    while (se!= null && se.TryReduce(out se)) ;
                    or.Expressions.Add(se);
                }
                if (hasnull) {
                    or.Expressions.Add(null);
                }
                reduced = or;
                return true;
            }
        }
    }
}
