using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ULox
{
    public class UnityLibrary : IULoxLibrary
    {
        private readonly List<GameObject> _availablePrefabs;
        private readonly System.Action<string> _outputText;

        public string Name => nameof(UnityLibrary);

        public UnityLibrary(
            List<GameObject> availablePrefabs,
            System.Action<string> uiTextOut)
        {
            _availablePrefabs = availablePrefabs;
            _outputText = uiTextOut;
        }

        public Table GetBindings()
            => this.GenerateBindingTable(
                (nameof(SetSpriteColour), Value.New(SetSpriteColour)),
                (nameof(SetCollisionCallback), Value.New(SetCollisionCallback)),
                (nameof(SetGameObjectTag), Value.New(SetGameObjectTag)),
                (nameof(GetGameObjectTag), Value.New(GetGameObjectTag)),
                (nameof(SetGameObjectPosition), Value.New(SetGameObjectPosition)),
                (nameof(SetGameObjectScale), Value.New(SetGameObjectScale)),
                (nameof(ReloadScene), Value.New(ReloadScene)),
                (nameof(SetUIText), Value.New(SetUIText)),
                (nameof(CreateFromPrefab), Value.New(CreateFromPrefab)),
                (nameof(RandRange), Value.New(RandRange)),
                (nameof(GetKey), Value.New(GetKey)),
                (nameof(DestroyUnityObject), Value.New(DestroyUnityObject)),
                (nameof(GetRigidBody2DFromGameObject), Value.New(GetRigidBody2DFromGameObject)),
                (nameof(SetRigidBody2DVelocity), Value.New(SetRigidBody2DVelocity))
            );

        private NativeCallResult RandRange(Vm vm, int argCount)
        {
            var min = vm.GetArg(1).val.asDouble;
            var max = vm.GetArg(2).val.asDouble;
            vm.PushReturn(Value.New(Random.Range((float)min, (float)max)));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult GetKey(Vm vm, int argCount)
        {
            var keyName = vm.GetArg(1).val.asString;
            vm.PushReturn(Value.New(Input.GetKey(keyName.String)));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult DestroyUnityObject(Vm vm, int argCount)
        {
            var go = vm.GetArg(1).val.asObject as GameObject;
            Object.Destroy(go);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult GetRigidBody2DFromGameObject(Vm vm, int argCount)
        {
            var go = vm.GetArg(1).val.asObject as GameObject;
            var rb2d = go.GetComponent<Rigidbody2D>();
            if (rb2d != null)
                vm.PushReturn(Value.Object(rb2d));

            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult SetRigidBody2DVelocity(Vm vm, int argCount)
        {
            var rb2d = vm.GetArg(1).val.asObject as Rigidbody2D;
            float x = (float)vm.GetArg(2).val.asDouble;
            float y = (float)vm.GetArg(3).val.asDouble;
            rb2d.velocity = new Vector2(x, y);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult SetUIText(Vm vm, int argCount)
        {
            _outputText?.Invoke(vm.GetArg(1).val.asString.String);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult SetSpriteColour(Vm vm, int argCount)
        {
            var go = vm.GetArg(1).val.asObject as GameObject;
            var r = vm.GetArg(2).val.asDouble;
            var g = vm.GetArg(3).val.asDouble;
            var b = vm.GetArg(4).val.asDouble;
            var a = vm.GetArg(5).val.asDouble;

            go.GetComponent<SpriteRenderer>().color = new Color((float)r, (float)g, (float)b, (float)a);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult SetCollisionCallback(Vm vm, int argCount)
        {
            var go = vm.GetArg(1).val.asObject as GameObject;
            var tagHit = vm.GetArg(2).val.asString;
            var closure = vm.GetArg(3);
            //TODO: too easy to copy or mistype the index, make it a stack

            go.GetOrAddComponent<ULoxCollisionFilter>().AddHandler(tagHit.String, () => vm.PushCallFrameAndRun(closure, 0));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult SetGameObjectTag(Vm vm, int argCount)
        {
            var go = vm.GetArg(1).val.asObject as GameObject;
            var tag = vm.GetArg(2).val.asString.String;
            go.tag = tag;
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult GetGameObjectTag(Vm vm, int argCount)
        {
            var go = vm.GetArg(1).val.asObject as GameObject;
            vm.PushReturn(Value.New(go.tag));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult SetGameObjectPosition(Vm vm, int argCount)
        {
            var go = vm.GetArg(1).val.asObject as GameObject;
            float x = (float)vm.GetArg(2).val.asDouble;
            float y = (float)vm.GetArg(3).val.asDouble;
            float z = (float)vm.GetArg(4).val.asDouble;
            go.transform.position = new Vector3(x, y, z);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult SetGameObjectScale(Vm vm, int argCount)
        {
            var go = vm.GetArg(1).val.asObject as GameObject;
            float x = (float)vm.GetArg(2).val.asDouble;
            float y = (float)vm.GetArg(3).val.asDouble;
            float z = (float)vm.GetArg(4).val.asDouble;
            go.transform.localScale = new Vector3(x, y, z);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult ReloadScene(Vm vm, int argCount)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult CreateFromPrefab(Vm vm, int argCount)
        {
            var targetName = vm.GetArg(1).val.asString.String;
            var loc = _availablePrefabs.Find(x => x.name == targetName);
            if (loc != null)
                vm.PushReturn(Value.Object(Object.Instantiate(loc)));
            else
                Debug.LogError($"Unable to find prefab of name '{targetName}'.");

            return NativeCallResult.SuccessfulExpression;
        }
    }
}

//TODO: provide a function to be called when a collision or trigger occures
