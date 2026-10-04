using UnityEngine;

[CreateAssetMenu(
    fileName = "MissionData",
    menuName = "Orbit Logic/Mission Data"
)]
public class MissionData : ScriptableObject
{
  [Header("Mission Information")]
  public int missionID;

  public string missionName;

  [TextArea(2, 4)]
  public string description;

  [Header("Mission Rules")]
  public float maxLandingSpeed;

  [Header("Landing Zone Info")]
  public float targetHeight;
  public float horizontalDistance;
  public float gravity;

  [Header("Difficulty")]
  public string difficulty;
}
