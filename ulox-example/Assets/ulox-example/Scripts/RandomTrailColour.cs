using UnityEngine;

namespace ULox.Demo
{
    public class RandomTrailColour : MonoBehaviour
    {
        private void Start()
        {
            var trail = GetComponent<TrailRenderer>();
            trail.startColor = Random.ColorHSV(0, 1, 0.6f, 0.8f, 0.6f, 0.8f);
            trail.endColor = trail.startColor;
        }
    }
}
