using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ULox.Demo
{
    public class BouncingBallsMonoSystem : MonoBehaviour
    {
        private const float numBallsToSpawn = 100;
        private const float limit = 5;
        private List<GameObject> balls = new List<GameObject>();
        private List<Vector3> vels = new List<Vector3>();
        [SerializeField] private GameObject ballPrefab;

        private void Start()
        {
            for (var i = 0; i < numBallsToSpawn; i += 1)
            {
                balls.Add(Instantiate(ballPrefab));
                balls.Last().transform.position = new Vector2(Random.Range(-3.0f, 3), Random.Range(-3.0f, 3));
                vels.Add(new Vector2(Random.Range(-3.0f, 3), Random.Range(-3.0f, 3)));
            }
        }

        // Update is called once per frame
        private void Update()
        {
            for (int i = 0; i < balls.Count; i++)
            {
                balls[i].transform.position += vels[i] * Time.deltaTime;

                var pos = balls[i].transform.position;

                if (pos.x < -limit && vels[i].x < 0) vels[i] = new Vector3(vels[i].x * -1, vels[i].y, 0);
                if (pos.x > limit && vels[i].x > 0) vels[i] = new Vector3(vels[i].x * -1, vels[i].y, 0);
                if (pos.y < -limit && vels[i].y < 0) vels[i] = new Vector3(vels[i].x, vels[i].y * -1, 0);
                if (pos.y > limit && vels[i].y > 0) vels[i] = new Vector3(vels[i].x, vels[i].y * -1, 0);
            }
        }
    }
}
