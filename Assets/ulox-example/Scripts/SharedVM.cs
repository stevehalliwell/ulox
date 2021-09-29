using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using System;

namespace ULox.Demo
{
    public class SharedVM : MonoBehaviour
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
                builtinDict = textAssetCollectionSO.Collection.ToDictionary(x => x.name,x => x.text);

            var scriptLocator = new ScriptLocator(builtinDict, new DirectoryInfo(Application.streamingAssetsPath));
            Engine = new Engine(scriptLocator, new Context(new Program(), new Vm()));

            Engine.DeclareAllLibraries(
                x => Debug.Log(x),
                () => new Vm());

            List<GameObject> prefabs = null; 
            if(prefabCollectionSO != null)
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
    public static partial class EngineExt
    {
        public static void DeclareUnityLibraries(
            this Engine engine,
            List<UnityEngine.GameObject> availablePrefabs,
            Action<string> outputText)
        {
            engine.Context.DeclareLibrary(new UnityLibrary(availablePrefabs, outputText));
        }
    }
}
