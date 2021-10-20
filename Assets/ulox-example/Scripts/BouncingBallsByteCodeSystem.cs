using UnityEngine;

namespace ULox.Demo
{
    public class BouncingBallsByteCodeSystem : MonoBehaviour
    {
        public TextAsset script;
        private Value gameUpdateFunction;

        private Engine _uLoxEngine;

        private void Start()
        {
            _uLoxEngine = FindObjectOfType<SharedVM>().Engine;

            _uLoxEngine.RunScript(script.text);

            var setupFunc = _uLoxEngine.Context.VM.GetGlobal("SetupGame");
            if (!setupFunc.IsNull)
            {
                _uLoxEngine.Context.VM.PushCallFrameAndRun(setupFunc, 0);
            }
            gameUpdateFunction = _uLoxEngine.Context.VM.GetGlobal("Update");
        }

        private void Update()
        {
            if (!gameUpdateFunction.IsNull)
            {
                _uLoxEngine.Context.VM.SetGlobal("dt", Value.New(Time.deltaTime));
                _uLoxEngine.Context.VM.PushCallFrameAndRun(gameUpdateFunction, 0);
            }
        }
    }
}
