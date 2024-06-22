using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class TracingTests : EngineTestBase
    {
        [Test]
        public void VmStatisticsReport_WhenSimpleMathOps_ShouldMatchExpected()
        {
            testEngine.Run(@"
var a = 1;
var b = 2;
var c = a + b;

print(c);

var d = Math.Sqrt(c);
");
            var statsReport = VmStatisticsReport.Create(testEngine.MyEngine.Context.Vm.Tracing.PerChunkStats).GenerateStringReport();

            Assert.AreEqual("3", testEngine.InterpreterResult);
            StringAssert.Contains("ADD   1", statsReport);
            StringAssert.Contains("FETCH_GLOBAL   6", statsReport);
        }

        [Test]
        public void Tracing_WhenEnabledAndSimpleMathOps_ShouldMatchExpected()
        {
            testEngine.MyEngine.Context.Vm.Tracing.EnableTracing = true;
            testEngine.Run(@"
var a = 1;
var b = 2;
var c = a + b;

print(c);

var d = Math.Sqrt(c);
");
            var statsReport = VmTracingReport.Create(testEngine.MyEngine.Context.Vm.Tracing.TimeLineEvents).GenerateJsonTracingEventArray();

            Assert.AreEqual("3", testEngine.InterpreterResult);
            StringAssert.Contains("\"name\":\"print\",\"ph\":\"B\"", statsReport);
            StringAssert.Contains("\"name\":\"Sqrt\",\"ph\":\"E\"", statsReport);
        }

        [Test]
        public void Tracing_WhenDisabledAndSimpleMathOps_ShouldMatchExpected()
        {
            testEngine.Run(@"
var a = 1;
var b = 2;
var c = a + b;

print(c);

var d = Math.Sqrt(c);
");
            var statsReport = VmTracingReport.Create(testEngine.MyEngine.Context.Vm.Tracing.TimeLineEvents).GenerateJsonTracingEventArray();

            Assert.AreEqual("3", testEngine.InterpreterResult);
            StringAssert.DoesNotContain("\"name\":\"print\",\"ph\":\"B\"", statsReport);
            StringAssert.DoesNotContain("\"name\":\"Sqrt\",\"ph\":\"E\"", statsReport);
        }

        [Test]
        public void Tracing_WhenEnabledAndInstancesAndSimpleMathOps_ShouldMatchExpected()
        {
            testEngine.MyEngine.Context.Vm.Tracing.EnableTracing = true;
            testEngine.MyEngine.Context.Vm.Tracing.EnableOpCodeInstantTraces = true;
            testEngine.Run(@"
var a = 1;
var b = 2;
var c = a + b;

print(c);

var d = Math.Sqrt(c);
");
            var statsReport = VmTracingReport.Create(testEngine.MyEngine.Context.Vm.Tracing.TimeLineEvents).GenerateJsonTracingEventArray();

            Assert.AreEqual("3", testEngine.InterpreterResult);
            StringAssert.Contains("\"name\":\"print\",\"ph\":\"B\"", statsReport);
            StringAssert.Contains("\"name\":\"Sqrt\",\"ph\":\"E\"", statsReport);
            StringAssert.Contains("\"name\":\"FETCH_GLOBAL\",\"ph\":\"I\",\"", statsReport);
        }
    }
}
