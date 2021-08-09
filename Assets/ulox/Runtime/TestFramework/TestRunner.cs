using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ULox
{
    public class TestRunner
    {
        private readonly Dictionary<string, bool> tests = new Dictionary<string, bool>();
        public bool Enabled { get; set; } = true;
        public bool AllPassed => !tests.Any(x => !x.Value);
        public int TestsFound => tests.Count;
        public string CurrentTestSetName { get; set; } = string.Empty;

        public void StartTest(string name) => tests[$"{CurrentTestSetName}:{name}"] = false;
        public void EndTest(string name) => tests[$"{CurrentTestSetName}:{name}"] = true;

        public string GenerateDump()
        {
            var sb = new StringBuilder();

            foreach (var item in tests)
            {
                sb.AppendLine($"{item.Key} {(item.Value ? "Completed" : "Incomplete")}");
            }

            return sb.ToString().Trim();
        }
    }
}
