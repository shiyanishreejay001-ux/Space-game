using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles the four primary Main Menu navigation buttons (PLAY, MISSIONS, TUTORIAL, SETTINGS).
/// Only wires destinations that already exist in the project. Buttons whose destination
/// system does not exist yet are left visually/functionally prepared and simply log a notice.
/// </summary>
public class MainMenuNavigation : MonoBehaviour
{
    private const string MissionSelectScene = "MissionSelect";

    /// <summary>MISSIONS button — the MissionSelect scene already exists and is in Build Settings.</summary>
    public void LoadMissionSelect()
    {
        SceneManager.LoadScene(MissionSelectScene);
    }

    /// <summary>PLAY button — no defined "first playable mission" flow exists yet (only mission
    /// selection followed by an arbitrary chosen mission). Left unconnected rather than inventing one.</summary>
    public void NotifyPlayNotConnected()
    {
        Debug.Log("[MainMenu] PLAY pressed — no defined first-mission flow exists yet. Not connected.");
    }

    /// <summary>TUTORIAL button — no tutorial scene/system exists yet.</summary>
    public void NotifyTutorialNotConnected()
    {
        Debug.Log("[MainMenu] TUTORIAL pressed — tutorial system does not exist yet. Not connected.");
    }

    /// <summary>SETTINGS button — no settings scene/panel system exists yet.</summary>
    public void NotifySettingsNotConnected()
    {
        Debug.Log("[MainMenu] SETTINGS pressed — settings system does not exist yet. Not connected.");
    }
}
