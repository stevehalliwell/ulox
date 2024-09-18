namespace ULox.Core.Bench
{
    public class ObjectVsSoa
    {
        public const string ObjectBasedScript = @"
class Foo
{
var
    x = 0,
    y = 0,
    vx = 0,
    vy = 0,
    ;
}

var foos = [];

fun TickAllFoos(foos, dt)
{
    var count = foos.Count();
    for(var i = 0; i < count; i +=1)
    {
        var item = foos[i];
        item.x = item.x + item.vx * dt;
        item.y = item.y + item.vy * dt;
    }
}


for(var i = 0; i < 1000; i+=1)
{
    foos.Add(Foo());
}

for(var i = 0; i < 1000; i+=1)
{
    TickAllFoos(foos, 0.01);
}
";


        public const string SoaBasedScript = @"
class Foo
{
var
    x = 0,
    y = 0,
    vx = 0,
    vy = 0,
    ;
}

soa FooSet
{
    Foo
}

var foos = FooSet();

fun TickAllFoos(foos, dt)
{
    var count = foos.Count();
    var x = foos.x;
    var y = foos.y;
    var vx = foos.vx;
    var vy = foos.vy;
    for(var i = 0; i < count; i +=1)
    {
        x[i] = x[i] + vx[i] * dt;
        y[i] = y[i] + vy[i] * dt;
    }
}


for(var i = 0; i < 1000; i+=1)
{
    foos.Add(Foo());
}

for(var i = 0; i < 1000; i+=1)
{
    TickAllFoos(foos, 0.01);
}
";
    }
}
