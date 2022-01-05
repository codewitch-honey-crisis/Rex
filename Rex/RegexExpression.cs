using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RX {
#if RXLIB
	public
#endif
	abstract class RegexExpression {
		private class _EFATransition {
			public RegexExpression Expression;
			public _EFA To;
			public _EFATransition(RegexExpression expression = null, _EFA to = null) {
				Expression = expression;
				To = to;
            }
        }
		private sealed class _EFA {
			public bool IsAccepting;
			public int Accept;
			public List<_EFATransition> Transitions { get; } = new List<_EFATransition>();
			public IList<_EFA> FillClosure(IList<_EFA> result = null) {
				if (result == null) result = new List<_EFA>();
				if (result.Contains(this))
					return result;
				result.Add(this);
				foreach (var t in Transitions) {
					t.To.FillClosure(result);
				}

				return result;
			}
			public static IList<KeyValuePair<_EFA, int>> GetIncomingTransitionIndices(IEnumerable<_EFA> closure, _EFA efa, bool includeLoops = true) {
				var result = new List<KeyValuePair<_EFA, int>>();
				foreach (var cfa in closure) {
					var i = 0;
					foreach (var t in cfa.Transitions) {
						if (includeLoops || t.To != cfa) {
							if (t.To == efa) {
								var kvp = new KeyValuePair<_EFA, int>(cfa, i);
								if (!result.Contains(kvp)) {
									result.Add(kvp);
								}
							}
						}
						++i;
					}
				}
				return result;
			}
			public IDictionary<_EFA, RegexExpression> FillInputTransitionsGroupedByState(IDictionary<_EFA, RegexExpression> result = null) {
				if (result == null) {
					result = new Dictionary<_EFA, RegexExpression>();
				}
				for (var i = 0; i < Transitions.Count; ++i) {
					var t = Transitions[i];
					RegexExpression exp;
					if (!result.TryGetValue(t.To, out exp)) {
						var or = new RegexOrExpression(t.Expression);
						result.Add(t.To, or);
					} else {
						var or = exp as RegexOrExpression;
						var oor = t.Expression as RegexOrExpression;
						if(oor!=null) {
							or.Expressions.AddRange(oor.Expressions);
                        } else
							or.Expressions.Add(t.Expression);
					}
				}
				return result;
			}
		}
		public static RegexExpression FromFA(FA fa) {
			// Still somewhat untested
			var closure = fa.FillClosure();
			IList<_EFA> efas = new List<_EFA>(closure.Count + 1);
			var i = 0;
			while (i <= closure.Count) {
				efas.Add(null);
				++i;
			}
			i = 0;
			foreach (var cfa in closure) {
				efas[i] = new _EFA();
				++i;
			}
			var final = new _EFA();
			final.IsAccepting = true;
			final.Accept = 0;
			efas[i] = final;
			for (i = 0; i < closure.Count; ++i) {
				var e = efas[i];
				var c = closure[i];
				if (c.AcceptSymbolId!=-1) {
					e.Transitions.Add(new _EFATransition(null,final));
				}
				for(var j = 0;j<c.Transitions.Count;++j) {
					var ct = c.Transitions[j];
					if(ct.Min==-1 && ct.Max==-1) {
						e.Transitions.Add(new _EFATransition(null, efas[closure.IndexOf(ct.To)]));
                    }
                }
				var rngGrps = c.FillInputTransitionRangesGroupedByState();
				foreach (var rngGrp in rngGrps) {
					var tto = efas[closure.IndexOf(rngGrp.Key)];
					if (rngGrp.Value.Count==1) {
						var r = rngGrp.Value[0];
						if(r.Key==r.Value) {
							var lit = new RegexLiteralExpression(r.Key);
							e.Transitions.Add(new _EFATransition(lit, tto));
							continue;
						}
                    }
					var sexpr = new RegexSetExpression(rngGrp.Value);
					e.Transitions.Add(new _EFATransition(sexpr, tto));
				}
			}
			//DumpEfas(efas);
			for (var jj = 0; jj < 2; ++jj) {
				i = 0;
				var done = false;

				while (!done) {
					done = true;
					var innerDone = false;
					while (!innerDone) {
						innerDone = true;
						i = 0;
						foreach (var e in efas) {
							if (e.Transitions.Count == 1) {
								var its = _EFA.GetIncomingTransitionIndices(efas, e);
								if (its.Count == 1 && its[0].Key.Transitions.Count == 1) {
									// is a loop?
									if (e.Transitions[0].To == its[0].Key) {
										var rep = new RegexRepeatExpression();
										rep.Expression = e.Transitions[0].Expression;
										rep.MinOccurs = rep.MaxOccurs = 0;
										e.Transitions[0].Expression = rep;
									} else {
										var exp = its[0].Key.Transitions[0].Expression;
										var cat = exp as RegexConcatExpression;
										if (cat == null) {
											cat = new RegexConcatExpression();
											cat.Expressions.Add(exp);
											exp = cat;
											its[0].Key.Transitions[0].Expression = cat;
										}
										cat.Expressions.Add(e.Transitions[0].Expression);
										its[0].Key.Transitions[0] = new _EFATransition(exp, e.Transitions[0].To);

									}
									innerDone = false;
									efas = efas[0].FillClosure();
									break;
								} else {
									foreach (var it in its) {
										// is it a loop?
										if (efas.IndexOf(it.Key) >= efas.IndexOf(e)) {
											// yes
										} else {
											// no
											var t = it.Key.Transitions[it.Value];
											it.Key.Transitions[it.Value] = new _EFATransition(t.Expression, e.Transitions[0].To);

											var exp = t.Expression;
											var cat = exp as RegexConcatExpression;
											if (cat == null) {
												cat = new RegexConcatExpression();
												cat.Expressions.Add(exp);
												exp = cat;
												it.Key.Transitions[it.Value].Expression = exp;
											}
											cat.Expressions.Add(e.Transitions[0].Expression);
											innerDone = false;
											efas = efas[0].FillClosure();
											break;
										}
									}
								}
							}
							++i;
						}
						if (innerDone) {
							efas = efas[0].FillClosure();
						} else
							done = false;

						// combine the unions
						innerDone = false;
						while (!innerDone) {
							innerDone = true;
							foreach (var e in efas) {
								var rgs = e.FillInputTransitionsGroupedByState();
								if (rgs.Count != e.Transitions.Count) {
									e.Transitions.Clear();
									foreach (var rg in rgs) {
										e.Transitions.Add(new _EFATransition(rg.Value, rg.Key));
									}
									innerDone = false;
									efas = efas[0].FillClosure();
									break;
								}
							}
						}
						if (innerDone) {
							efas = efas[0].FillClosure();
						} else
							done = false;

						// remove the loops
						innerDone = false;
						while (!innerDone) {
							innerDone = true;
							foreach (var e in efas) {
								for (var ii = 0; ii < e.Transitions.Count; ++ii) {
									var t = e.Transitions[ii];
									if (t.To == e) {
										// this is a loop
										var rep = new RegexRepeatExpression();
										rep.Expression = t.Expression;
										rep.MinOccurs = rep.MaxOccurs = 0;
										// prepend it to all the other transitions 
										for (var iii = 0; iii < e.Transitions.Count; ++iii) {
											if (ii != iii) {
												var tt = e.Transitions[iii];
												if (tt.To != e) {
													var cat = tt.Expression as RegexConcatExpression;
													if (cat == null) {
														cat = new RegexConcatExpression();
														cat.Expressions.Add(rep);
														cat.Expressions.Add(tt.Expression);
														e.Transitions[iii].Expression = cat;
													} else {
														cat.Expressions.Insert(0, rep);
													}
												}
											}
										}
										e.Transitions.RemoveAt(ii);
										--ii;
										innerDone = false;
										efas = efas[0].FillClosure();
										break;
									}

								}
							}
						}
						if (innerDone) {
							efas = efas[0].FillClosure();
						} else
							done = false;
					}
				}
			}
			var res = efas[0];
			if(res.Transitions.Count>0) {
				return res.Transitions[0].Expression;
            }
			return null;
		}
		/*static void DumpEfas(IList<_EFA> efas) {
			var i = 0;
			foreach (var e in efas) {
				System.Console.WriteLine("{0}q{1}:", e.IsAccepting ? "*" : "", i);
				foreach (var t in e.Transitions) {
					System.Console.WriteLine("\t{0} -> q{1}", t.Expression, efas.IndexOf(t.To));
				}
				++i;
				System.Console.WriteLine();
			}
		}*/
		protected class LexContext {
			public int TabWidth = 4;
			public long Position = -1;
			public int Line = 1;
			public int Column = 0;
			public string FileOrUrl = null;
			public IEnumerator<char> Cursor;
			public int Codepoint = -2;
			public void EnsureStarted() {
				if (Codepoint == -2) {
					Advance();
				}
			}
			public void Expecting(params int[] expecting) {
				if (-2 == Codepoint)
					throw new RegexException("The cursor is before the beginning of the input", Position, Line, Column, FileOrUrl);
				switch (expecting.Length) {
				case 0:
					if (-1 == Codepoint)
						throw new RegexException("Unexpected end of input", Position, Line, Column, FileOrUrl);
					break;
				case 1:
					if (expecting[0] != Codepoint)
						throw new RegexException(expecting, Position, Line, Column, FileOrUrl);
					break;
				default:
					if (0 > System.Array.IndexOf(expecting, Codepoint))
						throw new RegexException(expecting, Position, Line, Column, FileOrUrl);
					break;
				}
			}
			public int Advance() {
				if (Codepoint == -1) return -1;
				char ch;
				if (Cursor.MoveNext()) {
					ch = Cursor.Current;
					if (char.IsHighSurrogate(ch)) {
						if (!Cursor.MoveNext()) {
							throw new System.IO.IOException("Unexpected end of stream while parsing Unicode surrogate");
						}
						Codepoint = char.ConvertToUtf32(ch, Cursor.Current);
						++Position;
						++Column;
						return Codepoint;
					} else {
						Codepoint = ch;
						++Position;
						switch (ch) {
						case '\t':
							Column = (((Column - 1) / TabWidth) + 1) * TabWidth + 1;
							break;
						case '\r':
							Column = 1;
							break;
						case '\n':
							Column = 1;
							++Line;
							break;
						default:
							++Column;
							break;

						}
						return Codepoint;
					}
				}
				Codepoint = -1;
				return -1;

			}
			public bool TrySkipWhiteSpace() {
				EnsureStarted();
				if (-1 == Codepoint || !char.IsWhiteSpace(char.ConvertFromUtf32(Codepoint), 0))
					return false;
				while (-1 != Advance() && char.IsWhiteSpace(char.ConvertFromUtf32(Codepoint), 0)) ;
				return true;
			}
		}

		public abstract bool ShouldGroup { get; }
		/// <summary>
		/// Escapes a single codepoint
		/// </summary>
		/// <param name="codepoint">The codepoint</param>
		/// <param name="builder">The optional <see cref="StringBuilder"/> to write to.</param>
		/// <returns>The escaped codepoint</returns>
		public static string EscapeCodepoint(int codepoint, StringBuilder builder = null) {
			if (null == builder)
				builder = new StringBuilder();
			switch (codepoint) {
			case '.':
			case '[':
			case ']':
			case '^':
			case '-':
			case '\\':
				builder.Append('\\');
				builder.Append(char.ConvertFromUtf32(codepoint));
				break;
			case '\t':
				builder.Append("\\t");
				break;
			case '\n':
				builder.Append("\\n");
				break;
			case '\r':
				builder.Append("\\r");
				break;
			case '\0':
				builder.Append("\\0");
				break;
			case '\f':
				builder.Append("\\f");
				break;
			case '\v':
				builder.Append("\\v");
				break;
			case '\b':
				builder.Append("\\b");
				break;
			default:
				var s = char.ConvertFromUtf32(codepoint);
				if (!char.IsLetterOrDigit(s, 0) && !char.IsSeparator(s, 0) && !char.IsPunctuation(s, 0) && !char.IsSymbol(s, 0)) {
					if (s.Length == 1) {
						builder.Append("\\u");
						builder.Append(unchecked((ushort)codepoint).ToString("x4"));
					} else {
						builder.Append("\\U");
						builder.Append(codepoint.ToString("x8"));
					}

				} else
					builder.Append(s);
				break;
			}
			return builder.ToString();
		}
		protected static void WriteEscapedCodepoint(int codepoint, TextWriter writer) {
			switch (codepoint) {
			case '.':
			case '*':
			case '+':
			case '?':
			case '{':
			case '}':
			case '(':
			case ')':
			case '[':
			case ']':
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
		public abstract RegexExpression Clone();
		public abstract void WriteTo(TextWriter writer);
		public abstract FA ToFA(int accept = 0);
		/// <summary>
		/// Returns the string representation of this expression
		/// </summary>
		/// <returns>A string that represents this expression</returns>
		public override string ToString() {
			var result = new StringWriter();
			WriteTo(result);
			return result.ToString();
		}
		static RegexExpression _ParseModifier(RegexExpression expr, LexContext lc) {
			RegexRepeatExpression rep = new RegexRepeatExpression();
			rep.Expression = expr;
			switch (lc.Codepoint) {
			case '*':
				rep.MinOccurs = 0;
				rep.MaxOccurs = 0;
				expr = rep;
				lc.Advance();
				break;
			case '+':
				rep.MinOccurs = 1;
				rep.MaxOccurs = 0;
				expr = rep;
				lc.Advance();
				break;
			case '?':
				rep.MinOccurs = 0;
				rep.MaxOccurs = 1;
				expr = rep;
				lc.Advance();
				break;
			case '{':
				lc.Advance();
				lc.TrySkipWhiteSpace();
				lc.Expecting('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ',', '}');
				var min = 0;
				var max = 0;
				if (',' != lc.Codepoint && '}' != lc.Codepoint) {
					min = 0;
					while (lc.Codepoint >= '0' && lc.Codepoint <= '9') {
						min *= 10;
						min += lc.Codepoint - '0';
						lc.Advance();
					}
					lc.TrySkipWhiteSpace();
				}
				if (',' == lc.Codepoint) {
					lc.Advance();
					lc.TrySkipWhiteSpace();
					lc.Expecting('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '}');
					if ('}' != lc.Codepoint) {
						max = 0;
						while (lc.Codepoint >= '0' && lc.Codepoint <= '9') {
							max *= 10;
							max += lc.Codepoint - '0';
							lc.Advance();
						}

						lc.TrySkipWhiteSpace();
					}
				} else { max = min; }
				lc.Expecting('}');
				lc.Advance();
				rep.MinOccurs = min;
				rep.MaxOccurs = max;
				expr = rep;
				break;
			}
			if (lc.Codepoint == '?') {
				lc.Advance();
				rep.IsLazy = true;
			}
			return expr;
		}
		static IRegexSetElement _ToSetElements(IEnumerable<int> ranges) {
			IRegexSetElement result = null;
			IRegexSetElement current = null;
			var rcur = ranges.GetEnumerator();
			while (rcur.MoveNext()) {
				var first = rcur.Current;
				if (!rcur.MoveNext()) break;
				var last = rcur.Current;
				var rng = new RegexSetRange();
				rng.First = first;
				rng.Last = last;
				if (result == null) {
					result = rng;
					current = rng;
				} else {
					current.NextElement = rng;
					current = rng;
				}
			}
			return result;
		}
		/*static RegexSetExpression _ClassToSetExpression(int[] @class, bool negate = false) {
			var result = new RegexSetExpression();
			var elems = _ToSetElements(@class);
			if (negate) {
				var not = new RegexSetNegate();
				not.Next = elems;
				elems = not;
			}
			result.First = elems;
			return result;
		}*/
		static RegexSetExpression _ClassToSetExpression(string @class, bool negate = false) {
			var result = new RegexSetExpression();
			IRegexSetElement elems;
			var cls = new RegexSetCharacterClass();
			cls.Class = @class;
			elems = cls;
			if (negate) {
				var not = new RegexSetNegate();
				not.Next = elems;
				elems = not;
			}
			result.First = elems;
			return result;
		}
		static byte _FromHexChar(char hex) {
			if (':' > hex && '/' < hex)
				return (byte)(hex - '0');
			if ('G' > hex && '@' < hex)
				return (byte)(hex - '7'); // 'A'-10
			if ('g' > hex && '`' < hex)
				return (byte)(hex - 'W'); // 'a'-10
			throw new System.ArgumentException("The value was not hex.", "hex");
		}
		static bool _IsHexChar(int hex) {
			if (':' > hex && '/' < hex)
				return true;
			if ('G' > hex && '@' < hex)
				return true;
			if ('g' > hex && '`' < hex)
				return true;
			return false;
		}
		static int _ParseEscapePart(LexContext pc) {
			if (-1 == pc.Codepoint) return -1;
			switch (pc.Codepoint) {
			case 'f':
				pc.Advance();
				return '\f';
			case 'v':
				pc.Advance();
				return '\v';
			case 't':
				pc.Advance();
				return '\t';
			case 'n':
				pc.Advance();
				return '\n';
			case 'r':
				pc.Advance();
				return '\r';
			case 'x':
				if (-1 == pc.Advance() || !_IsHexChar(pc.Codepoint))
					return 'x';
				byte b = _FromHexChar((char)pc.Codepoint);
				if (-1 == pc.Advance() || !_IsHexChar(pc.Codepoint))
					return unchecked((char)b);
				b <<= 4;
				b |= _FromHexChar((char)pc.Codepoint);
				if (-1 == pc.Advance() || !_IsHexChar(pc.Codepoint))
					return unchecked((char)b);
				b <<= 4;
				b |= _FromHexChar((char)pc.Codepoint);
				if (-1 == pc.Advance() || !_IsHexChar(pc.Codepoint))
					return unchecked((char)b);
				b <<= 4;
				b |= _FromHexChar((char)pc.Codepoint);
				return b;
			case 'u':
				if (-1 == pc.Advance())
					return 'u';
				ushort u = _FromHexChar((char)pc.Codepoint);
				u <<= 4;
				if (-1 == pc.Advance())
					return unchecked((char)u);
				u |= _FromHexChar((char)pc.Codepoint);
				u <<= 4;
				if (-1 == pc.Advance())
					return unchecked((char)u);
				u |= _FromHexChar((char)pc.Codepoint);
				u <<= 4;
				if (-1 == pc.Advance())
					return unchecked((char)u);
				u |= _FromHexChar((char)pc.Codepoint);
				return u;
			default:
				int i = pc.Codepoint;
				pc.Advance();
				return i;
			}
		}
		static int _ParseRangeEscapePart(LexContext pc) {
			if (-1 == pc.Codepoint)
				return -1;
			switch (pc.Codepoint) {
			case '0':
				pc.Advance();
				return '\0';
			case 'f':
				pc.Advance();
				return '\f';
			case 'v':
				pc.Advance();
				return '\v';
			case 't':
				pc.Advance();
				return '\t';
			case 'n':
				pc.Advance();
				return '\n';
			case 'r':
				pc.Advance();
				return '\r';
			case 'x':
				if (-1 == pc.Advance() || !_IsHexChar(pc.Codepoint))
					return 'x';
				byte b = _FromHexChar((char)pc.Codepoint);
				if (-1 == pc.Advance() || !_IsHexChar(pc.Codepoint))
					return b;
				b <<= 4;
				b |= _FromHexChar((char)pc.Codepoint);
				if (-1 == pc.Advance() || !_IsHexChar(pc.Codepoint))
					return b;
				b <<= 4;
				b |= _FromHexChar((char)pc.Codepoint);
				if (-1 == pc.Advance() || !_IsHexChar(pc.Codepoint))
					return b;
				b <<= 4;
				b |= _FromHexChar((char)pc.Codepoint);
				return b;
			case 'u':
				if (-1 == pc.Advance())
					return 'u';
				ushort u = _FromHexChar((char)pc.Codepoint);
				u <<= 4;
				if (-1 == pc.Advance())
					return u;
				u |= _FromHexChar((char)pc.Codepoint);
				u <<= 4;
				if (-1 == pc.Advance())
					return u;
				u |= _FromHexChar((char)pc.Codepoint);
				u <<= 4;
				if (-1 == pc.Advance())
					return u;
				u |= _FromHexChar((char)pc.Codepoint);
				return u;
			default:
				int i = pc.Codepoint;
				pc.Advance();
				return i;
			}
		}
		static int _ParseRangePart(LexContext lc) {
			lc.Expecting();
			if (lc.Codepoint == '\\') {
				lc.Advance();
				lc.Expecting();
				return _ParseRangeEscapePart(lc);
			}
			var i = lc.Codepoint;
			lc.Advance();
			return i;
		}
		static RegexExpression _ParseSet(LexContext lc) {
			var result = new RegexSetExpression();
			IRegexSetElement current = null;
			lc.Expecting('[');
			lc.Advance();
			lc.Expecting();
			if (lc.Codepoint == '^') {
				current = new RegexSetNegate();
				result.First = current;
				lc.Advance();
				lc.Expecting();
			}
			bool first = true;
			while (lc.Codepoint > -1 && lc.Codepoint != ']') {
				if (lc.Codepoint == '[') {
					lc.Advance();
					lc.Expecting(':');
					lc.Advance();
					var scls = new RegexSetCharacterClass();
					scls.Class = "";
					while (-1 < lc.Codepoint && lc.Codepoint != ':') {
						scls.Class += char.ConvertFromUtf32(lc.Codepoint);
					}
					lc.Expecting(':');
					lc.Advance();
					lc.Expecting(']');
					lc.Advance();
					if (current == null) {
						current = scls;
						result.First = current;
					} else {
						current.NextElement = scls;
						current = current.NextElement;
					}
					continue;
				} else if (!first && lc.Codepoint == '&') {
					lc.Advance();
					if (lc.Codepoint == '&') {
						lc.Advance();
					} else {
						var rr = new RegexSetRange();
						if (lc.Codepoint == '-') {
							lc.Advance();
							if (lc.Codepoint == -1 || lc.Codepoint == ']') {
								rr.First = rr.Last = '&';
								if (current == null) {
									current = rr;
									result.First = current;
								} else {
									current.NextElement = rr;
									current = current.NextElement;
								}
								rr = new RegexSetRange();
								rr.First = '-';
								rr.Last = '-';
								current.NextElement = rr;
								current = current.NextElement;
							} else {
								rr.First = '&';
								rr.Last = _ParseRangeEscapePart(lc);
								if (current == null) {
									current = rr;
									result.First = current;
								} else {
									current.NextElement = rr;
									current = current.NextElement;
								}
							}
						} else {
							rr.First = rr.Last = '&';
							if (current == null) {
								current = rr;
								result.First = current;
							} else {
								current.NextElement = rr;
								current = current.NextElement;
							}
						}
						continue;
					}
					if (lc.Codepoint == '[') {
						var si = new RegexSetIntersect();
						var se = Parse(lc);
						si.SetExpression = (RegexSetExpression)se;
						if (current == null) {
							current = si;
							result.First = current;
						} else {
							current.NextElement = si;
							current = current.NextElement;
						}
						continue;
					} else {
						continue;
					}
				} else {
					var rf = _ParseRangePart(lc);
					var rr = new RegexSetRange();
					if (lc.Codepoint == '-') {
						lc.Advance();
						if (lc.Codepoint == -1 || lc.Codepoint == ']') {
							rr.First = rr.Last = rf;
							if (current == null) {
								current = rr;
								result.First = current;
							} else {
								current.NextElement = rr;
								current = current.NextElement;
							}
							rr = new RegexSetRange();
							rr.First = '-';
							rr.Last = '-';
							current.NextElement = rr;
							current = current.NextElement;
						} else {
							rr.First = rf;
							rr.Last = _ParseRangePart(lc);
							if (current == null) {
								current = rr;
								result.First = current;
							} else {
								current.NextElement = rr;
								current = current.NextElement;
							}
						}
					} else {
						rr.First = rr.Last = rf;
						if (current == null) {
							current = rr;
							result.First = current;
						} else {
							current.NextElement = rr;
							current = current.NextElement;
						}
					}
				}
				first = false;
			}
			lc.Advance();
			return result;
		}
		protected static RegexExpression Parse(LexContext lc) {
			RegexExpression result = null;
			RegexExpression next = null;
			RegexSetExpression st;
			RegexSetNegate not;
			int ich;
			IRegexSetElement elems = null;
			while (true) {
				switch (lc.Codepoint) {
				case -1:
					return result;
				case '.':
					var dot = new RegexSetExpression();
					var rng = new RegexSetRange();
					rng.First = 0;
					rng.Last = 0x10FFFF;
					dot.First = rng;
					if (null == result)
						result = dot;
					else {
						var cat = new RegexConcatExpression();
						cat.Expressions.Add(result);
						cat.Expressions.Add(dot);
						result = cat;
					}
					lc.Advance();
					result = _ParseModifier(result, lc);
					break;
				case '\\':

					lc.Advance();
					lc.Expecting();
					var isNot = false;
					switch (lc.Codepoint) {
					case 'P':
						isNot = true;
						goto case 'p';
					case 'p':
						lc.Advance();
						lc.Expecting('{');
						var uc = new StringBuilder();
						int uli = lc.Line;
						int uco = lc.Column;
						long upo = lc.Position;
						while (-1 != lc.Advance() && '}' != lc.Codepoint)
							uc.Append(char.ConvertFromUtf32(lc.Codepoint));
						lc.Expecting('}');
						lc.Advance();
						int uci = 0;
						switch (uc.ToString()) {
						case "Pe":
							uci = 21;
							break;
						case "Pc":
							uci = 18;
							break;
						case "Cc":
							uci = 14;
							break;
						case "Sc":
							uci = 26;
							break;
						case "Pd":
							uci = 19;
							break;
						case "Nd":
							uci = 8;
							break;
						case "Me":
							uci = 7;
							break;
						case "Pf":
							uci = 23;
							break;
						case "Cf":
							uci = 15;
							break;
						case "Pi":
							uci = 22;
							break;
						case "Nl":
							uci = 9;
							break;
						case "Zl":
							uci = 12;
							break;
						case "Ll":
							uci = 1;
							break;
						case "Sm":
							uci = 25;
							break;
						case "Lm":
							uci = 3;
							break;
						case "Sk":
							uci = 27;
							break;
						case "Mn":
							uci = 5;
							break;
						case "Ps":
							uci = 20;
							break;
						case "Lo":
							uci = 4;
							break;
						case "Cn":
							uci = 29;
							break;
						case "No":
							uci = 10;
							break;
						case "Po":
							uci = 24;
							break;
						case "So":
							uci = 28;
							break;
						case "Zp":
							uci = 13;
							break;
						case "Co":
							uci = 17;
							break;
						case "Zs":
							uci = 11;
							break;
						case "Mc":
							uci = 6;
							break;
						case "Cs":
							uci = 16;
							break;
						case "Lt":
							uci = 2;
							break;
						case "Lu":
							uci = 0;
							break;
						}
						elems = _ToSetElements(RegexCharacterClasses.UnicodeCategories[uci]);
						if (isNot) {
							not = new RegexSetNegate();
							not.Next = elems;
							elems = not;
						}
						st = new RegexSetExpression();
						st.First = elems;
						next = st;
						break;
					case 'd':
						next = _ClassToSetExpression("digit");
						lc.Advance();
						break;
					case 'D':
						next = _ClassToSetExpression("digit", true);
						lc.Advance();
						break;
					case 's':
						next = _ClassToSetExpression("space");
						lc.Advance();
						break;
					case 'S':
						next = _ClassToSetExpression("space", true);
						lc.Advance();
						break;
					case 'w':
						next = _ClassToSetExpression("word");
						lc.Advance();
						break;
					case 'W':
						next = _ClassToSetExpression("word", true);
						lc.Advance();
						break;
					default:
						if (-1 != (ich = _ParseEscapePart(lc))) {
							var lt = new RegexLiteralExpression();
							lt.Codepoint = ich;
							next = lt;

						} else {
							lc.Expecting(); // throw an error
							return null; // doesn't execute
						}
						break;
					}
					next = _ParseModifier(next, lc);
					if (null != result) {
						var cat = result as RegexConcatExpression;
						if (cat == null) {
							cat = new RegexConcatExpression();
							cat.Expressions.Add(result);
							cat.Expressions.Add(next);
							result = cat;
						} else {
							cat.Expressions.Add(next);
						}
					} else
						result = next;
					break;
				case ')':
					return result;
				case '(':
					lc.Advance();
					lc.Expecting();
					var grp = new RegexGroupExpression();
					grp.Group = "";
					if (lc.Codepoint == '?') {
						lc.Advance();
						lc.Expecting('<', ':');
						if (lc.Codepoint == '<') {
							lc.Advance();
							lc.Expecting();
							while ('>' != lc.Codepoint) {
								grp.Group += char.ConvertFromUtf32(lc.Codepoint);
								lc.Advance();
							}
							lc.Expecting('>');
							lc.Advance();
						} else {
							grp.Group = null;
							lc.Advance();
						}
						lc.Expecting();
					}
					grp.Expression = Parse(lc);
					next = grp;
					lc.Expecting(')');
					lc.Advance();
					next = _ParseModifier(next, lc);
					if (null == result)
						result = next;
					else {
						if (result is RegexConcatExpression) {
							((RegexConcatExpression)result).Expressions.Add(next);
						} else {
							var cat = new RegexConcatExpression();
							cat.Expressions.Add(result);
							cat.Expressions.Add(next);
							result = cat;
						}
					}
					break;
				case '|':
					if (-1 != lc.Advance()) {
						next = Parse(lc);
						if (null != result) {
							if (result is RegexOrExpression) {
								((RegexOrExpression)result).Expressions.Add(next);
							} else {
								var or = new RegexOrExpression();
								or.Expressions.Add(result);
								or.Expressions.Add(next);
								result = or;
							}
						} else
							result = next;
					} else {
						if (null != result) {
							if (result is RegexOrExpression) {
								((RegexOrExpression)result).Expressions.Add(null);
							} else {
								var or = new RegexOrExpression();
								or.Expressions.Add(result);
								or.Expressions.Add(null);
								result = or;
							}
						} else {
							result = next;
						}
					}
					break;
				case '[':
					var seti = _ParseSet(lc);
					next = seti;
					next = _ParseModifier(next, lc);

					if (null == result)
						result = next;
					else {
						if (result is RegexConcatExpression) {
							((RegexConcatExpression)result).Expressions.Add(next);
						} else {
							var cat = new RegexConcatExpression();
							cat.Expressions.Add(result);
							cat.Expressions.Add(next);
							result = cat;
						}
					}
					break;
				default:
					ich = lc.Codepoint;
					var lit = new RegexLiteralExpression();
					lit.Codepoint = ich;
					next = lit;
					lc.Advance();
					next = _ParseModifier(next, lc);
					if (null == result)
						result = next;
					else {
						if (result is RegexConcatExpression) {
							((RegexConcatExpression)result).Expressions.Add(next);
						} else {
							var cat = new RegexConcatExpression();
							cat.Expressions.Add(result);
							cat.Expressions.Add(next);
							result = cat;
						}
					}
					break;
				}
			}
		}
		public abstract bool Equals(RegexExpression rhs);
		public abstract bool TryReduce(out RegexExpression reduced);
		public RegexExpression Reduce() {
			RegexExpression result = this;
			while (result.TryReduce(out result)) ;
			return result;
        }
		public static RegexExpression Parse(IEnumerable<char> text, long position = -1) {
			LexContext lc = new LexContext();
			lc.Cursor = text.GetEnumerator();
			lc.Position = position;
			lc.EnsureStarted();
			return Parse(lc);
		}
	}
}

