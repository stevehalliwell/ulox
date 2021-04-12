using UnityEngine;

namespace ULox.Demo
{
    public class BouncingBallMonoBeh : MonoBehaviour
    {
        private float limit, x, y, vx, vy;

        // Start is called before the first frame update
        private void Start()
        {
            limit = 5;

            x = Random.Range(-3.0f, 3);
            y = Random.Range(-3.0f, 3);
            vx = Random.Range(-3.0f, 3);
            vy = Random.Range(-3.0f, 3);
        }

        // Update is called once per frame
        private void Update()
        {
            x += vx * Time.deltaTime;
            y += vy * Time.deltaTime;

            if (x < -limit && vx < 0) { vx *= -1; }
            if (x > limit && vx > 0) { vx *= -1; }
            if (y < -limit && vy < 0) { vy *= -1; }
            if (y > limit && vy > 0) { vy *= -1; }

            transform.position = new Vector3(x, y, 0);
        }
    }
}
