using UnityEngine;
using UnityEngine.UI;

namespace ULox.Demo
{
    public class ScriptLocator
    {

    }

    public class SharedVM : MonoBehaviour
    {
        [SerializeField] private PrefabCollectionSO prefabCollectionSO;
        [SerializeField] private TextAssetCollectionSO textAssetCollectionSO;
        [SerializeField] private Text outputText;
        [SerializeField] private bool bindAllLibraries = false;

        public Engine Engine { get; private set; }

        private void Awake()
        {
            Reset();
        }

        public void Reset()
        {
            Engine = new Engine(new Context(new Program(), new VM()));

            Engine.DeclareAllLibraries(
                x => Debug.Log(x),
                prefabCollectionSO.Collection,
                x => outputText.text = x,
                () => new VM());

            if (bindAllLibraries)
                Engine.BindAllLibraries();

            if (textAssetCollectionSO != null)
            {
                foreach (var item in textAssetCollectionSO.Collection)
                {
                    Engine.RunScript(item.text);
                }
            }
        }
    }
}
