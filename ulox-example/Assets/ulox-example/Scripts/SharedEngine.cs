using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ULox.Demo
{
    public class SharedEngine : MonoBehaviour
    {
        [SerializeField] private PrefabCollectionSO prefabCollectionSO;
        [SerializeField] private TextAssetCollectionSO textAssetCollectionSO;
        [SerializeField] private Text outputText;
        [SerializeField] private bool bindAllLibraries = false;
        [SerializeField] private List<string> scriptsNamesToLoad;

        public Engine Engine { get; private set; }

        private void Awake()
        {
            Reset();
        }

        public void Reset()
        {
            Dictionary<string, string> builtinDict = null;
            if (textAssetCollectionSO != null)
                builtinDict = textAssetCollectionSO.Collection.ToDictionary(x => x.name, x => x.text);

            var scriptLocator = new ScriptLocator(builtinDict, new DirectoryInfo(Application.streamingAssetsPath));
            Engine = new Engine(scriptLocator, new Context(new Program(), new Vm()));

            Engine.DeclareAllLibraries(
                x => Debug.Log(x),
                () => new Vm());

            List<GameObject> prefabs = null;
            if (prefabCollectionSO != null)
                prefabs = prefabCollectionSO.Collection;

            Engine.DeclareUnityLibraries(
                prefabs,
                x => outputText.text = x);

            if (bindAllLibraries)
                Engine.BindAllLibraries();

            foreach (var item in scriptsNamesToLoad)
            {
                Engine.LocateAndQueue(item);
            }

            Engine.BuildAndRun();
        }
    }

    public static partial class SharedEngineExt
    {
        public static void DeclareUnityLibraries(
            this Engine engine,
            List<UnityEngine.GameObject> availablePrefabs,
            Action<string> outputText)
        {
            engine.Context.DeclareLibrary(new UnityLibrary(availablePrefabs, outputText));
        }

        public static void DeclareAllLibraries(
            this Engine engine,
            Action<string> logger,
            Func<VMBase> createVM)
        {
            engine.Context.DeclareLibrary(new CoreLibrary(logger));
            engine.Context.DeclareLibrary(new StandardClassesLibrary());
            engine.Context.DeclareLibrary(new AssertLibrary(createVM));
            engine.Context.DeclareLibrary(new DebugLibrary());
            engine.Context.DeclareLibrary(new VmLibrary(createVM));
        }

        public static void BindAllLibraries(this Engine engine)
        {
            foreach (var libName in engine.Context.LibraryNames)
            {
                engine.Context.BindLibrary(libName);
            }
        }

        public static Value FindFunctionWithArity(this Engine engine, HashedString name, int arity)
        {
            var vm = engine.Context.VM;

            try
            {
                var globalVal = vm.GetGlobal(name);

                if (globalVal.type == ValueType.Closure &&
                    globalVal.val.asClosure.chunk.Arity == arity)
                {
                    return globalVal;
                }
            }
            catch (System.Exception)
            {
            }

            return Value.Null();
        }
    }
}
