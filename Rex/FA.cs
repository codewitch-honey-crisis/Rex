using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace RX {
#if RXLIB
    public
#endif
    struct FATransition : IComparable<FATransition> {
        public int Min; // -1 for Epsilon
        public int Max; // -1 for Epsilon
        public FA To;
        public FATransition(int min,int max,FA to) {
            Min = min;
            Max = max;
            To = to;
        }
        public FATransition(FA to) {
            Min = Max = -1;
            To = to;
        }
        public int CompareTo([AllowNull] FATransition other) {
            var c = Min.CompareTo(other.Min);
            if (c != 0) return c;
            return Max.CompareTo(other.Max);
        }
    }
#if RXLIB
    public
#endif
    partial class FA {
        private sealed class _KeySet<T> : ISet<T>, IEquatable<_KeySet<T>> {
            HashSet<T> _inner;
            int _hashCode;
            public _KeySet(IEqualityComparer<T> comparer) {
                _inner = new HashSet<T>(comparer);
                _hashCode = 0;
            }
            public _KeySet() {
                _inner = new HashSet<T>();
                _hashCode = 0;
            }
            public int Count => _inner.Count;
            
            public bool IsReadOnly => true;

            // hack - we allow this method so the set can be filled
            public bool Add(T item) {
                if (null != item)
                    _hashCode ^= item.GetHashCode();
                return _inner.Add(item);
            }
            bool ISet<T>.Add(T item) {
                _ThrowReadOnly();
                return false;
            }
            public void Clear() {
                _ThrowReadOnly();
            }

            public bool Contains(T item) {
                return _inner.Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex) {
                _inner.CopyTo(array, arrayIndex);
            }

            void ISet<T>.ExceptWith(IEnumerable<T> other) {
                _ThrowReadOnly();
            }

            public IEnumerator<T> GetEnumerator() {
                return _inner.GetEnumerator();
            }

            void ISet<T>.IntersectWith(IEnumerable<T> other) {
                _ThrowReadOnly();
            }

            public bool IsProperSubsetOf(IEnumerable<T> other) {
                return _inner.IsProperSubsetOf(other);
            }

            public bool IsProperSupersetOf(IEnumerable<T> other) {
                return _inner.IsProperSupersetOf(other);
            }

            public bool IsSubsetOf(IEnumerable<T> other) {
                return _inner.IsSubsetOf(other);
            }

            public bool IsSupersetOf(IEnumerable<T> other) {
                return _inner.IsSupersetOf(other);
            }

            public bool Overlaps(IEnumerable<T> other) {
                return _inner.Overlaps(other);
            }

            bool ICollection<T>.Remove(T item) {
                _ThrowReadOnly();
                return false;
            }

            public bool SetEquals(IEnumerable<T> other) {
                return _inner.SetEquals(other);
            }

            void ISet<T>.SymmetricExceptWith(IEnumerable<T> other) {
                _ThrowReadOnly();
            }

            void ISet<T>.UnionWith(IEnumerable<T> other) {
                _ThrowReadOnly();
            }

            void ICollection<T>.Add(T item) {
                _ThrowReadOnly();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
                return _inner.GetEnumerator();
            }
            static void _ThrowReadOnly() {
                throw new NotSupportedException("The set is read only");
            }
            public bool Equals(_KeySet<T> rhs) {
                if (ReferenceEquals(this, rhs))
                    return true;
                if (ReferenceEquals(rhs, null))
                    return false;
                if (rhs._hashCode != _hashCode)
                    return false;
                var ic = _inner.Count;
                if (ic != rhs._inner.Count)
                    return false;
                return _inner.SetEquals(rhs._inner);
            }
            public override int GetHashCode() {
                return _hashCode;
            }
        }
        private class _TransSrchCmp : IComparer<FATransition> {
            public int Compare([AllowNull] FATransition x, [AllowNull] FATransition y) {
                return x.Min.CompareTo(y.Min);
            }
            public static readonly _TransSrchCmp Default = new _TransSrchCmp();
        }
        public FA(int accept = -1) {
            AcceptSymbolId = accept;
        }
        List<FATransition> _transitions = new List<FATransition>();
        public IList<FATransition> Transitions { get { return _transitions.AsReadOnly(); } }
        public int Tag { get; set; }
        public bool IsDeterministic { get; private set; } = true;
        public int AcceptSymbolId { get; set; } = -1;
        public IList<FA> FillClosure(IList<FA> result = null) {
            if(null==result) {
                result = new List<FA>();
            }
            if (!result.Contains(this)) {
                result.Add(this);
                for (var i = 0; i < _transitions.Count; ++i) {
                    _transitions[i].To.FillClosure(result);
                }
            }
            return result;
        }
        public IList<FA> FillEpsilonClosure(IList<FA> result = null) {
            if(result == null) {
                result = new List<FA>();
            }
            if (!result.Contains(this))
                result.Add(this);
            for (var i = 0; i < _transitions.Count; ++i) {
                var t = _transitions[i];
                if (t.Min != -1 || t.Max != -1) break;
                _transitions[i].To.FillEpsilonClosure(result);
            }
            return result;
        }
        public FA FirstAcceptingState {
            get {
                foreach(var fa in FillClosure()) {
                    if(fa.AcceptSymbolId!=-1) {
                        return fa;
                    }
                }
                return null;               
            }
        }
        public void AddEpsilon(FA to)
            => AddTransition(-1, -1, to);
        public void AddTransition(int min,int max,FA to) {
            int i;
            if(min>max) {
                i = min;
                min = max;
                max = i;
            }
            if(min==-1&&max==-1) {
                for(i=0;i<_transitions.Count;++i) {
                    var t = _transitions[i];
                    if (t.Min != -1 || t.Max != -1)
                        break;
                    if (t.To == to) {
                        return;
                    }
                }
                _transitions.Insert(i, new FATransition(to));
                IsDeterministic = false;
                return;
            }
            var tr = new FATransition(min, max, to);
            i = _transitions.BinarySearch(tr, _TransSrchCmp.Default);
            if(0>i) {
                i = ~i;
            }
            _transitions.Insert(i, new FATransition(min, max, to));
            if(i>0) {
                var pt = _transitions[i - 1];
                if (pt.To == to) {
                    if (pt.Max + 1 >= min) {
                        min = pt.Min;
                        _transitions[i] = new FATransition(min, max, to);
                        _transitions.RemoveAt(--i);
                    } else {
                        if(pt.Max>=min) {
                            IsDeterministic = false;
                        }
                    }
                }
            }
            var j = i + 1;
            while(j<_transitions.Count) {
                var t = _transitions[j];
                if (t.To == to) {
                    if (max + 1 < t.Min)
                        break;
                    _transitions[i] = new FATransition(min, t.Max, to);
                    _transitions.RemoveAt(j);
                } else {
                    if(max>=t.Min) {
                        IsDeterministic = false;
                    }
                }
            }
            
        }
        public FA Clone() {
            var closure = FillClosure();
            var result = new FA[closure.Count];
            for (var i = 0; i < result.Length; ++i) {
                result[i] = new FA();
            }
            for (var i = 0; i < result.Length; ++i) {
                var fa = closure[i];
                var cfa = result[i];
                cfa.IsDeterministic = fa.IsDeterministic;
                cfa.AcceptSymbolId = fa.AcceptSymbolId;
                for (var j = 0; j < fa._transitions.Count; ++j) {
                    var t = fa._transitions[j];
                    cfa._transitions.Add(new FATransition(t.Min, t.Max, result[closure.IndexOf(t.To)]));
                }
            }
            return result[0];
        }
        void _FlattenImpl() {
            var done = false;
            while (!done) {
                done = true;
                if (_transitions.Count > 0) {
                    var t = _transitions[0];
                    if (t.Min == -1 && t.Max == -1) {
                        _transitions.RemoveAt(0);
                        done = false;
                        if (t.To != this) {
                            // has more epsilons
                            var dst = t.To;
                            if (dst.AcceptSymbolId != -1) {
                                AcceptSymbolId = dst.AcceptSymbolId;
                            }
                            for (var i = 0; i < dst._transitions.Count; ++i) {
                                t = dst._transitions[i];
                                AddTransition(t.Min, t.Max, t.To);
                            }
                        }
                    }
                }
            }
        }
        public void Flatten() {
            foreach (var fa in FillClosure()) {
                fa._FlattenImpl();
            }
        }
        public void Totalize() {
            var s = new FA();
            s._transitions.Add(new FATransition(0, 0x10ffff, s));
            foreach (var p in FillClosure()) {
                int maxi = 0;
                var sortedTrans = new List<FATransition>(p._transitions);
                sortedTrans.Sort((x, y) => { var c = x.Min.CompareTo(y.Min); if (0 != c) return c; return x.Max.CompareTo(y.Max); });
                foreach (var t in sortedTrans) {
                    if (t.Min > maxi) {
                        p._transitions.Add(new FATransition(maxi, (t.Min - 1), s));
                    }

                    if (t.Max + 1 > maxi) {
                        maxi = t.Max + 1;
                    }
                }

                if (maxi <= 0x10ffff) {
                    p._transitions.Add(new FATransition(maxi, 0x10ffff, s));
                }
            }
        }
        public void Determinize() {
            Flatten();
            var p = new HashSet<int>();
            var closure = new List<FA>();
            FillClosure(closure);
            for (int ic = closure.Count, i = 0; i < ic; ++i) {
                var ffa = closure[i];
                p.Add(0);
                foreach (var t in ffa.Transitions) {
                    p.Add(t.Min);
                    if (t.Max < 0x10ffff) {
                        p.Add((t.Max + 1));
                    }
                }
            }

            var points = new int[p.Count];
            p.CopyTo(points, 0);
            Array.Sort(points);

            var sets = new Dictionary<_KeySet<FA>, _KeySet<FA>>();
            var working = new Queue<_KeySet<FA>>();
            var dfaMap = new Dictionary<_KeySet<FA>, FA>();
            var initial = new _KeySet<FA>();
            initial.Add(this);
            sets.Add(initial, initial);
            working.Enqueue(initial);
            var result = new FA();
            foreach (var afa in initial) {
                if (afa.AcceptSymbolId!=-1) {
                    result.AcceptSymbolId = afa.AcceptSymbolId;
                    break;
                }
            }
            dfaMap.Add(initial, result);
            while (working.Count > 0) {
                var s = working.Dequeue();
                FA dfa;
                dfaMap.TryGetValue(s, out dfa);
                foreach (FA q in s) {
                    if (q.AcceptSymbolId!=-1) {
                        dfa.AcceptSymbolId = q.AcceptSymbolId;
                        break;
                    }
                }

                for (var i = 0; i < points.Length; i++) {
                    var pnt = points[i];
                    var set = new _KeySet<FA>();
                    foreach (FA c in s) {
                        foreach (var trns in c._transitions) {
                            if (trns.Min <= pnt && pnt <= trns.Max) {
                                set.Add(trns.To);
                            }
                        }
                    }
                    if (!sets.ContainsKey(set)) {
                        sets.Add(set, set);
                        working.Enqueue(set);
                        dfaMap.Add(set, new FA());
                    }

                    FA dst;
                    dfaMap.TryGetValue(set, out dst);
                    int first = pnt;
                    int last;
                    if (i + 1 < points.Length)
                        last = (points[i + 1] - 1);
                    else
                        last = 0x10ffff;
                    dfa._transitions.Add(new FATransition(first, last, dst));
                }

            }
            // remove dead transitions
            foreach (var ffa in result.FillClosure()) {
                var itrns = new List<FATransition>(ffa._transitions);
                foreach (var trns in itrns) {
                    var acc = trns.To.FirstAcceptingState;
                    if (null==acc) {
                        ffa._transitions.Remove(trns);
                    }
                }
            }
            _transitions.Clear();
            IsDeterministic = true;
            foreach(var t in result._transitions) {
                _transitions.Add(t);
            }
        }
        FA _Step(int input) {
            for (int ic = _transitions.Count, i = 0; i < ic; ++i) {
                var t = _transitions[i];
                if (t.Min <= input && input <= t.Max)
                    return t.To;

            }
            return null;
        }
        static void _Init<T>(IList<T> list, int count) {
            for (int i = 0; i < count; ++i) {
                list.Add(default(T));
            }
        }
        private sealed class _IntPair {
            private readonly int n1;
            private readonly int n2;

            public _IntPair(int n1, int n2) {
                this.n1 = n1;
                this.n2 = n2;
            }

            public int N1 {
                get { return n1; }
            }

            public int N2 {
                get { return n2; }
            }
        }
        private sealed class _FList {
            public int Count { get; set; }

            public _FListNode First { get; set; }

            public _FListNode Last { get; set; }

            public _FListNode Add(FA q) {
                return new _FListNode(q, this);
            }
        }
        private sealed class _FListNode {
            public _FListNode(FA q, _FList sl) {
                State = q;
                StateList = sl;
                if (sl.Count++ == 0) {
                    sl.First = sl.Last = this;
                } else {
                    sl.Last.Next = this;
                    Prev = sl.Last;
                    sl.Last = this;
                }
            }

            public _FListNode Next { get; private set; }

            private _FListNode Prev { get; set; }

            public _FList StateList { get; private set; }

            public FA State { get; private set; }

            public void Remove() {
                StateList.Count--;
                if (StateList.First == this) {
                    StateList.First = Next;
                } else {
                    Prev.Next = Next;
                }

                if (StateList.Last == this) {
                    StateList.Last = Prev;
                } else {
                    Next.Prev = Prev;
                }
            }
        }

        static void _MinimizeImpl(FA a) {
            var result = a;
            a.Determinize();
            var tr = a._transitions;
            if (1 == tr.Count) {
                FATransition t = tr[0];
                if (t.To == a && t.Min == 0 && t.Max == 0x10ffff) {
                    return;
                }
            }

            a.Totalize();

            // Make arrays for numbered states and effective alphabet.
            var cl = a.FillClosure();
            var states = new FA[cl.Count];
            int number = 0;
            foreach (var q in cl) {
                states[number] = q;
                q.Tag = number;
                ++number;
            }

            var pp = new List<int>();
            for (int ic = cl.Count, i = 0; i < ic; ++i) {
                var ffa = cl[i];
                pp.Add(0);
                foreach (var t in ffa._transitions) {
                    pp.Add(t.Min);
                    if (t.Max < 0x10ffff) {
                        pp.Add((t.Max + 1));
                    }
                }
            }

            var sigma = new int[pp.Count];
            pp.CopyTo(sigma, 0);
            Array.Sort(sigma);

            // Initialize data structures.
            var reverse = new List<List<Queue<FA>>>();
            foreach (var s in states) {
                var v = new List<Queue<FA>>();
                _Init(v, sigma.Length);
                reverse.Add(v);
            }

            var reverseNonempty = new bool[states.Length, sigma.Length];

            var partition = new List<LinkedList<FA>>();
            _Init(partition, states.Length);

            var block = new int[states.Length];
            var active = new _FList[states.Length, sigma.Length];
            var active2 = new _FListNode[states.Length, sigma.Length];
            var pending = new Queue<_IntPair>();
            var pending2 = new bool[sigma.Length, states.Length];
            var split = new List<FA>();
            var split2 = new bool[states.Length];
            var refine = new List<int>();
            var refine2 = new bool[states.Length];

            var splitblock = new List<List<FA>>();
            _Init(splitblock, states.Length);

            for (int q = 0; q < states.Length; q++) {
                splitblock[q] = new List<FA>();
                partition[q] = new LinkedList<FA>();
                for (int x = 0; x < sigma.Length; x++) {
                    reverse[q][x] = new Queue<FA>();
                    active[q, x] = new _FList();
                }
            }

            // Find initial partition and reverse edges.
            foreach (var qq in states) {
                int j = qq.AcceptSymbolId!=-1 ? 0 : 1;

                partition[j].AddLast(qq);
                block[qq.Tag] = j;
                for (int x = 0; x < sigma.Length; x++) {
                    var y = sigma[x];
                    var p = qq._Step(y);
                    var pn = p.Tag;
                    reverse[pn][x].Enqueue(qq);
                    reverseNonempty[pn, x] = true;
                }
            }

            // Initialize active sets.
            for (int j = 0; j <= 1; j++) {
                for (int x = 0; x < sigma.Length; x++) {
                    foreach (var qq in partition[j]) {
                        if (reverseNonempty[qq.Tag, x]) {
                            active2[qq.Tag, x] = active[j, x].Add(qq);
                        }
                    }
                }
            }

            // Initialize pending.
            for (int x = 0; x < sigma.Length; x++) {
                int a0 = active[0, x].Count;
                int a1 = active[1, x].Count;
                int j = a0 <= a1 ? 0 : 1;
                pending.Enqueue(new _IntPair(j, x));
                pending2[x, j] = true;
            }

            // Process pending until fixed point.
            int k = 2;
            while (pending.Count > 0) {
                _IntPair ip = pending.Dequeue();
                int p = ip.N1;
                int x = ip.N2;
                pending2[x, p] = false;

                // Find states that need to be split off their blocks.
                for (var m = active[p, x].First; m != null; m = m.Next) {
                    foreach (var s in reverse[m.State.Tag][x]) {
                        if (!split2[s.Tag]) {
                            split2[s.Tag] = true;
                            split.Add(s);
                            int j = block[s.Tag];
                            splitblock[j].Add(s);
                            if (!refine2[j]) {
                                refine2[j] = true;
                                refine.Add(j);
                            }
                        }
                    }
                }

                // Refine blocks.
                foreach (int j in refine) {
                    if (splitblock[j].Count < partition[j].Count) {
                        LinkedList<FA> b1 = partition[j];
                        LinkedList<FA> b2 = partition[k];
                        foreach (var s in splitblock[j]) {
                            b1.Remove(s);
                            b2.AddLast(s);
                            block[s.Tag] = k;
                            for (int c = 0; c < sigma.Length; c++) {
                                _FListNode sn = active2[s.Tag, c];
                                if (sn != null && sn.StateList == active[j, c]) {
                                    sn.Remove();
                                    active2[s.Tag, c] = active[k, c].Add(s);
                                }
                            }
                        }

                        // Update pending.
                        for (int c = 0; c < sigma.Length; c++) {
                            int aj = active[j, c].Count;
                            int ak = active[k, c].Count;
                            if (!pending2[c, j] && 0 < aj && aj <= ak) {
                                pending2[c, j] = true;
                                pending.Enqueue(new _IntPair(j, c));
                            } else {
                                pending2[c, k] = true;
                                pending.Enqueue(new _IntPair(k, c));
                            }
                        }

                        k++;
                    }

                    foreach (var s in splitblock[j]) {
                        split2[s.Tag] = false;
                    }

                    refine2[j] = false;
                    splitblock[j].Clear();
                }

                split.Clear();
                refine.Clear();
            }

            // Make a new state for each equivalence class, set initial state.
            var newstates = new FA[k];
            for (int n = 0; n < newstates.Length; n++) {
                var s = new FA();
                newstates[n] = s;
                foreach (var q in partition[n]) {
                    if (q == a) {
                        a = s;
                    }

                    s.AcceptSymbolId = q.AcceptSymbolId;
                    s.Tag = q.Tag; // Select representative.
                    q.Tag = n;
                }
            }

            // Build transitions and set acceptance.
            foreach (var s in newstates) {
                var st = states[s.Tag];
                s.AcceptSymbolId = st.AcceptSymbolId;
                foreach (var t in st._transitions) {
                    s._transitions.Add(new FATransition(t.Min, t.Max, newstates[t.To.Tag]));
                }
            }
            // remove dead transitions
            foreach (var ffa in a.FillClosure()) {
                var itrns = new List<FATransition>(ffa._transitions);
                foreach (var trns in itrns) {
                    var acc = trns.To.FirstAcceptingState;
                    if (null == acc) {
                        ffa._transitions.Remove(trns);
                    }
                }
            }
            result._transitions.Clear();
            result.AcceptSymbolId = a.AcceptSymbolId;
            result.IsDeterministic = true;
            foreach(var t in a._transitions) {
                result._transitions.Add(t);
            }
        }
        public void Minimize() {
            _MinimizeImpl(this);
        }
        public void Maximize() {
            var fa = RegexExpression.FromFA(this).ToFA(FirstAcceptingState.AcceptSymbolId);
            _transitions.Clear();
            IsDeterministic = fa.IsDeterministic;
            AcceptSymbolId = fa.AcceptSymbolId;
            for(var i = 0;i<fa._transitions.Count;++i) {
                var t = fa._transitions[i];
                _transitions.Add(new FATransition(t.Min,t.Max,t.To));
            }
        }
        public override string ToString() {
            var e= RegexExpression.FromFA(this);
            if (e != null) e = e.Reduce();
            if (e != null) return e.ToString();
            return "";
        }
        public IDictionary<FA,IList<KeyValuePair<int,int>>> FillInputTransitionRangesGroupedByState(bool includeEpsilonClosure=false,IDictionary<FA, IList<KeyValuePair<int, int>>> result = null) {
            if(result==null) {
                result = new Dictionary<FA, IList<KeyValuePair<int, int>>>();
            }
            if (!includeEpsilonClosure) {
                for (var i = 0; i < _transitions.Count; ++i) {
                    var t = _transitions[i];
                    if (t.Min != -1 || t.Max != -1) {
                        IList<KeyValuePair<int, int>> rgs;
                        if (!result.TryGetValue(t.To, out rgs)) {
                            rgs = new List<KeyValuePair<int, int>>();
                            result.Add(t.To, rgs);
                        }
                        rgs.Add(new KeyValuePair<int, int>(t.Min, t.Max));
                    }
                }
            } else {
                foreach(var fa in FillEpsilonClosure()) {
                    fa.FillInputTransitionRangesGroupedByState(false,result);
                }
            }
            return result;
        }
        /// <summary>
		/// Creates a packed DFA table from this machine
		/// </summary>
		/// <returns>A new array that contains this machine as a DFA table</returns>
		public int[] ToDfaTable() {
            var working = new List<int>();
            var fa = this;
            var closure = fa.FillClosure();
            var isDfa = true;
            foreach (var cfa in closure) {
                if (!cfa.IsDeterministic) {
                    isDfa = false;
                    break;
                }
            }
            if (!isDfa) {
                fa = fa.Clone();
                fa.Minimize();
                closure.Clear();
                fa.FillClosure(closure);
            }
            var stateIndices = new int[closure.Count];
            for (var i = 0; i < closure.Count; ++i) {
                var cfa = closure[i];
                stateIndices[i] = working.Count;
                // add the accept
                working.Add(cfa.AcceptSymbolId);
                var itrgp = cfa.FillInputTransitionRangesGroupedByState();
                // add the number of transitions
                working.Add(itrgp.Count);
                foreach (var itr in itrgp) {
                    // We have to fill in the following after the fact
                    // We don't have enough info here
                    // for now just drop the state index as a placeholder
                    working.Add(closure.IndexOf(itr.Key));
                    // add the number of packed ranges
                    working.Add(itr.Value.Count / 2);
                    // add the packed ranges
                    foreach(var kvp in itr.Value) {
                        working.Add(kvp.Key);
                        working.Add(kvp.Value);
                    }
                }
            }
            var result = working.ToArray();
            var state = 0;
            while (state < result.Length) {
                state++;
                var tlen = result[state++];
                for (var i = 0; i < tlen; ++i) {
                    // patch the destination
                    result[state] = stateIndices[result[state]];
                    ++state;
                    var prlen = result[state++];
                    state += prlen * 2;
                }
            }
            return result;
        }

        /// <summary>
        /// Creates a machine based on the given DFA table
        /// </summary>
        /// <param name="dfa">The DFA table</param>
        /// <returns>A new machine based on the DFA table</returns>
        public static FA FromDfaTable(int[] dfa) {
            if (null == dfa) return null;
            if (dfa.Length == 0) return new FA();
            var si = 0;
            var states = new Dictionary<int, FA>();
            while (si < dfa.Length) {
                var fa = new FA();
                states.Add(si, fa);
                fa.AcceptSymbolId = dfa[si++];
                var tlen = dfa[si++];
                for (var i = 0; i < tlen; ++i) {
                    ++si; // tto
                    var prlen = dfa[si++];
                    si += prlen * 2;
                }
            }
            si = 0;
            var sid = 0;
            while (si < dfa.Length) {
                var fa = states[si++];
                var tlen = dfa[si++];
                for (var i = 0; i < tlen; ++i) {
                    var tto = dfa[si++];
                    var to = states[tto];
                    var prlen = dfa[si++];
                    for (var j = 0; j < prlen; ++j) {
                        var pmin = dfa[si++];
                        var pmax = dfa[si++];
                        fa._transitions.Add(new FATransition(pmin, pmax, to));
                    }
                }
                ++sid;
            }
            return states[0];
        }
    }
}
