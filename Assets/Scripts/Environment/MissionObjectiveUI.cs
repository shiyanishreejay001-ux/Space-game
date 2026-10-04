using UnityEngine;
using TMPro;

// Purely additive, read-only display: shows the selected MissionData (set by
// MissionSelectManager) so players understand what they're trying to
// achieve. Does not modify MissionSelectManager, MissionData, or any
// gameplay script - it only reads the existing static SelectedMission field.
public class MissionObjectiveUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI objectiveText;

    private void Start()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (objectiveText == null) return;

        MissionData mission = MissionSelectManager.SelectedMission;

        if (mission != null)
        {
            objectiveText.text =
                $"<b>{mission.missionName}</b>\n" +
                $"{mission.description}\n\n" +
                $"Target Height: {mission.targetHeight:F0} m\n" +
                $"Max Landing Speed: {mission.maxLandingSpeed:F1} m/s\n" +
                $"Difficulty: {mission.difficulty}";
        }
        else
        {
            objectiveText.text =
                "<b>Free Flight</b>\n" +
                "No mission selected - fly the rocket and land it safely!";
        }
    }
}
