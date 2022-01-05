using System;
using System.Collections.Generic;
using RX;
namespace Demo {
    class Program {
        static void Main(string[] args) {
            var rx = RegexExpression.Parse(@"[A-Z_a-z][A-Z_a-z0-9]*");
            Console.WriteLine(rx);
            var opts = new FA.DotGraphOptions();
            opts.HideAcceptSymbolIds = true;
            var fa = rx.ToFA();
            fa.RenderToFile(@"..\..\..\test.jpg", opts);
            var rx2= RegexExpression.FromFA(fa);
            Console.WriteLine(rx2);
            Console.WriteLine(rx2.Reduce());
            var dfa = fa.Clone();
            dfa.Minimize();
            dfa.RenderToFile(@"..\..\..\test_dfa.jpg", opts);
            var rx3 = RegexExpression.FromFA(dfa);
            Console.WriteLine(rx3);
            if (rx3 != null) {
                rx3 = rx3.Reduce();
                Console.WriteLine(rx3);
            }
            var mfa = dfa.Clone();
            mfa.Maximize();
            mfa.RenderToFile(@"..\..\..\test_max.jpg", opts);

        }
    }
}
