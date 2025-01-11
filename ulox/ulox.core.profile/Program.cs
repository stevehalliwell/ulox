using ULox;

var scanRepeat = 10000;
var compileRepeat = 0001;
var runRepeat = 0001;
var callFuncRepeat = 00001;
var funcSetupName = "SetupGame";
var funcUpdateName = "Update";


var targetScript = ULox.Core.Bench.Vec2Variants.Tuple;

while (true)
{
    var tokenisedScript = default(TokenisedScript);
    Console.WriteLine($"Scan only {scanRepeat} times.");
    for (int i = 0; i < compileRepeat; i++)
    {
        var scanner = new Scanner();
        tokenisedScript = scanner.Scan(targetScript);
    }

    //Console.WriteLine($"Compiling only {compileRepeat} times.");
    //for (int i = 0; i < compileRepeat; i++)
    //{
    //    var engine = Engine.CreateDefault();
    //    var compiled = engine.Context.CompileScript(targetScript);
    //}

    //Console.WriteLine($"RunScript {runRepeat} times.");
    //for (int i = 0; i < runRepeat; i++)
    //{
    //    var engine = Engine.CreateDefault();
    //    engine.RunScript(targetScript);
    //}

    //Console.WriteLine($"Call {funcUpdateName} {callFuncRepeat} times.");
    //{
    //    var engine = Engine.CreateDefault();
    //    engine.RunScript(targetScript);
    //    engine.Context.Vm.Globals.Get(new HashedString(funcSetupName), out var funcStartValue);
    //    engine.Context.Vm.Globals.Get(new HashedString(funcUpdateName), out var funcUpdateValue);
    //    engine.Context.Vm.PushCallFrameAndRun(funcStartValue, 0);
    //    for (int i = 0; i < callFuncRepeat; i++)
    //    {
    //        engine.Context.Vm.PushCallFrameAndRun(funcUpdateValue, 0);
    //    }
    //}
    Console.WriteLine($"{tokenisedScript.Tokens.Count}");
}