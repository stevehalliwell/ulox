using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ULox.Demo
{
    public class ULoxTryItOut : MonoBehaviour
    {
        [SerializeField] private TMP_InputField scriptInput;
        [SerializeField] private Text bytecode, output, state;

        [SerializeField] private SharedVM sharedVM;

        private void Start()
        {
            scriptInput.text = string.Empty;
            bytecode.text = string.Empty;
            output.text = string.Empty;
            state.text = string.Empty;

            sharedVM = FindObjectOfType<SharedVM>();
            scriptInput.text = @"
fun fib(n)
{
    if (n < 2) return n;
    return fib(n - 2) + fib(n - 1);
}

print (fib(20));
";
            RunAndLog();
        }

        public void RunAndLog()
        {
            Application.logMessageReceived += Application_logMessageReceived;
            sharedVM.Engine.Run(scriptInput.text);
            Application.logMessageReceived -= Application_logMessageReceived;

            state.text = sharedVM.Engine.VM.GenerateGlobalsDump();
            bytecode.text = sharedVM.Engine.Disassembly;
        }

        private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
        {
            output.text += condition;
        }
    }
}