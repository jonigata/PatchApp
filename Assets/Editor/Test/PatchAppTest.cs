using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;

public struct Foo {
    public int foo;
    public Bar bar;
    public string zot;
}

public struct Bar {
    public float baz;
    public string qux;
}

public struct Baz {
    public Foo foo;
    public List<string> quux;
}

public struct Zot {
    public Bar bar;
    public Foo foo;
    public List<Baz> baz;
}

public class PatchAppTest {
    Foo MockUpFoo() {
        Foo foo = new Foo();
        foo.foo = 7;
        foo.zot = "hello";
        foo.bar = MockUpBar();
        return foo;
    }

    Bar MockUpBar() {
        var bar = new Bar();
        bar.baz = 3.14f;
        bar.qux = "world";
        return bar;
    }

    Baz MockUpBaz() {
        Baz baz = new Baz();
        baz.foo = MockUpFoo();
        baz.quux = new List<string>();
        baz.quux.Add("hello");
        baz.quux.Add("world");
        return baz;
    }

    Zot MockUpZot() {
        Zot zot = new Zot();
        zot.bar = MockUpBar();
        zot.foo = MockUpFoo();
        zot.baz = new List<Baz>() { MockUpBaz(), MockUpBaz(), MockUpBaz() };
        return zot;
    }

    [Test]
    public void ToString() {
        var foo = MockUpFoo();

        var s = "Foo { foo: 7, bar: Bar { baz: 3.14, qux: \"world\" }, zot: \"hello\" }";
        Assert.AreEqual(s, patchapp.Utils.ToString(patchapp.Utils.Clone(foo)));
    }

    [Test]
    public void NoDifference() {
        Foo foo1 = MockUpFoo();
        Foo foo2 = MockUpFoo();

        var diff = patchapp.Utils.Diff(foo1, foo2);
        Assert.IsTrue(diff.Count == 0);
    }

    [Test]
    public void DiffList() {
        List<int> a = new List<int>();
        a.Add(7); a.Add(9);
        List<int> b = new List<int>();
        b.Add(7); b.Add(3); b.Add(9);

        var ideal = new List<patchapp.Difference>() {
            new patchapp.Insert("/1", 3)
        };
        var actual = patchapp.Utils.DiffList(a, b);
        Assert.AreEqual(ideal, actual);
    }

    [Test]
    public void DiffList_Complex() {
        List<int> a = new List<int>();
        a.Add(7); a.Add(9); a.Add(1);
        List<int> b = new List<int>();
        b.Add(7); b.Add(3); b.Add(2); b.Add(1); 

        var ideal = new List<patchapp.Difference>() {
            new patchapp.Modify("/1", 3),
            new patchapp.Insert("/2", 2)
        };
        var actual = patchapp.Utils.DiffList(a, b);
        Assert.AreEqual(ideal, actual);
    }

    [Test]
    public void DiffShallowStruct() {
        Foo foo1 = MockUpFoo();
        Foo foo2 = MockUpFoo();
        foo2.zot = "world";

        var ideal = new List<patchapp.Difference>() {
            new patchapp.Modify("/zot", "world")
        };
        var actual = patchapp.Utils.Diff(foo1, foo2);
        Assert.AreEqual(ideal, actual);
    }

    [Test]
    public void DiffShallowStruct_Fail() {
        Foo foo1 = MockUpFoo();
        Foo foo2 = MockUpFoo();
        foo2.zot = "world";

        var ideal = new List<patchapp.Difference>() {
            new patchapp.Modify("/zot2", "world")
        };
        var actual = patchapp.Utils.Diff(foo1, foo2);
        Assert.AreNotEqual(ideal, actual);
    }

    [Test]
    public void DiffDeepStruct() {
        Foo foo1 = MockUpFoo();
        Foo foo2 = MockUpFoo();
        foo2.bar.baz = 3.0f;

        var ideal = new List<patchapp.Difference>() {
            new patchapp.Modify("/bar/baz", 3.0f)
        };
        var actual = patchapp.Utils.Diff(foo1, foo2);
        Assert.AreEqual(ideal, actual);
    }

    [Test]
    public void DiffStructContaingList() {
        Baz baz1 = MockUpBaz();
        Baz baz2 = MockUpBaz();
        baz2.quux.Insert(1, "dirty");
        var ideal = new List<patchapp.Difference>() {
            new patchapp.Insert("/quux/1", "dirty")
        };
        var actual = patchapp.Utils.Diff(baz1, baz2);
        Assert.AreEqual(ideal, actual);
    }

    [Test]
    public void DiffStructContainingListOfStruct() {
        Zot zot1 = MockUpZot();
        Zot zot2 = MockUpZot();

        var ideal = new List<patchapp.Difference>() {};
        var actual = patchapp.Utils.Diff(zot1, zot2);
        Assert.AreEqual(ideal, actual);
    }

    [Test]
    public void DiffStructMinimalDifference() {
        Zot zot1 = MockUpZot();
        Zot zot2 = MockUpZot();

        zot2.baz[2].quux[1] = "grande";

        var ideal = new List<patchapp.Difference>() {
            new patchapp.Modify("/baz/2/quux/1", "grande")
        };
        var actual = patchapp.Utils.Diff(zot1, zot2);
        Assert.AreEqual(ideal, actual);
    }

    [Test]
    public void DoNothing() {
    }

    // A UnityTest behaves like a coroutine in PlayMode
    // and allows you to yield null to skip a frame in EditMode
    [UnityTest]
    public IEnumerator PatchAppTestWithEnumeratorPasses() {
        // Use the Assert class to test conditions.
        // yield to skip a frame
        yield return null;
    }
}
