namespace ulox.core.bench
{
    public static class Looping
    {
        public const string While = @"
var i = 0;
var arr = [].Resize(100,0);

while(i < 100)
{
    arr[i] = i;
    i+=1;
}";

        public const string For = @"
var arr = [].Resize(100,0);

for(var i = 0; i < 100; i+=1)
{
    arr[i] = i;
}";

        public const string Loop = @"
var arr = [].Resize(100,0);

loop (arr)
{
    item = i;
}";
    }

    public static class Conditional
    {
        public const string If = @"
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
";

        public const string Match = @"
var i = 0;
match i 
{
1: i = 1;
2: i = 2;
0: i = 3;
}";
    }
}
