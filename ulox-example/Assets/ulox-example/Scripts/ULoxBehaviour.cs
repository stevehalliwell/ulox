using UnityEngine;

namespace ULox.Demo
{
    public class ULoxBehaviour : MonoBehaviour
    {
        private static readonly HashedString UpdateName = new HashedString("Update");
        private static readonly HashedString SetupGameName = new HashedString("SetupGame");
        private static readonly HashedString dtName = new HashedString("dt");
        private static readonly HashedString thisGameObjectName = new HashedString("thisGameObject");
        private static readonly HashedString OnCollisionName = new HashedString("OnCollision");

        [SerializeField] private TextAsset script;
        [SerializeField] private bool useInstanceVm = true;
        private Value _anonymousOnCollision = Value.Null();
        private Value _gameUpdateFunction = Value.Null();
        private IVm _ourVM;
        private Engine _engine;

        private void Start()
        {
            _engine = FindObjectOfType<SharedEngine>().Engine;

            BindToScript();

            var setupFunc = _engine.FindFunctionWithArity(SetupGameName, 0);
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
                _ourVM.SetGlobal(dtName, Value.New(Time.deltaTime));
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

            _ourVM.SetGlobal(thisGameObjectName, Value.Object(gameObject));

            _anonymousOnCollision = _engine.FindFunctionWithArity(OnCollisionName, 0);
            _gameUpdateFunction = _engine.FindFunctionWithArity(UpdateName, 0);
        }
    }
}