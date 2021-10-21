using UnityEngine;

namespace ULox.Demo
{
    public class ULoxBehaviour : MonoBehaviour
    {
        [SerializeField] private TextAsset script;
        [SerializeField] private bool useInstanceVm = true;
        private Value _anonymousOnCollision = Value.Null();
        private Value _gameUpdateFunction = Value.Null();
        private IVm _ourVM;
        private Engine _engine;

        private void Start()
        {
            _engine = FindObjectOfType<SharedVM>().Engine;

            BindToScript();
            
            var setupFunc = _ourVM.FindFunctionWithArity("SetupGame",0);
            if (!setupFunc.IsNull)
            {
                _ourVM.PushCallFrameAndRun(setupFunc, 0);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_anonymousOnCollision.IsNull)
                _ourVM.PushCallFrameAndRun(_anonymousOnCollision, 0);
        }

        private void Update()
        {
            if (!_gameUpdateFunction.IsNull)
            {
                _ourVM.SetGlobal("dt", Value.New(Time.deltaTime));
                _ourVM.PushCallFrameAndRun(_gameUpdateFunction, 0);
            }
        }

        private void BindToScript()
        {
            _engine.RunScript(script.text);
            _ourVM = _engine.Context.VM;
            if (useInstanceVm)
            {
                _ourVM = new Vm();
                _ourVM.CopyFrom(_engine.Context.VM);
            }
            _ourVM.SetGlobal("thisGameObject", Value.Object(gameObject));

            _anonymousOnCollision = _ourVM.FindFunctionWithArity("OnCollision", 0);
            _gameUpdateFunction = _ourVM.FindFunctionWithArity("Update", 0);
        }
    }
}