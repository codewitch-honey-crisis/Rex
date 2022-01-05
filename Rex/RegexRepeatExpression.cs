using System.IO;

namespace RX {
#if RXLIB
    public
#endif
    class RegexRepeatExpression : RegexExpression {
        public override bool ShouldGroup => Expression!=null&&(MinOccurs!=1||MaxOccurs!=1||Expression.ShouldGroup);
        public RegexExpression Expression { get; set; } = null;
        public bool IsLazy { get; set; } = false;
        public int MinOccurs { get; set; } = 0;
        public int MaxOccurs { get; set; } = 0;
		public RegexRepeatExpression() {

        }
		public RegexRepeatExpression(RegexExpression expr, int minOccurs = 0, int maxOccurs = 0) {
			// TODO: Reduce for when expr is another repeat expression
			Expression = expr;
			MinOccurs = minOccurs;
			MaxOccurs = maxOccurs;
        }
        public override void WriteTo(TextWriter writer) {
            if (Expression == null) return;
            if (Expression.ShouldGroup && (MinOccurs!=1||MaxOccurs!=1)) {
                writer.Write("(?:");
                Expression.WriteTo(writer);
                writer.Write(")");
            } else {
                Expression.WriteTo(writer);
            }
            if (0 < MinOccurs && MinOccurs == MaxOccurs) {
                writer.Write("{");
                writer.Write(MinOccurs);
                writer.Write("}");
            } else {
                switch (MinOccurs) {
                case 0:
                    switch (MaxOccurs) {
                    case 0:
                        writer.Write("*");
                        break;
                    case 1:
                        writer.Write("?");
                        break;
                    default:
                        writer.Write("{0, ");
                        writer.Write(MaxOccurs);
                        writer.Write("}");
                        break;
                    }
                    break;
                case 1:
                    switch (MaxOccurs) {
                    case 0:
                        writer.Write("+");
                        break;
                    case 1:
                        break;
                    default:
                        writer.Write("{1, ");
                        writer.Write(MaxOccurs);
                        writer.Write("}");
                        break;
                    }
                    break;
                default: {
                    int min, max;
                    if (MinOccurs <= MaxOccurs) {
                        min = MinOccurs;
                        max = MaxOccurs;
                    } else {
                        min = MaxOccurs;
                        max = MinOccurs;
                    }
                    writer.Write("{");
                    writer.Write(min);
                    writer.Write(", ");
                    writer.Write(max);
                    writer.Write("}");
                }
                break;
                }
            }
            if(IsLazy && (MinOccurs!=1||MaxOccurs!=1 && (MinOccurs!=MaxOccurs || MaxOccurs==0))) {
                writer.Write("?");
            }
        }
		public bool Equals(RegexRepeatExpression rhs) {
			if (object.ReferenceEquals(this, rhs)) return true;
			if (object.ReferenceEquals(rhs, null)) return false;
			if(MinOccurs == rhs.MinOccurs && MaxOccurs == rhs.MaxOccurs && IsLazy == rhs.IsLazy) {
				if (object.ReferenceEquals(Expression, rhs.Expression)) return true;
				if (object.ReferenceEquals(Expression, null)) return false;
				return Expression.Equals(rhs.Expression);
            }
			return false;
        }
		public override bool Equals(RegexExpression rhs) {
			return Equals(rhs as RegexRepeatExpression);
        }
        public override FA ToFA(int accept = 0) {
			// TODO: Figure out how to do lazy matching
			if (IsLazy) throw new System.NotImplementedException("Lazy matching is not yet implemented");
			var minOccurs = MinOccurs;
			var maxOccurs = MaxOccurs;
			if(minOccurs!=0 && maxOccurs!=0 && minOccurs>maxOccurs) {
				maxOccurs = MinOccurs;
				minOccurs = MaxOccurs;
            }
			FA result,facc,expr;
			RegexConcatExpression cat;
			RegexRepeatExpression rep;
			RegexRepeatExpression opt;
			if (Expression==null) {
				result = new FA(accept);
				return result;
            }
			
			switch (minOccurs) {
			case 0:
				switch (maxOccurs) {
				case 0:
					result = new FA(accept);
					expr = Expression.ToFA(accept);
					facc = expr.FirstAcceptingState;
					facc.AddEpsilon(result);
					result.AddEpsilon(expr);
					facc.AcceptSymbolId = -1;
					return result;
				case 1:
					result = Expression.ToFA(accept);
					facc = result.FirstAcceptingState;
					if(facc!=result)
						result.AddEpsilon(facc);
					return result;
				default:
					cat = new RegexConcatExpression();
					opt = new RegexRepeatExpression();
					opt.MinOccurs = 0;
					opt.MaxOccurs = 1;
					opt.Expression = Expression;
					for(var i = 0;i<maxOccurs;++i) {
						cat.Expressions.Add(opt);
                    }
					result = cat.ToFA(accept);
					return result;
				}
			case 1:
				
				switch (maxOccurs) {
				case 0:
					cat = new RegexConcatExpression();
					cat.Expressions.Add(Expression);
					rep = new RegexRepeatExpression();
					rep.MinOccurs = rep.MaxOccurs = 0;
					rep.Expression = Expression;
					cat.Expressions.Add(rep);
					return cat.ToFA(accept);
				case 1:
					return Expression.ToFA(accept);
				default:
					cat = new RegexConcatExpression();
					cat.Expressions.Add(Expression);
					rep = new RegexRepeatExpression();
					rep.Expression = Expression;
					rep.MinOccurs = 0;
					rep.MaxOccurs = maxOccurs-1;
					cat.Expressions.Add(rep);
					return cat.ToFA(accept);
				}
			default:
				switch (maxOccurs) {
				case 0:
					cat = new RegexConcatExpression();
					rep = new RegexRepeatExpression();
					rep.Expression = Expression;
					rep.MinOccurs = minOccurs;
					rep.MaxOccurs = minOccurs;
					cat.Expressions.Add(rep);
					rep = new RegexRepeatExpression();
					rep.MinOccurs = rep.MaxOccurs = 0;
					rep.Expression = Expression;
					cat.Expressions.Add(rep);
					return cat.ToFA(accept);
				case 1:
					// should never get here
					throw new System.NotImplementedException("Shouldn't get here");
				default:
					cat = new RegexConcatExpression();
					if (minOccurs == maxOccurs) {
						for(var i = 0;i<minOccurs;++i) {
							cat.Expressions.Add(Expression);
                        }
						return cat.ToFA(accept);
					}
					rep = new RegexRepeatExpression();
					rep.Expression = Expression;
					rep.MinOccurs = rep.MaxOccurs = minOccurs;
					cat.Expressions.Add(rep);
					opt = new RegexRepeatExpression();
					opt.MinOccurs = 0;
					opt.MaxOccurs = 1;
					opt.Expression = Expression;
					rep = new RegexRepeatExpression();
					rep.Expression = opt;
					rep.MinOccurs = rep.MaxOccurs = maxOccurs - minOccurs;
					cat.Expressions.Add(rep);
					return cat.ToFA(accept);
				}
			}
			// should never get here
			throw new System.NotImplementedException("Shouldn't get here");
		}
        public override bool TryReduce(out RegexExpression reduced) {
            if(Expression==null) {
				reduced = null;
				return true;
            }
			var e = Expression;
			var r = false;
			while (e != null && e.TryReduce(out e)) r = true;
			if(e==null) {
				reduced = null;
				return true;
            }
			if (MinOccurs == 1 && MaxOccurs == 1) {
				reduced = e;
				return true;
			}
			var re = e as RegexRepeatExpression;
			if(!IsLazy && re!=null && !re.IsLazy) {
				if (re.MinOccurs == 0 && (re.MaxOccurs == 1 || re.MaxOccurs == 0)) {
					if (MinOccurs == 1 && MaxOccurs == 0) {
						e = re.Expression;
						MinOccurs = 0;
						r = true;
					}
				} else if (re.MinOccurs == 1 && re.MaxOccurs == 0) {
					if (MinOccurs == 0 && (MaxOccurs == 1 || MaxOccurs == 0)) {
						e = re.Expression;
						r = true;
					}
				}
            }
			if (!r) {
				reduced = this;
				return false;
			}
			var rep = new RegexRepeatExpression(e, MinOccurs, MaxOccurs);
			rep.IsLazy = IsLazy;
			reduced = rep;
			return true;
        }
        public override RegexExpression Clone() {
			var result = new RegexRepeatExpression();
			result.MinOccurs = MinOccurs;
			result.MaxOccurs = MaxOccurs;
			result.IsLazy = IsLazy;
			if(Expression!=null) {
				result.Expression = Expression.Clone();
            }
			return result;
        }
    }
}
