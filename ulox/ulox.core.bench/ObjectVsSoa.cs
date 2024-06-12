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
    for(var i = 0; i < count; i +=1)
    {
        foos.x[i] = foos.x[i] + foos.vx[i] * dt;
        foos.y[i] = foos.y[i] + foos.vy[i] * dt;
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
