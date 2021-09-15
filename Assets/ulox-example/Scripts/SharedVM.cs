using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

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
            var builtinDict = textAssetCollectionSO.Collection.ToDictionary(x => x.name,x => x.text);
            var scriptLocator = new ScriptLocator(builtinDict, new DirectoryInfo(Application.streamingAssetsPath));
            var builder = new Builder();
            Engine = new Engine(scriptLocator, builder, new Context(new Program(), new VM(builder)));

            Engine.DeclareAllLibraries(
                x => Debug.Log(x),
                prefabCollectionSO.Collection,
                x => outputText.text = x,
                () => new VM(null));

            if (bindAllLibraries)
                Engine.BindAllLibraries();

            foreach (var item in scriptsNamesToLoad)
            {
                Engine.LocateAndQueue(item);
            }

            Engine.BuildAndRun();
        }
    }
}
