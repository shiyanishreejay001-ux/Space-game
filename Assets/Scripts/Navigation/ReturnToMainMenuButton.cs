using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Put this on any UI Button to make it load the main menu scene.
/// It wires its own Button.onClick at runtime, so no manual OnClick setup is needed.
/// </summary>
[DisallowMultipleComponent]
public class ReturnToMainMenuButton : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "main menu";

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveListener(ReturnToMainMenu);
            button.onClick.AddListener(ReturnToMainMenu);
        }
        else
        {
            Debug.LogWarning("[ReturnToMainMenuButton] No Button component on " + name, this);
        }
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(ReturnToMainMenu);
    }

    public void ReturnToMainMenu()
    {
        // Make sure time is running again in case a pause menu froze it.
        Time.timeScale = 1f;
        SceneTransition.Load(mainMenuSceneName);
    }
}
