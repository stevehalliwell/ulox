using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ULox.Demo
{
    public class SharedVM : MonoBehaviour
    {
        [SerializeField] private List<GameObject> availablePrefabs;

        public ByteCodeInterpreterEngine Engine { get; private set; }

        private void Awake()
        {
            Reset();
        }

        public void Reset()
        {
            Engine = new ByteCodeInterpreterEngine();
            Engine.AddLibrary(new CoreLibrary(Debug.Log));
            Engine.AddLibrary(new StandardClassesLibrary());
            Engine.AddLibrary(new UnityLibrary(availablePrefabs));
            Engine.AddLibrary(new AssertLibrary(() => new VM()));
            Engine.AddLibrary(new DebugLibrary());
            Engine.AddLibrary(new VMLibrary(() => new VM()));
        }
    }
}
