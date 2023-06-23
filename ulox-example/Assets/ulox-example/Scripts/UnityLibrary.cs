using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ULox
{
    public class UnityLibrary : IULoxLibrary
    {
        private readonly List<GameObject> _availablePrefabs;
        private readonly System.Action<string> _outputText;
        private readonly Dictionary<string, ProfilerMarker> _profilerMarkers = new Dictionary<string, ProfilerMarker>();

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
                (nameof(SetSpriteColour), Value.New(SetSpriteColour, 1, 5)),
                (nameof(SetCollisionCallback), Value.New(SetCollisionCallback, 1, 3)),
                (nameof(SetGameObjectTag), Value.New(SetGameObjectTag, 1, 2)),
                (nameof(GetGameObjectTag), Value.New(GetGameObjectTag, 1, 1)),
                (nameof(SetGameObjectPosition), Value.New(SetGameObjectPosition, 1, 4)),
                (nameof(SetGameObjectScale), Value.New(SetGameObjectScale, 1, 4)),
                (nameof(ReloadScene), Value.New(ReloadScene, 1, 0)),
                (nameof(SetUIText), Value.New(SetUIText, 1, 1)),
                (nameof(CreateFromPrefab), Value.New(CreateFromPrefab, 1, 1)),
                (nameof(RandRange), Value.New(RandRange, 1, 2)),
                (nameof(GetKey), Value.New(GetKey, 1, 1)),
                (nameof(DestroyUnityObject), Value.New(DestroyUnityObject, 1, 1)),
                (nameof(GetRigidBody2DFromGameObject), Value.New(GetRigidBody2DFromGameObject, 1, 1)),
                (nameof(SetRigidBody2DVelocity), Value.New(SetRigidBody2DVelocity, 1, 3)),
                (nameof(ProfileBegin), Value.New(ProfileBegin, 1, 1)),
                (nameof(ProfileEnd), Value.New(ProfileEnd, 1, 1)),
                (nameof(SetListOfGoToListOfPositions), Value.New(SetListOfGoToListOfPositions, 1, 4))
            );

        private NativeCallResult RandRange(Vm vm)
        {
            var min = vm.GetArg(1).val.asDouble;
            var max = vm.GetArg(2).val.asDouble;
            vm.SetNativeReturn(0, Value.New(Random.Range((float)min, (float)max)));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult GetKey(Vm vm)
        {
            var keyName = vm.GetArg(1).val.asString;
            vm.SetNativeReturn(0, Value.New(Input.GetKey(keyName.String)));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult DestroyUnityObject(Vm vm)
        {
            var go = vm.GetArg(1).val.asObject as GameObject;
            Object.Destroy(go);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult GetRigidBody2DFromGameObject(Vm vm)
        {
            var go = vm.GetArg(1).val.asObject as GameObject;
            var rb2d = go.GetComponent<Rigidbody2D>();
            if (rb2d != null)
                vm.SetNativeReturn(0, Value.Object(rb2d));

            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult SetRigidBody2DVelocity(Vm vm)
        {
            var rb2d = vm.GetArg(1).val.asObject as Rigidbody2D;
            float x = (float)vm.GetArg(2).val.asDouble;
            float y = (float)vm.GetArg(3).val.asDouble;
            rb2d.velocity = new Vector2(x, y);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult SetUIText(Vm vm)
        {
            _outputText?.Invoke(vm.GetArg(1).val.asString.String);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult SetSpriteColour(Vm vm)
        {
            var go = vm.GetArg(1).val.asObject as GameObject;
            var r = vm.GetArg(2).val.asDouble;
            var g = vm.GetArg(3).val.asDouble;
            var b = vm.GetArg(4).val.asDouble;
            var a = vm.GetArg(5).val.asDouble;

            go.GetComponent<SpriteRenderer>().color = new Color((float)r, (float)g, (float)b, (float)a);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult SetCollisionCallback(Vm vm)
        {
            var go = vm.GetArg(1).val.asObject as GameObject;
            var tagHit = vm.GetArg(2).val.asString;
            var closure = vm.GetArg(3);
            //TODO: too easy to copy or mistype the index, make it a stack

            go.GetOrAddComponent<ULoxCollisionFilter>().AddHandler(tagHit.String, () => vm.PushCallFrameAndRun(closure, 0));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult SetGameObjectTag(Vm vm)
        {
            var go = vm.GetArg(1).val.asObject as GameObject;
            var tag = vm.GetArg(2).val.asString.String;
            go.tag = tag;
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult GetGameObjectTag(Vm vm)
        {
            var go = vm.GetArg(1).val.asObject as GameObject;
            vm.SetNativeReturn(0, Value.New(go.tag));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult SetGameObjectPosition(Vm vm)
        {
            var go = vm.GetArg(1).val.asObject as GameObject;
            float x = (float)vm.GetArg(2).val.asDouble;
            float y = (float)vm.GetArg(3).val.asDouble;
            float z = (float)vm.GetArg(4).val.asDouble;
            go.transform.position = new Vector3(x, y, z);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult SetListOfGoToListOfPositions(Vm vm)
        {
            var gos = (vm.GetArg(1).val.asObject as NativeListInstance).List;
            var pos2ds = (vm.GetArg(2).val.asObject as NativeListInstance).List;
            var len = gos.Count;
            var xField = vm.GetArg(3).val.asString;
            var yField = vm.GetArg(4).val.asString;

            for (var i = 0; i < len; i++)
            {
                var posObj = pos2ds[i].val.asInstance;
                posObj.Fields.Get(xField, out var xVal);
                posObj.Fields.Get(yField, out var yVal);

                var pos = new Vector2((float)xVal.val.asDouble, (float)yVal.val.asDouble);
                (gos[i].val.asObject as GameObject).transform.position = pos;
            }

            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult SetGameObjectScale(Vm vm)
        {
            var go = vm.GetArg(1).val.asObject as GameObject;
            float x = (float)vm.GetArg(2).val.asDouble;
            float y = (float)vm.GetArg(3).val.asDouble;
            float z = (float)vm.GetArg(4).val.asDouble;
            go.transform.localScale = new Vector3(x, y, z);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult ReloadScene(Vm vm)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult CreateFromPrefab(Vm vm)
        {
            var targetName = vm.GetArg(1).val.asString.String;
            var loc = _availablePrefabs.Find(x => x.name == targetName);
            if (loc != null)
                vm.SetNativeReturn(0, Value.Object(Object.Instantiate(loc)));
            else
                Debug.LogError($"Unable to find prefab of name '{targetName}'.");

            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult ProfileBegin(Vm vm)
        {
            var name = vm.GetArg(1).val.asString.String;
            if (!_profilerMarkers.TryGetValue(name, out var marker))
            {
                marker = new ProfilerMarker(name);
                _profilerMarkers[name] = marker;
            }
            marker.Begin();
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult ProfileEnd(Vm vm)
        {
            var name = vm.GetArg(1).val.asString.String;
            _profilerMarkers[name].End();
            return NativeCallResult.SuccessfulExpression;
        }
    }
}

//TODO: provide a function to be called when a collision or trigger occures
