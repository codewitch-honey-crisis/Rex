using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RX {
#if RXLIB
    public
#endif
    class RegexGroupExpression : RegexExpression {
        public string Group { get; set; } = null;
        public RegexExpression Expression { get; set; } = null;
        public override bool ShouldGroup => false;
        public RegexGroupExpression() {
            
        }
        public RegexGroupExpression(string group, RegexExpression expression) {
            Group = group;
            Expression = expression;
        }
        public RegexGroupExpression(RegexExpression expression) {
            Expression = expression;
        }
        public override void WriteTo(TextWriter writer) {
            if(Expression!=null) {
                if(null==Group) {
                    writer.Write("(?:");
                } else {
                    if (Group == "") {
                        writer.Write("(");
                    } else {
                        writer.Write("(?<");
                        writer.Write(Group);
                        writer.Write(">");
                    }
                }
                Expression.WriteTo(writer);
                writer.Write(")");
            }
        }
        public override bool Equals(RegexExpression rhs) {
            return Equals(rhs as RegexGroupExpression);
        }
        public bool Equals(RegexGroupExpression rhs) {
            if (object.ReferenceEquals(rhs, this)) return true;
            if (object.ReferenceEquals(rhs, null)) return false;
            if (rhs.Group != Group) return false;
            if (object.ReferenceEquals(Expression, rhs.Expression)) return true;
            if (object.ReferenceEquals(Expression, null)) return false;
            return Expression.Equals(rhs.Expression);
        }
        public override FA ToFA(int accept = 0) {
            if(Expression==null) {
                var fa = new FA();
                fa.AcceptSymbolId = accept;
                return fa;
            }
            return Expression.ToFA(accept);
        }
        public override RegexExpression Clone() {
            var result = new RegexGroupExpression();
            result.Group = Group;
            if (Expression != null) {
                result.Expression = Expression.Clone();
            }
            return result;
        }
        public override bool TryReduce(out RegexExpression reduced) {
            if(Group!=null) {
                var e = Expression;
                var r = false;
                while (e != null && e.TryReduce(out e)) r = true;
                if(r) {
                    reduced = new RegexGroupExpression(Group, e);
                    return true;
                }
                reduced = this;
                return false;
            }
            if(Expression==null) {
                reduced = null;
                return true;
            }
            reduced = Expression;
            while (reduced != null && reduced.TryReduce(out reduced)) ;
            return true;
        }
    }
}
