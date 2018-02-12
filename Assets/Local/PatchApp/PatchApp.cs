using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace patchapp {

public class Utils {

    static public T Clone<T>(T o) {
        return (T)Clone(typeof(T), o);
    }

    static public object Clone(Type t, object o) {
        if (t == typeof(char) || t == typeof(string) || t.IsEnum ||
            t == typeof(bool) || t == typeof(int) || t == typeof(long) ||
            t == typeof(float) || t == typeof(double)) {
            
            // atom
            return o;
        } else if (
            t.IsGenericType &&
            t.GetGenericTypeDefinition() == typeof(List<>)) {

            // List<>
            var dl = Activator.CreateInstance(t);
            var sl =(IList)o;

            if (sl != null) {
                foreach(object e in sl) {
                    ((IList)dl).Add(
                        Clone(t.GetGenericArguments()[0], e));
                }
            }
            return dl;
        } else if (
            t.IsGenericType &&
            t.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {

            // Dictionary<>
            IDictionary dd =(IDictionary)Activator.CreateInstance(t);
            var sd =(IDictionary)o;

            if (sd != null) {
                foreach(DictionaryEntry e in sd) {
                    var ga = t.GetGenericArguments();
                    dd.Add(
                        Clone(ga[0], e.Key),
                        Clone(ga[1], e.Value));
                }
            }
            return dd;
        } else if (t.IsValueType && !t.IsPrimitive) {

            // Struct
            var sv = o;
            var v = Activator.CreateInstance(t);
            foreach(var f in t.GetFields()) {
                f.SetValue(v, Clone(f.FieldType, f.GetValue(sv)));
            }
            return v;
        } else {
            Debug.Log("Loading Unknown" + t.Name);
        }
        return null;
    }

    static public void Log<T>(T o) {
        Debug.Log(ToString(o));
    }

    static public string ToString<T>(T o) {
        return ToString(typeof(T), o);
    }

    static public string ToString(Type t, object o) {
        if (o == null) {
            // null
            return "<null>";
        } else if (
            t == typeof(char) || t.IsEnum ||
            t == typeof(bool) || t == typeof(int) || t == typeof(long) ||
            t == typeof(float) || t == typeof(double)) {
            // atom
            return Convert.ToString(o);
        } else if (t == typeof(string)) {
            // string
            return "\"" + Convert.ToString(o)+ "\"";
        } else if (
            t.IsGenericType &&
            t.GetGenericTypeDefinition() == typeof(List<>)) {

            // List<>
            if (o == null) {
                return "<null>";
            } else {
                var dl = new List<string>();
                var head = "";
                var delimiter = ", ";
                foreach(object e in(IEnumerable)o) {
                    var s = ToString(t.GetGenericArguments()[0], e);
                    if (s.EndsWith("\n")) {
                        head = "\n";
                        delimiter = "\n";
                    }
                    dl.Add(s);
                }
                return "[" + head + string.Join(delimiter, dl.ToArray())+ "]";
            }
        } else if (
            t.IsGenericType &&
            t.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {

            // Dictionary<>
            var dl = new List<string>();
            foreach(DictionaryEntry e in(IDictionary)o) {
                dl.Add(
                    ToString(t.GetGenericArguments()[0], e.Key)+ " => " +
                    ToString(t.GetGenericArguments()[1], e.Value));
            }
            return " { " + string.Join(", ", dl.ToArray())+ " }";
        } else if (t.IsValueType && !t.IsPrimitive) {

            // struct
            var dl = new List<string>();
            foreach(var f in t.GetFields()) {
                dl.Add(f.Name + ": " + ToString(f.FieldType, f.GetValue(o)));
            }
            return t.Name + " { " + string.Join(", ", dl.ToArray())+ " }";
        } else if (o is Difference) {
            return o.ToString();
        }
                   
        return "<unknown>";
    }

    public static List<Difference> Diff<U>(U a, U b) {
        var diffs = new List<Difference>();
        Diff("", typeof(U), a, b, diffs);
        return diffs;
    }
        
    static void Diff(
        string path,
        Type t,
        object a,
        object b,
        List<Difference> diffs) {

        if (t == typeof(char) || t == typeof(string) || t.IsEnum ||
            t == typeof(bool) || t == typeof(int) || t == typeof(long) ||
            t == typeof(float) || t == typeof(double)) {

            // atom
            if (!a.Equals(b)) {
                diffs.Add(new Modify(path, b));
            }
        } else if (
            t.IsGenericType &&
            t.GetGenericTypeDefinition() == typeof(List<>)) {

            // List<>
            DiffList(path, t.GetGenericArguments()[0], a, b, diffs);
        } else if (
            t.IsGenericType &&
            t.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {

            // Dictionary<>
            var path2 = path + "/";
            var dic1 = (IDictionary)a;
            var dic2 = (IDictionary)b;
            foreach(DictionaryEntry e in dic1) {
                if (!dic2.Contains(e.Key)) {
                    diffs.Add(new Delete(path2 + e.Key));
                } else {
                    Diff(
                        path2 + e.Key, t,
                        e.Value, dic2[e.Key], diffs);
                }
            }
            foreach (DictionaryEntry e in dic2) {
                if (!dic1.Contains(e.Key)) {
                    diffs.Add(new Insert(path2 + e.Key, e.Value));
                }
            }
        } else if (t.IsValueType && !t.IsPrimitive) {
            // struct
            var path2 = path + "/";
            foreach(var f in t.GetFields()) {
                Diff(
                    path2 + f.Name, f.FieldType, 
                    f.GetValue(a), f.GetValue(b), diffs);
            }
        }
    }

    public static List<Difference> DiffList<T>(List<T> a, List<T> b) {
        var diffs = new List<Difference>();
        DiffList("", typeof(T), a, b, diffs);
        return diffs;
    }

    public static void DiffList(
        string path, Type t, object a, object b, List<Difference> diffs) {
        var al = (IList)a;
        var bl = (IList)b;
        var aa = new EnumerableAdaptor(al);
        var bb = new EnumerableAdaptor(bl);

        var option = new NetDiff.DiffOption<object>();
        option.EqualityComparer = new EqualityComparer();

        var results = NetDiff.DiffUtil.Diff(aa, bb, option);
        var ordered = NetDiff.DiffUtil.Order(
            results, NetDiff.DiffOrderType.LazyDeleteFirst);
        var optimized = NetDiff.DiffUtil.OptimizeCaseDeletedFirst(ordered);
/*
        foreach (var r in optimized) {
            Debug.Log(r.ToFormatString());
        }
        */
        
        int index = 0;
        foreach (var r in optimized) {
            switch(r.Status) {
                case NetDiff.DiffStatus.Equal: index++; break;
                case NetDiff.DiffStatus.Inserted:
                    diffs.Add(
                        new Insert(path + "/" + index.ToString(), r.Obj2));
                    index++;
                    break;
                case NetDiff.DiffStatus.Deleted:
                    diffs.Add(new Delete(path + "/" + index.ToString()));
                    break;
                case NetDiff.DiffStatus.Modified:
                    int prev = diffs.Count;
                    Diff(
                        path + "/" + index.ToString(), t,
                        al[index], bl[index], diffs);
                    index++;
                    break;
            }
        }
        //Debug.Log(ToString(diffs));
    }

}

class EqualityComparer : IEqualityComparer<object> {
    public bool Equals(object a, object b) {
        if (a == null && b == null) { return true; }
        if (a != null && b == null) { return false; }
        if (a == null && b != null) { return false; }

        var t = a.GetType();
        var bt = b.GetType();
        if (t != bt) { return false; }

        if (t == typeof(char) || t == typeof(string) || t.IsEnum ||
            t == typeof(bool) || t == typeof(int) || t == typeof(long) ||
            t == typeof(float) || t == typeof(double)) {

            return a.Equals(b);
        } else if (
            t.IsGenericType &&
            t.GetGenericTypeDefinition() == typeof(List<>)) {

            // List<>
            var al = (IList)a;
            var bl = (IList)b;
            if (al.Count != bl.Count) { return false; }

            var be = bl.GetEnumerator();
            foreach(object e in al) {
                be.MoveNext();
                if (!Equals(e, be.Current)) {
                    return false;
                }
            }
            return true;
        } else if (
            t.IsGenericType &&
            t.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {

            // Dictionary<>
            var ad = (IDictionary)a;
            var bd = (IDictionary)b;
            if (ad.Count != bd.Count) { return false; }
            
            foreach(DictionaryEntry e in ad) {
                if (!bd.Contains(e.Key)) { return false; }
                if (!Equals(e.Value, bd[e.Key])) { return false; }
            }
            return true;
        } else if (t.IsValueType && !t.IsPrimitive) {
            // struct
            foreach(var f in t.GetFields()) {
                if (!Equals(f.GetValue(a), f.GetValue(b))) { return false; }
            }
            return true;
        }
        return false;
    }
    public int GetHashCode(object a) {
        return a.GetHashCode();
    }
}

public abstract class Difference {
    public string path;
}

public class Modify : Difference {
    object value;
    public Modify(string path, object a) {
        this.path = path;
        this.value = a;
    }
    public override string ToString() {
        return path + " @: " + value.ToString() + "\n";
    }
    public override bool Equals(object obj) {
        if (obj == null || !(obj is Modify)) { return false; }
        Modify m = (Modify)obj;
        return this.path == m.path && this.value.Equals(m.value);
    }
    public override int GetHashCode() {
        return path.GetHashCode() ^ value.GetHashCode();
    }
}

public class Insert : Difference {
    object value;
    public Insert(string path, object a) {
        this.path = path;
        this.value = a;
    }
    public override string ToString() {
        return path + " +: " + value.ToString() + "\n";
    }
    public override bool Equals(object obj) {
        if (obj == null || !(obj is Insert)) { return false; }
        Insert m = (Insert)obj;
        return this.path == m.path && this.value.Equals(m.value);
    }
    public override int GetHashCode() {
        return path.GetHashCode() ^ value.GetHashCode();
    }
}

public class Delete : Difference {
    public Delete(string path) { this.path = path; }
    public override string ToString() {
        return path + " -: " + "\n";
    }
    public override bool Equals(object obj) {
        if (obj == null || !(obj is Delete)) { return false; }
        Delete m = (Delete)obj;
        return this.path == m.path;
    }
    public override int GetHashCode() {
        return path.GetHashCode();
    }
}

public class PatchApp<T> {
    T previous;

    public PatchApp() {}

    public void Apply(T o) {
        if (previous == null) {
            previous = o;
            return;
        } else {
            // diff between previous and o
        }
    }

}

public class EnumeratorAdaptor : IEnumerator<object> {
    IEnumerator e;
        
    public EnumeratorAdaptor(IEnumerator e) { this.e = e; }
    public object Current { get { return e.Current; } }
    public bool MoveNext() { return e.MoveNext(); }
    public void Reset() {  e.Reset(); }
    public void Dispose() {} 
}

public class EnumerableAdaptor : IEnumerable<object> {
    IEnumerable e;
        
    public EnumerableAdaptor(IEnumerable e) { this.e = e; }
    public IEnumerator<object> GetEnumerator() {
        return new EnumeratorAdaptor(e.GetEnumerator());
    }
    IEnumerator IEnumerable.GetEnumerator() {
        return this.GetEnumerator();
    }
}

}