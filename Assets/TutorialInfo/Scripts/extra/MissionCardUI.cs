using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MissionCardUI : MonoBehaviour
{
  [Header("Mission Data")]
  [SerializeField] private MissionData missionData;

  [Header("UI References")]
  [SerializeField] private TMP_Text missionNumberText;
  [SerializeField] private TMP_Text missionNameText;
  [SerializeField] private TMP_Text descriptionText;
  [SerializeField] private TMP_Text limitValueText;
  [SerializeField] private TMP_Text difficultyText;

  // Visual-only accent colors used to distinguish difficulty at a glance.
  // These do not change mission data or gameplay logic in any way.
  private static readonly Color DifficultyBeginner = new Color(0.55f, 0.85f, 0.55f, 1f);
  private static readonly Color DifficultyIntermediate = new Color(0.95f, 0.78f, 0.35f, 1f);
  private static readonly Color DifficultyAdvanced = new Color(0.95f, 0.45f, 0.40f, 1f);
  private static readonly Color DifficultyDefault = new Color(0.70f, 0.80f, 0.95f, 1f);

  private void Start()
  {
    DisplayMission();
  }

  public void DisplayMission()
  {
    if (missionData == null)
    {
      Debug.LogWarning("Mission Data is not assigned.");
      return;
    }

    if (missionNumberText != null)
      missionNumberText.text = $"MISSION {missionData.missionID:00}";

    if (missionNameText != null)
      missionNameText.text = missionData.missionName;

    if (descriptionText != null)
      descriptionText.text = missionData.description;

    if (limitValueText != null)
      limitValueText.text = $"{missionData.maxLandingSpeed:F0} m/s";

    if (difficultyText != null)
    {
      difficultyText.text = missionData.difficulty;
      difficultyText.color = GetDifficultyColor(missionData.difficulty);
    }
  }

  // Presentation-only helper: picks an accent color based on the existing
  // difficulty string so the card is easier to scan. Does not modify or
  // invent any mission data.
  private static Color GetDifficultyColor(string difficulty)
  {
    if (string.IsNullOrEmpty(difficulty))
      return DifficultyDefault;

    string d = difficulty.ToLowerInvariant();

    if (d.Contains("beginner") || d.Contains("easy"))
      return DifficultyBeginner;

    if (d.Contains("intermediate") || d.Contains("medium"))
      return DifficultyIntermediate;

    if (d.Contains("advanced") || d.Contains("hard"))
      return DifficultyAdvanced;

    return DifficultyDefault;
  }
}
