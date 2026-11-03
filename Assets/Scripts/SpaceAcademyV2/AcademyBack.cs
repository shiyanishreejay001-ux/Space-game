using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpaceAcademyV2
{
    // Return button: loads the scene named in 'sceneName' (default: main menu).
    [RequireComponent(typeof(Button))]
    public class BackButton : MonoBehaviour
    {
        public string sceneName = "main menu";

        void Awake() { GetComponent<Button>().onClick.AddListener(Go); }

        void Go()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }
}
