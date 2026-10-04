using UnityEngine;
using UnityEngine.SceneManagement;

public class LaunchAltitudeTrigger : MonoBehaviour
{
  [SerializeField] private string nextScene = "SpaceScene";

  private void OnTriggerEnter(Collider other)
  {
    if (other.CompareTag("Ship"))
    {
      SceneManager.LoadScene(nextScene);
    }
  }
}
