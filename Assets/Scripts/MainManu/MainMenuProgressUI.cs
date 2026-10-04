using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Reads mission progress for the Main Menu's ProgressPanel and displays it.
/// This is a display-only component — it does not own mission/save logic.
///
/// The project currently has no persistent mission-progress, save, or star/rating
/// system (verified by inspecting MissionData, MissionSelectManager, and all
/// gameplay scripts before writing this). The only real existing "current mission"
/// tracking is MissionSelectManager.SelectedMission, a static field set when the
/// player picks a mission in the MissionSelect scene — this script reuses that
/// rather than inventing a new one.
///
/// Total mission count is real (the MissionData assets assigned below).
/// Completed-mission count and stars are honest placeholders (0) until an actual
/// progress/save system exists — they are not faked.
/// </summary>
public class MainMenuProgressUI : MonoBehaviour
{
    [Header("Mission Data (real assets — used only for total count)")]
    [SerializeField] private MissionData[] allMissions;

    [Header("Placeholder progress (no save system exists yet)")]
    [SerializeField] private int completedMissions = 0;

    [Header("UI References")]
    [SerializeField] private Image progressFill;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI starsText;
    [SerializeField] private TextMeshProUGUI missionNameText;
    [SerializeField] private Button continueButton;

    // Matches Assets/Scenes/RocketLauncher.unity, the actual gameplay scene
    // (there is no "SampleScene" in this project's Build Settings).
    private const string GameplaySceneName = "RocketLauncher";

    private void Start()
    {
        RefreshProgress();
    }

    public void RefreshProgress()
    {
        int total = allMissions != null ? allMissions.Length : 0;
        int completed = Mathf.Clamp(completedMissions, 0, total);

        if (progressText != null)
        {
            progressText.text = $"{completed} / {total} Missions";
        }

        if (progressFill != null)
        {
            progressFill.fillAmount = total > 0 ? (float)completed / total : 0f;
        }

        if (starsText != null)
        {
            // No star/rating system exists yet — show one empty slot per mission
            // as a neutral structural placeholder, not an invented score.
            // Uses a plain ASCII asterisk rather than a Unicode star glyph, since
            // the project's UI fonts don't include the star character and were
            // rendering it as a broken-glyph box.
            starsText.text = total > 0 ? new string('*', total) : "";
        }

        // Reuse the existing MissionSelectManager.SelectedMission field rather
        // than inventing a new save/current-mission system.
        MissionData currentMission = MissionSelectManager.SelectedMission;

        if (currentMission != null)
        {
            if (missionNameText != null)
            {
                missionNameText.text = currentMission.missionName;
            }
            if (continueButton != null)
            {
                continueButton.interactable = true;
            }
        }
        else
        {
            if (missionNameText != null)
            {
                missionNameText.text = "Start your first mission";
            }
            if (continueButton != null)
            {
                continueButton.interactable = false;
            }
        }
    }

    /// <summary>Wired to the Continue button. Only does anything if a mission is
    /// already selected via the existing MissionSelectManager flow.</summary>
    public void OnContinuePressed()
    {
        if (MissionSelectManager.SelectedMission == null)
        {
            return;
        }

        SceneManager.LoadScene(GameplaySceneName);
    }
}
