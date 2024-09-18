namespace ULox.Core.Bench
{
    public static class CompileVsExecute
    {
        public static readonly Script Script = new(nameof(Script), @"
var a = 1;

class Foo { var b = 2, c, d = ""Hello""; }

class Bar
{
    var e = 3, f, g = ""World"", superNull;
    Meth(){retval = this.e;}
}

class FooBar
{
    mixin
        Foo,
        Bar;

    init(c,f){}
}

var fb = FooBar(7,8);

expect
    fb.b == 2,
    fb.c == 7,
    fb.d == ""Hello"",
    fb.e == 3,
    fb.f == 8,
    fb.g == ""World"",
    fb.Meth() == fb.e,
    fb.superNull == null;
");
    }
}
