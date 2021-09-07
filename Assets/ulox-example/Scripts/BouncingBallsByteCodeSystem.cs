using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ULox.Demo
{
    public class BouncingBallsByteCodeSystem : MonoBehaviour
    {
        public TextAsset script;
        public Text text;
        private Value gameUpdateFunction;

        private ULoxScriptEnvironment _uLoxScriptEnvironment;

        private void Start()
        {
            _uLoxScriptEnvironment = new ULoxScriptEnvironment(
                FindObjectOfType<SharedVM>().Engine);

            _uLoxScriptEnvironment.SetGlobal("SetUIText", Value.New((vm, args) =>
            {
                text.text = vm.GetArg(1).val.asString;
                return Value.Null();
            }));

            _uLoxScriptEnvironment.Run(script.text);

            var setupFunc = _uLoxScriptEnvironment.GetGlobal("SetupGame");
            if (setupFunc.HasValue)
            {
                _uLoxScriptEnvironment.CallFunction(setupFunc.Value, 0);
            }
            var updateFunc = _uLoxScriptEnvironment.GetGlobal("Update");
            if(updateFunc.HasValue)
            {
                gameUpdateFunction = updateFunc.Value;
            }
        }

        private void Update()
        {
            if (!gameUpdateFunction.IsNull)
            {
                _uLoxScriptEnvironment.SetGlobal("dt", Value.New(Time.deltaTime));
                _uLoxScriptEnvironment.CallFunction(gameUpdateFunction, 0);
            }
        }
    }
}
