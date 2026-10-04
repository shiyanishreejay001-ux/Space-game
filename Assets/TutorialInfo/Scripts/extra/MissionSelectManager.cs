using UnityEngine;
using UnityEngine.SceneManagement;

public class MissionSelectManager : MonoBehaviour
{
  public static MissionData SelectedMission;

  public void StartMission(MissionData mission)
  {
    SelectedMission = mission;
    SceneManager.LoadScene("RocketLauncher");
  }
}
