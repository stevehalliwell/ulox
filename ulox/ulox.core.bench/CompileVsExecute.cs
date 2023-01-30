using ULox;

namespace ulox.core.bench
{
    public static class CompileVsExecute
    {
        public static readonly Script Script = new Script(nameof(Script), @"
var a = 1;

class Foo { var b = 2, c, d = ""Hello""; }

class Bar
{
    var e = 3, f, g = ""World"";
    Meth(){retval = this.e;}
}

class FooBar
{
    mixin
        Foo,
        Bar;
}

var fb = FooBar();

expect
    fb.b == 2,
    fb.c == null,
    fb.d == ""Hello"",
    fb.e == 3,
    fb.f == null,
    fb.g == ""World"",
    fb.Meth() == fb.e;
");
    }
}
