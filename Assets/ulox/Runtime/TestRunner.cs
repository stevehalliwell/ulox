using System;
using System.Collections.Generic;
using System.Text;

namespace ULox
{
    public class TestRunner
    {
        private Dictionary<string, bool> tests = new Dictionary<string, bool>();

        public void StartTest(string name) => tests[name] = false;
        public void EndTest(string name) => tests[name] = true;

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
