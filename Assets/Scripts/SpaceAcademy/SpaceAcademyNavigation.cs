using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Navigation for the SpaceAcademy scene. The RETURN button calls ReturnToMainMenu().
/// </summary>
public class SpaceAcademyNavigation : MonoBehaviour
{
    private const string MainMenuScene = "main menu";

    /// <summary>RETURN button — goes back to the main menu scene.</summary>
    public void ReturnToMainMenu()
    {
        SceneTransition.Load(MainMenuScene);
    }
}
