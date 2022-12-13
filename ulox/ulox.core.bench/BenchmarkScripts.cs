using ULox;

namespace ulox.core.bench
{
    public static class BenchmarkScripts
    {
        public static readonly Script While = new Script(nameof(While), @"
var i = 0;
var arr = [];
arr.Resize(100,0);

while(i < 100)
{
    arr[i] = i;
    i+=1;
}");

        public static readonly Script For = new Script(nameof(For), @"
var arr = [];
arr.Resize(100,0);

for(var i = 0; i < 100; i+=1)
{
    arr[i] = i;
}");

        public static readonly Script Loop = new Script(nameof(Loop), @"
var arr = [];
arr.Resize(100,0);

loop (arr)
{
    item = i;
}");

        public static readonly Script If = new Script(nameof(If), @"
var i = 0;

if(i == 1)
{
    i = 1;
}
else if (i == 2)
{
    i = 2;
}
else
{
    i = 3;
}
");

        public static readonly Script Match = new Script(nameof(Match), @"
var i = 0;
match i 
{
1: i = 1;
2: i = 2;
0: i = 3;
}");
    }
}
