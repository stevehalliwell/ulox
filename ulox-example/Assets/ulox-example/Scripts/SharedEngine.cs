using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ULox.Demo
{
    public class SharedEngine : MonoBehaviour
    {
        [SerializeField] private PrefabCollectionSO prefabCollectionSO;
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
            var scriptLocator = new ScriptLocator(null, Application.streamingAssetsPath);
            Engine = new Engine(new Context(scriptLocator, new Program(), new Vm(), scriptLocator));

            Engine.Context.AddLibrary(new PrintLibrary(x => Debug.Log(x)));

            List<GameObject> prefabs = null;
            if (prefabCollectionSO != null)
                prefabs = prefabCollectionSO.Collection;

            Engine.Context.AddLibrary(new UnityLibrary(prefabs, x => outputText.text = x));
            
            foreach (var item in scriptsNamesToLoad)
            {
                Engine.LocateAndQueue(item);
            }

            Engine.BuildAndRun();
        }

        public Value FindFunctionWithArity(HashedString name, int arity)
        {
            var vm = Engine.Context.Vm;

            try
            {
                vm.Globals.Get(name, out var globalVal);

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
