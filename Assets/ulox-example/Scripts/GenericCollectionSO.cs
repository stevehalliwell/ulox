using System.Collections.Generic;
using UnityEngine;

public class GenericCollectionSO<T> : ScriptableObject
{
    [SerializeField]
    private List<T> items = new List<T>();

    public List<T> Collection => items;
}
