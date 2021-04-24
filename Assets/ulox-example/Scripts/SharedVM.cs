using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ULox.Demo
{
    public class SharedVM : MonoBehaviour
    {
        [SerializeField] private List<GameObject> availablePrefabs;

        public ByteCodeInterpreterEngine Engine { get; private set; } = new ByteCodeInterpreterEngine();

        private void Awake()
        {
            Engine.AddLibrary(new CoreLibrary(Debug.Log));
            Engine.AddLibrary(new StandardClassesLibrary());
            Engine.AddLibrary(new UnityLibrary(availablePrefabs));
        }
    }
}
