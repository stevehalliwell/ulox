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
            Engine = new Engine(scriptLocator, new Context(new Program(), new VM()));

            Engine.DeclareAllLibraries(
                x => Debug.Log(x),
                prefabCollectionSO.Collection,
                x => outputText.text = x,
                () => new VM());

            if (bindAllLibraries)
                Engine.BindAllLibraries();

            foreach (var item in scriptsNamesToLoad)
            {
                Engine.LocateAndRun(item);
            }
        }
    }
}
