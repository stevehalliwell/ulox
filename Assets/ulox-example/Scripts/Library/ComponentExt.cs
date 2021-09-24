using UnityEngine;

namespace ULox
{
    public static class ComponentExt
    {
        public static T GetOrAddComponent<T>(this GameObject go)
            where T : Component
        {
            var existing = go.GetComponent<T>();
            if (existing != null)
                return existing;

            return go.AddComponent<T>();
        }
    }
}