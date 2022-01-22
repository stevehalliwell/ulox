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
            Engine = new Engine(new Context(scriptLocator, new Program(), new Vm()));

            DeclareAllLibraries(
                x => Debug.Log(x),
                () => new Vm());

            List<GameObject> prefabs = null;
            if (prefabCollectionSO != null)
                prefabs = prefabCollectionSO.Collection;

            Engine.Context.DeclareLibrary(new UnityLibrary(prefabs, x => outputText.text = x));

                BindAllLibraries();

            foreach (var item in scriptsNamesToLoad)
            {
                Engine.LocateAndQueue(item);
            }

            Engine.BuildAndRun();
        }

        public Value FindFunctionWithArity(HashedString name, int arity)
        {
            var vm = Engine.Context.VM;

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

        private void DeclareAllLibraries(
            Action<string> logger,
            Func<Vm> createVM)
        {
            Engine.Context.DeclareLibrary(new CoreLibrary(logger));
            Engine.Context.DeclareLibrary(new StandardClassesLibrary());
            Engine.Context.DeclareLibrary(new AssertLibrary(createVM));
            Engine.Context.DeclareLibrary(new DebugLibrary());
            Engine.Context.DeclareLibrary(new VmLibrary(createVM));
        }

        private void BindAllLibraries()
        {
            if (!bindAllLibraries) return;

            foreach (var libName in Engine.Context.LibraryNames)
            {
                Engine.Context.BindLibrary(libName);
            }
        }
    }
}
