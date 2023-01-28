using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    public void To(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }
}
