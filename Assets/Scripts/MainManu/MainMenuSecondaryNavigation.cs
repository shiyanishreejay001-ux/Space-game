using UnityEngine;

/// <summary>
/// Handles the Main Menu secondary navigation buttons (HELP, CREDITS, EXIT).
/// Only wires destinations that already exist in the project. Buttons whose destination
/// system does not exist yet are left visually/functionally prepared and simply log a notice,
/// matching the pattern established by MainMenuNavigation.
/// </summary>
public class MainMenuSecondaryNavigation : MonoBehaviour
{
    /// <summary>HELP button — no Help scene/panel exists yet in the project.</summary>
    public void NotifyHelpNotConnected()
    {
        Debug.Log("[MainMenu] HELP pressed — help system does not exist yet. Not connected.");
    }

    /// <summary>CREDITS button — no Credits scene/panel exists yet in the project.</summary>
    public void NotifyCreditsNotConnected()
    {
        Debug.Log("[MainMenu] CREDITS pressed — credits system does not exist yet. Not connected.");
    }

    /// <summary>
    /// EXIT button — quits the built application. In the Editor this only stops Play Mode
    /// so testing never closes the Editor itself.
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("[MainMenu] EXIT pressed — stopping Play Mode (Editor). Application.Quit() will run in a real build.");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Debug.Log("[MainMenu] EXIT pressed — quitting application.");
        Application.Quit();
#endif
    }
}
