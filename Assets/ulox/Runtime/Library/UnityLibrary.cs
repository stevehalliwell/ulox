using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ULox
{
    public class UnityLibrary : IULoxLibrary
    {
        private List<GameObject> _availablePrefabs;
        private System.Action<string> _outputText;

        public string Name => nameof(UnityLibrary);

        public UnityLibrary(
            List<GameObject> availablePrefabs,
            System.Action<string> uiTextOut)
        {
            _availablePrefabs = availablePrefabs;
            _outputText = uiTextOut;
        }

        public Table GetBindings()
        {
            var resTable = new Table();

            resTable.Add(nameof(SetSpriteColour), Value.New(SetSpriteColour));
            resTable.Add(nameof(SetCollisionCallback), Value.New(SetCollisionCallback));
            resTable.Add(nameof(SetGameObjectTag), Value.New(SetGameObjectTag));
            resTable.Add(nameof(GetGameObjectTag), Value.New(GetGameObjectTag));
            resTable.Add(nameof(SetGameObjectPosition), Value.New(SetGameObjectPosition)); 
            resTable.Add(nameof(SetGameObjectScale), Value.New(SetGameObjectScale));
            resTable.Add(nameof(ReloadScene), Value.New(ReloadScene));
            resTable.Add(nameof(SetUIText), Value.New(SetUIText));
            resTable.Add(nameof(CreateFromPrefab), Value.New(CreateFromPrefab));

            resTable.Add("RandRange",
                Value.New(
                (vm, args) =>
                {
                    var min = vm.GetArg(1).val.asDouble;
                    var max = vm.GetArg(2).val.asDouble;
                    return Value.New(UnityEngine.Random.Range((float)min, (float)max));
                }));

            resTable.Add("GetKey",
                Value.New(
                (vm, args) =>
                {
                    var keyName = vm.GetArg(1).val.asString;
                    return Value.New(UnityEngine.Input.GetKey(keyName));
                }));

            resTable.Add("DestroyUnityObject",
                Value.New(
                (vm, args) =>
                {
                    var go = vm.GetArg(1).val.asObject as UnityEngine.GameObject;
                    UnityEngine.Object.Destroy(go);
                    return Value.Null();
                }));

            resTable.Add("GetRigidBody2DFromGameObject",
                Value.New(
                (vm, args) =>
                {
                    var go = vm.GetArg(1).val.asObject as UnityEngine.GameObject;
                    var rb2d = go.GetComponent<Rigidbody2D>();
                    if (rb2d != null)
                        return Value.Object(rb2d);
                    return Value.Null();
                }));

            resTable.Add("SetRigidBody2DVelocity",
                Value.New(
                (vm, args) =>
                {
                    var rb2d = vm.GetArg(1).val.asObject as UnityEngine.Rigidbody2D;
                    float x = (float)vm.GetArg(2).val.asDouble;
                    float y = (float)vm.GetArg(3).val.asDouble;
                    rb2d.velocity = new Vector2(x, y);
                    return Value.Null();
                }));
            return resTable;
        }

        private Value SetUIText(VMBase vm, int argCount)
        {
            _outputText?.Invoke(vm.GetArg(1).val.asString);
            return Value.Null();
        }

        private Value SetSpriteColour(VMBase vm, int argCount)
        {
            var go = vm.GetArg(1).val.asObject as UnityEngine.GameObject;
            var r = vm.GetArg(2).val.asDouble;
            var g = vm.GetArg(3).val.asDouble;
            var b = vm.GetArg(4).val.asDouble;
            var a = vm.GetArg(5).val.asDouble;

            go.GetComponent<SpriteRenderer>().color = new Color((float)r, (float)g, (float)b, (float)a);
            return Value.Null();
        }

        private Value SetCollisionCallback(VMBase vm, int argCount)
        {
            var go = vm.GetArg(1).val.asObject as UnityEngine.GameObject;
            var tagHit = vm.GetArg(2).val.asString;
            var closure = vm.GetArg(3);
            //TODO: too easy to copy or mistype the index, make it a stack

            go.GetOrAddComponent<ULoxCollisionFilter>().AddHandler(tagHit, () => vm.PushCallFrameAndRun(closure,0));
            return Value.Null();
        }

        private Value SetGameObjectTag(VMBase vm, int argCount)
        {
            var go = vm.GetArg(1).val.asObject as UnityEngine.GameObject;
            var tag = vm.GetArg(2).val.asString;
            go.tag = tag;
            return Value.Null();
        }

        private Value GetGameObjectTag(VMBase vm, int argCount)
        {
            var go = vm.GetArg(1).val.asObject as UnityEngine.GameObject;
            return Value.New(go.tag);
        }

        private Value SetGameObjectPosition(VMBase vm, int argCount)
        {
            var go = vm.GetArg(1).val.asObject as UnityEngine.GameObject;
            float x = (float)vm.GetArg(2).val.asDouble;
            float y = (float)vm.GetArg(3).val.asDouble;
            float z = (float)vm.GetArg(4).val.asDouble;
            go.transform.position = new UnityEngine.Vector3(x, y, z);
            return Value.Null();
        }

        private Value SetGameObjectScale(VMBase vm, int argCount)
        {
            var go = vm.GetArg(1).val.asObject as UnityEngine.GameObject;
            float x = (float)vm.GetArg(2).val.asDouble;
            float y = (float)vm.GetArg(3).val.asDouble;
            float z = (float)vm.GetArg(4).val.asDouble;
            go.transform.localScale = new UnityEngine.Vector3(x, y, z);
            return Value.Null();
        }

        private Value ReloadScene(VMBase vm, int argCount)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return Value.Null();
        }
        private Value CreateFromPrefab(VMBase vm, int argCount)
        {
            var targetName = vm.GetArg(1).val.asString;
            var loc = _availablePrefabs.Find(x => x.name == targetName);
            if (loc != null)
                return Value.Object(UnityEngine.Object.Instantiate(loc));
            else
                Debug.LogError($"Unable to find prefab of name '{targetName}'.");

            return Value.Null();
        }
    }
}

//TODO: provide a function to be called when a collision or trigger occures