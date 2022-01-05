using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace RX {
    partial class FA {
		/// <summary>
		/// Represents optional rendering parameters for a dot graph.
		/// </summary>
		public sealed class DotGraphOptions {
			/// <summary>
			/// The resolution, in dots-per-inch to render at
			/// </summary>
			public int Dpi { get; set; } = 300;
			/// <summary>
			/// The prefix used for state labels
			/// </summary>
			public string StatePrefix { get; set; } = "q";

			public bool HideAcceptSymbolIds { get; set; } = false;

		}
		static string _EscapeLabel(string label) {
			if (string.IsNullOrEmpty(label)) return label;

			string result = label.Replace("\\", @"\\");
			result = result.Replace("\"", "\\\"");
			result = result.Replace("\n", "\\n");
			result = result.Replace("\r", "\\r");
			result = result.Replace("\0", "\\0");
			result = result.Replace("\v", "\\v");
			result = result.Replace("\t", "\\t");
			result = result.Replace("\f", "\\f");
			return result;
		}
		public void WriteDotTo(TextWriter writer, DotGraphOptions options = null) {
			_WriteDotTo(FillClosure(), writer, options);
		}
		/// <summary>
		/// Renders Graphviz output for this machine to the specified file
		/// </summary>
		/// <param name="filename">The output filename. The format to render is indicated by the file extension.</param>
		/// <param name="options">A <see cref="DotGraphOptions"/> instance with any options, or null to use the defaults</param>
		public void RenderToFile(string filename, DotGraphOptions options = null) {
			if (null == options)
				options = new DotGraphOptions();
			string args = "-T";
			string ext = Path.GetExtension(filename);
			if (0 == string.Compare(".png", ext, StringComparison.InvariantCultureIgnoreCase))
				args += "png";
			else if (0 == string.Compare(".jpg", ext, StringComparison.InvariantCultureIgnoreCase))
				args += "jpg";
			else if (0 == string.Compare(".bmp", ext, StringComparison.InvariantCultureIgnoreCase))
				args += "bmp";
			else if (0 == string.Compare(".svg", ext, StringComparison.InvariantCultureIgnoreCase))
				args += "svg";
			if (0 < options.Dpi)
				args += " -Gdpi=" + options.Dpi.ToString();

			args += " -o\"" + filename + "\"";

			var psi = new ProcessStartInfo("dot", args) {
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardInput = true
			};
			using (var proc = Process.Start(psi)) {
				WriteDotTo(proc.StandardInput, options);
				proc.StandardInput.Close();
				proc.WaitForExit();
			}

		}

		/// <summary>
		/// Renders Graphviz output for this machine to a stream
		/// </summary>
		/// <param name="format">The output format. The format to render can be any supported dot output format. See dot command line documation for details.</param>
		/// <param name="copy">True to copy the stream, otherwise false</param>
		/// <param name="options">A <see cref="DotGraphOptions"/> instance with any options, or null to use the defaults</param>
		/// <returns>A stream containing the output. The caller is expected to close the stream when finished.</returns>
		public Stream RenderToStream(string format, bool copy = false, DotGraphOptions options = null) {
			if (null == options)
				options = new DotGraphOptions();
			string args = "-T";
			args += string.Concat(" ", format);
			if (0 < options.Dpi)
				args += " -Gdpi=" + options.Dpi.ToString();

			var psi = new ProcessStartInfo("dot", args) {
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardInput = true,
				RedirectStandardOutput = true
			};
			using (var proc = Process.Start(psi)) {
				WriteDotTo(proc.StandardInput, options);
				proc.StandardInput.Close();
				if (!copy)
					return proc.StandardOutput.BaseStream;
				else {
					var stm = new MemoryStream();
					proc.StandardOutput.BaseStream.CopyTo(stm);
					proc.StandardOutput.BaseStream.Close();
					proc.WaitForExit();
					return stm;
				}
			}
		}
		static void _WriteDotTo(IList<FA> closure, TextWriter writer, DotGraphOptions options = null) {
			if (null == options) options = new DotGraphOptions();
			string spfx = null == options.StatePrefix ? "q" : options.StatePrefix;
			writer.WriteLine("digraph FFA {");
			writer.WriteLine("rankdir=LR");
			writer.WriteLine("node [shape=circle]");
			var finals = new List<FA>();

			var accepting = new List<FA>();
			foreach (var ffa in closure)
				if (ffa.Transitions.Count==0 && ffa.AcceptSymbolId==-1)
					finals.Add(ffa);

			int i = 0;
			foreach (var ffa in closure) {
				if (!finals.Contains(ffa)) {
					if (ffa.AcceptSymbolId!=-1)
						accepting.Add(ffa);

				}
				foreach(var efa in ffa.FillEpsilonClosure()) {
					if (efa == ffa) continue;
					var ei = closure.IndexOf(efa);
					writer.Write(spfx);
					writer.Write(i);
					writer.Write("->");
					writer.Write(spfx);
					writer.Write(ei.ToString());
					writer.WriteLine(" [style=dashed,color=gray]");
				}
				var rngGrps = ffa.FillInputTransitionRangesGroupedByState();
				foreach (var rngGrp in rngGrps) {
					var di = closure.IndexOf(rngGrp.Key);
					writer.Write(spfx);
					writer.Write(i);
					writer.Write("->");
					writer.Write(spfx);
					writer.Write(di.ToString());
					writer.Write(" [label=\"");
					var sb = new StringBuilder();
					IList<KeyValuePair<int, int>> rngs = rngGrp.Value;
					var sexp = new RegexSetExpression();
					IRegexSetElement sc=null;
					for(var j = 0;j<rngs.Count;++j) {
						var rng = rngs[j];
						var r = new RegexSetRange();
						r.First = rng.Key;
						r.Last = rng.Value;
						if(sc==null) {
							sc = r;
							sexp.First = sc;
                        } else {
							sc.NextElement = r;
							sc = sc.NextElement;
                        }
                    }
					var s1 = sexp.ToString();
					var negr = new RegexSetNegate();
					negr.Next = sexp.First;
					sexp.First = negr;
					rngs = new List<KeyValuePair<int, int>>(sexp.GetRanges());
					sc = new RegexSetNegate();
					sexp.First = sc;
					string s2;
					if (rngs.Count > 0) {
						for (var j = 0; j < rngs.Count; ++j) {
							var rng = rngs[j];
							var r = new RegexSetRange();
							r.First = rng.Key;
							r.Last = rng.Value;
							sc.NextElement = r;
							sc = sc.NextElement;
						}
						s2 = sexp.ToString();
					} else {
						s2 = null;
                    }
					var srng = (s2==null || s1.Length < s2.Length) ? s1 : s2;
					writer.Write(_EscapeLabel(srng));
					writer.WriteLine("\"]");
				}

				++i;
			}

			i = 0;
			foreach (var ffa in closure) {
				writer.Write(spfx);
				writer.Write(i);
				writer.Write(" [");

				writer.Write("label=<");
				writer.Write("<TABLE BORDER=\"0\"><TR><TD>");
				writer.Write(spfx);
				writer.Write("<SUB>");
				writer.Write(i);
				writer.Write("</SUB></TD></TR>");


				if (!options.HideAcceptSymbolIds && ffa.AcceptSymbolId != -1) {
					writer.Write("<TR><TD>");
					writer.Write(Convert.ToString(ffa.AcceptSymbolId).Replace("\"", "&quot;"));
					writer.Write("</TD></TR>");

				}
				writer.Write("</TABLE>");
				writer.Write(">");
				bool isfinal = false;
				if (accepting.Contains(ffa) || (isfinal = finals.Contains(ffa)))
					writer.Write(",shape=doublecircle");
				if (isfinal) {

					writer.Write(",color=gray");

				}
				writer.WriteLine("]");
				++i;
			}
			string delim = "";
			if (0 < accepting.Count) {
				foreach (var ntfa in accepting) {
					writer.Write(delim);
					writer.Write(spfx);
					writer.Write(closure.IndexOf(ntfa));
					delim = ",";
				}
				writer.WriteLine(" [shape=doublecircle]");
			}

			delim = "";
			if (0 < finals.Count) {
				foreach (var ntfa in finals) {
					writer.Write(delim);
					writer.Write(spfx);
					writer.Write(closure.IndexOf(ntfa));
					delim = ",";
				}
				writer.WriteLine(" [shape=doublecircle,color=gray]");
			}

			writer.WriteLine("}");
		}
	}
}
