﻿using System.Collections.Generic;
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

        private Value RandRange(VMBase vm, int argCount)
        {
            var min = vm.GetArg(1).val.asDouble;
            var max = vm.GetArg(2).val.asDouble;
            return Value.New(Random.Range((float)min, (float)max));
        }

        private Value GetKey(VMBase vm, int argCount)
        {
            var keyName = vm.GetArg(1).val.asString;
            return Value.New(Input.GetKey(keyName.String));
        }

        private Value DestroyUnityObject(VMBase vm, int argCount)
        {
            var go = vm.GetArg(1).val.asObject as GameObject;
            Object.Destroy(go);
            return Value.Null();
        }

        private Value GetRigidBody2DFromGameObject(VMBase vm, int argCount)
        {
            var go = vm.GetArg(1).val.asObject as GameObject;
            var rb2d = go.GetComponent<Rigidbody2D>();
            if (rb2d != null)
                return Value.Object(rb2d);
            return Value.Null();
        }

        private Value SetRigidBody2DVelocity(VMBase vm, int argCount)
        {
            var rb2d = vm.GetArg(1).val.asObject as Rigidbody2D;
            float x = (float)vm.GetArg(2).val.asDouble;
            float y = (float)vm.GetArg(3).val.asDouble;
            rb2d.velocity = new Vector2(x, y);
            return Value.Null();
        }

        private Value SetUIText(VMBase vm, int argCount)
        {
            _outputText?.Invoke(vm.GetArg(1).val.asString.String);
            return Value.Null();
        }

        private Value SetSpriteColour(VMBase vm, int argCount)
        {
            var go = vm.GetArg(1).val.asObject as GameObject;
            var r = vm.GetArg(2).val.asDouble;
            var g = vm.GetArg(3).val.asDouble;
            var b = vm.GetArg(4).val.asDouble;
            var a = vm.GetArg(5).val.asDouble;

            go.GetComponent<SpriteRenderer>().color = new Color((float)r, (float)g, (float)b, (float)a);
            return Value.Null();
        }

        private Value SetCollisionCallback(VMBase vm, int argCount)
        {
            var go = vm.GetArg(1).val.asObject as GameObject;
            var tagHit = vm.GetArg(2).val.asString;
            var closure = vm.GetArg(3);
            //TODO: too easy to copy or mistype the index, make it a stack

            go.GetOrAddComponent<ULoxCollisionFilter>().AddHandler(tagHit.String, () => vm.PushCallFrameAndRun(closure, 0));
            return Value.Null();
        }

        private Value SetGameObjectTag(VMBase vm, int argCount)
        {
            var go = vm.GetArg(1).val.asObject as GameObject;
            var tag = vm.GetArg(2).val.asString.String;
            go.tag = tag;
            return Value.Null();
        }

        private Value GetGameObjectTag(VMBase vm, int argCount)
        {
            var go = vm.GetArg(1).val.asObject as GameObject;
            return Value.New(go.tag);
        }

        private Value SetGameObjectPosition(VMBase vm, int argCount)
        {
            var go = vm.GetArg(1).val.asObject as GameObject;
            float x = (float)vm.GetArg(2).val.asDouble;
            float y = (float)vm.GetArg(3).val.asDouble;
            float z = (float)vm.GetArg(4).val.asDouble;
            go.transform.position = new Vector3(x, y, z);
            return Value.Null();
        }

        private Value SetGameObjectScale(VMBase vm, int argCount)
        {
            var go = vm.GetArg(1).val.asObject as GameObject;
            float x = (float)vm.GetArg(2).val.asDouble;
            float y = (float)vm.GetArg(3).val.asDouble;
            float z = (float)vm.GetArg(4).val.asDouble;
            go.transform.localScale = new Vector3(x, y, z);
            return Value.Null();
        }

        private Value ReloadScene(VMBase vm, int argCount)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return Value.Null();
        }

        private Value CreateFromPrefab(VMBase vm, int argCount)
        {
            var targetName = vm.GetArg(1).val.asString.String;
            var loc = _availablePrefabs.Find(x => x.name == targetName);
            if (loc != null)
                return Value.Object(Object.Instantiate(loc));
            else
                Debug.LogError($"Unable to find prefab of name '{targetName}'.");

            return Value.Null();
        }
    }
}

//TODO: provide a function to be called when a collision or trigger occures