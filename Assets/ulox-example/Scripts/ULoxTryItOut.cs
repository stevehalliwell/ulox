using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;

namespace ULox.Demo
{
    public class ULoxTryItOut : MonoBehaviour
    {
        [SerializeField] private TMP_InputField scriptInput;
        [SerializeField] private Text bytecode, output, state;

        [SerializeField] private Dropdown dropdown, testDropdown;
        [SerializeField] private TextAsset[] sampleScripts, testScripts;


        private SharedVM sharedVM;

        private void Start()
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(new System.Collections.Generic.List<Dropdown.OptionData> { new Dropdown.OptionData("None") });
            dropdown.AddOptions(sampleScripts.Select(x => new Dropdown.OptionData(x.name)).ToList());
            dropdown.onValueChanged.AddListener(SampleDropDownChangedHandler);

            
            testDropdown.ClearOptions();
            testDropdown.AddOptions(new System.Collections.Generic.List<Dropdown.OptionData> { new Dropdown.OptionData("None") });
            testDropdown.AddOptions(testScripts.Select(x => new Dropdown.OptionData(x.name)).ToList());
            testDropdown.onValueChanged.AddListener(TestDropDownChangedHandler);

            sharedVM = FindObjectOfType<SharedVM>();
            Reset();
        }

        private void SampleDropDownChangedHandler(int arg0)
        {
            if (arg0 < 1 || arg0 > sampleScripts.Length)
                return;
            Reset();
            dropdown.SetValueWithoutNotify(arg0);
            scriptInput.text = sampleScripts[arg0-1].text;
            RunAndLog();
        }

        private void TestDropDownChangedHandler(int arg0)
        {
            if (arg0 < 1 || arg0 > testScripts.Length)
                return;
            Reset();
            testDropdown.SetValueWithoutNotify(arg0);
            scriptInput.text = testScripts[arg0 - 1].text;
            RunAndLog();
        }

        public void RunAndLog()
        {
            Application.logMessageReceived += Application_logMessageReceived;
            try
            {
                sharedVM.Engine.RunScript(scriptInput.text);
            }
            catch (Exception)
            {
            }
            Application.logMessageReceived -= Application_logMessageReceived;

            state.text = sharedVM.Engine.Context.VM.GenerateGlobalsDump();
            bytecode.text = sharedVM.Engine.Context.Program.Disassembly;
            output.text += "\n";
        }

        public void Reset()
        {
            scriptInput.text = string.Empty;
            bytecode.text = string.Empty;
            output.text = string.Empty;
            state.text = string.Empty;
            sharedVM.Reset();
            dropdown.value = -1;
            dropdown.value = 0;
        }

        private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
        {
            output.text += condition;
        }
    }
}