using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles the primary Main Menu navigation buttons (PLAY, MISSIONS, TUTORIAL, SPACE ACADEMY, SETTINGS).
/// </summary>
public class MainMenuNavigation : MonoBehaviour
{
    private const string MissionSelectScene = "MissionSelect";
    private const string SpaceAcademyScene = "SpaceAcademy";
    private const string GameplayScene = "RocketLauncher";

    [Header("PLAY")]
    [Tooltip("Mission started by the PLAY button (Mission01 = FIRST FLIGHT).")]
    [SerializeField] private MissionData firstMission;

    [Header("Panels")]
    [SerializeField] private MenuPanel settingsPanel;
    [SerializeField] private MenuPanel tutorialPanel;

    /// <summary>MISSIONS button.</summary>
    public void LoadMissionSelect()
    {
        SceneTransition.Load(MissionSelectScene);
    }

    /// <summary>SPACE ACADEMY button.</summary>
    public void LoadSpaceAcademy()
    {
        SceneTransition.Load(SpaceAcademyScene);
    }

    /// <summary>PLAY button — selects FIRST FLIGHT and jumps straight into the gameplay scene.</summary>
    public void PlayFirstMission()
    {
        if (firstMission == null)
        {
            Debug.LogWarning("[MainMenu] PLAY pressed but no first mission is assigned; opening mission select instead.");
            LoadMissionSelect();
            return;
        }

        MissionSelectManager.SelectedMission = firstMission;
        SceneTransition.Load(GameplayScene);
    }

    /// <summary>TUTORIAL button — opens the How to Play panel.</summary>
    public void OpenTutorial()
    {
        if (settingsPanel != null && settingsPanel.IsOpen) settingsPanel.Close();
        if (tutorialPanel != null) tutorialPanel.Open();
    }

    /// <summary>SETTINGS button — opens the Settings panel.</summary>
    public void OpenSettings()
    {
        if (tutorialPanel != null && tutorialPanel.IsOpen) tutorialPanel.Close();
        if (settingsPanel != null) settingsPanel.Open();
    }
}
