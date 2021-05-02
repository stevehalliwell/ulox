using UnityEngine;

namespace ULox.Demo
{
    public class ULoxBehaviour : MonoBehaviour
    {
        [SerializeField] private TextAsset script;
        private Value _anonymousOnCollision = Value.Null();
        private Value _gameUpdateFunction = Value.Null();
        private ULoxScriptEnvironment _uLoxScriptEnvironment;

        private void Start()
        {
            _uLoxScriptEnvironment = new ULoxScriptEnvironment(
                FindObjectOfType<SharedVM>().Engine);

            BindToScript();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_anonymousOnCollision.IsNull)
                _uLoxScriptEnvironment.CallFunction(_anonymousOnCollision, 0);
        }

        private void Update()
        {
            if (!_gameUpdateFunction.IsNull)
            {
                _uLoxScriptEnvironment.SetGlobal("dt", Value.New(Time.deltaTime));
                _uLoxScriptEnvironment.CallFunction(_gameUpdateFunction, 0);
            }
        }

        private void BindToScript()
        {
            _uLoxScriptEnvironment.Run(script.text);

            _uLoxScriptEnvironment.SetGlobal("thisGameObject",Value.Object(gameObject));

            _anonymousOnCollision = _uLoxScriptEnvironment.FindFunctionWithArity("OnCollision", 0);
            _gameUpdateFunction = _uLoxScriptEnvironment.FindFunctionWithArity("Update", 0);

        }
    }
}