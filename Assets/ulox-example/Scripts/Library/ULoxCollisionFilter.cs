using System;
using System.Collections.Generic;
using UnityEngine;

namespace ULox
{
    public class ULoxCollisionFilter : MonoBehaviour
    {
        private Dictionary<string, Action> _tagActions = new Dictionary<string, Action>();

        public void AddHandler(string tag, Action action)
        {
            _tagActions[tag] = action;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if(_tagActions.TryGetValue(collision.gameObject.tag, out var found))
            {
                found.Invoke();
            }
        }
    }
}