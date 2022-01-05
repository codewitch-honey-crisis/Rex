using System.Collections.Generic;
using System.IO;

namespace RX {
#if RXLIB
    public
#endif
    class RegexConcatExpression : RegexExpression {
        public override bool ShouldGroup => Expressions.Count > 1 || (Expressions[0] != null && Expressions[0].ShouldGroup);
        public List<RegexExpression> Expressions { get; } = new List<RegexExpression>();
        public RegexConcatExpression(params RegexExpression[] exprs) {
            foreach(var expr in exprs) {
                var cat = expr as RegexConcatExpression;
                if (cat != null) {
                    Expressions.AddRange(cat.Expressions);
                } else
                    Expressions.Add(expr);
            }
        }
        public override void WriteTo(TextWriter writer) {
            for(var i = 0;i<Expressions.Count;++i) {
                var e = Expressions[i];
                var paren = e is RegexOrExpression && e.ShouldGroup;
                if (e != null) {
                    if(paren) {
                        writer.Write("(?:");
                        e.WriteTo(writer);
                        writer.Write(")");
                    } else 
                        e.WriteTo(writer);
                }
            }
        }
        public override bool Equals(RegexExpression rhs) {
            return Equals(rhs as RegexConcatExpression);
        }
        public bool Equals(RegexConcatExpression rhs) {
            if (object.ReferenceEquals(this, rhs)) return true;
            if (object.ReferenceEquals(null, rhs)) return false;
            if (rhs.Expressions.Count != Expressions.Count) return false;
            for(var i = 0;i<Expressions.Count;++i) {
                var l = Expressions[i];
                var r = rhs.Expressions[i];
                if (object.ReferenceEquals(l, r)) continue;
                if (object.ReferenceEquals(l, null)) return false;
                if (!l.Equals(r)) return false;
            }
            return true;
        }
        public override RegexExpression Clone() {
            var result = new RegexConcatExpression();
            foreach (var exp in Expressions) {
                result.Expressions.Add(exp != null ? exp.Clone() : null);
            }
            return result;
        }
        public override FA ToFA(int accept = 0) {
            if(Expressions.Count==0) {
                var fa = new FA();
                fa.AcceptSymbolId = accept;
                return fa;
            }
            var e = Expressions[Expressions.Count - 1];
            var current = (null != e) ? e.ToFA(accept) : new FA(accept);
            for(var i = Expressions.Count-2;i>=0;--i) {
                e = Expressions[i];
                if (e==null) {
                    continue;
                }
                var fa = e.ToFA(accept);
                if(fa.Transitions.Count==1 && fa.Transitions[0].To.Transitions.Count==0) {
                    var ft = fa.Transitions[0];
                    var ffa = new FA();
                    ffa.AddTransition(ft.Min, ft.Max, current);
                    current = ffa;
                } else {
                    var fas = fa.FirstAcceptingState;
                    fas.AddEpsilon(current);
                    fas.AcceptSymbolId = -1;
                    current = fa;
                }
            }
            return current;
        }
        private bool _AddReduced(RegexExpression e) {
            if (e == null) return true;
            var r = false;
            while (e!=null && e.TryReduce(out e)) r = true;
            if (e == null) return true;
            var c = e as RegexConcatExpression;
            if(null!=c) {
                for(var i = 0;i<c.Expressions.Count;++i) {
                    var ce = c.Expressions[i];
                    if(ce!=null) {
                        _AddReduced(ce);
                    }
                }
                return true;
            }
            Expressions.Add(e);
            return r;
        }
        public override bool TryReduce(out RegexExpression reduced) {
            var result = false;
            var cat = new RegexConcatExpression();
            for(var i = 0;i<Expressions.Count;++i) {
                var e = Expressions[i];
                if(e==null) {
                    result = true;
                    continue;
                }
                if(cat._AddReduced(e)) {
                    result = true;
                }
            }
            switch(cat.Expressions.Count) {
            case 0:
                reduced = null;
                return true;
            case 1:
                reduced = cat.Expressions[0].Reduce();
                return true;
            default:
                for(var i = 1;i<cat.Expressions.Count;++i) {
                    var e = cat.Expressions[i].Reduce();
                    var rep = e as RegexRepeatExpression;
                    if (rep != null) {
                        var ee = rep.Expression;
                        var cc = ee as RegexConcatExpression;
                        if (cc != null) {
                            var k = 0;
                            for (var j = i - cc.Expressions.Count; j < i; ++j) {
                                if (!cc.Expressions[k].Equals(cat.Expressions[j])) {
                                    reduced = result ? cat : this;
                                    return result;
                                }
                                ++k;
                            }
                            cat.Expressions[i] = new RegexRepeatExpression(cc, rep.MinOccurs + 1, rep.MaxOccurs > 0 ? rep.MaxOccurs + 1 : 0).Reduce();
                            cat.Expressions.RemoveRange(i - cc.Expressions.Count, cc.Expressions.Count);
                            result = true;
                        } else {
                            if (cat.Expressions[i - 1].Equals(ee)) {
                                cat.Expressions[i] = new RegexRepeatExpression(ee, rep.MinOccurs + 1, rep.MaxOccurs > 0 ? rep.MaxOccurs + 1 : 0).Reduce();
                                cat.Expressions.RemoveAt(i - 1);
                                result = true;
                            }
                        }
                    }
                }
                reduced = result?cat:this;
                return result;
            }
        }
    }
}
