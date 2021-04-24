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

        [SerializeField] private Dropdown dropdown;
        [SerializeField] private TextAsset[] sampleScripts;


        private SharedVM sharedVM;

        private void Start()
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(new System.Collections.Generic.List<Dropdown.OptionData> { new Dropdown.OptionData("None") });
            dropdown.AddOptions(sampleScripts.Select(x => new Dropdown.OptionData(x.name)).ToList());
            
            dropdown.onValueChanged.AddListener(DropDownChangedHandler);

            sharedVM = FindObjectOfType<SharedVM>();
            Reset();
            dropdown.value = -1;
            dropdown.value = 0;
        }

        private void DropDownChangedHandler(int arg0)
        {
            if (arg0 < 1 || arg0 > sampleScripts.Length)
                return;

            scriptInput.text = sampleScripts[arg0-1].text;
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

        public void Reset()
        {
            scriptInput.text = string.Empty;
            bytecode.text = string.Empty;
            output.text = string.Empty;
            state.text = string.Empty;
            sharedVM.Reset();
            dropdown.value = 0;
        }

        private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
        {
            output.text += condition;
        }
    }
}