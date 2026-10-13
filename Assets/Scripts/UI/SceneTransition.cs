using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Fade-through-deep-space scene transition. Call SceneTransition.Load("SceneName") instead of
/// SceneManager.LoadScene. A persistent full-screen overlay (created automatically, survives scene loads)
/// fades to a dark space colour, loads the scene asynchronously, then fades back in.
/// It also blocks clicks during the transition so buttons can't be double-pressed, and fades in
/// from black when the game first starts. Uses unscaled time, so it works while Time.timeScale == 0.
/// </summary>
public class SceneTransition : MonoBehaviour
{
    private const float FadeOutTime = 0.35f;
    private const float FadeInTime = 0.55f;
    private static readonly Color CoverColor = new Color(0.02f, 0.03f, 0.07f, 1f);

    private static SceneTransition instance;

    private CanvasGroup group;
    private bool busy;

    public static bool IsTransitioning => instance != null && instance.busy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        var t = Ensure();
        // Start covered, then reveal the first scene.
        t.group.alpha = 1f;
        t.busy = true;
        t.StartCoroutine(t.IntroRoutine());
    }

    /// <summary>Loads a scene with a fade transition.</summary>
    public static void Load(string sceneName)
    {
        var t = Ensure();
        if (t.busy) return;
        t.StartCoroutine(t.LoadRoutine(sceneName));
    }

    /// <summary>Reloads the active scene with a fade transition.</summary>
    public static void Reload()
    {
        Load(SceneManager.GetActiveScene().name);
    }

    private static SceneTransition Ensure()
    {
        if (instance != null) return instance;

        var go = new GameObject("[SceneTransition]");
        DontDestroyOnLoad(go);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        var group = go.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        var coverGo = new GameObject("Cover", typeof(RectTransform));
        coverGo.transform.SetParent(go.transform, false);
        var rt = (RectTransform)coverGo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = coverGo.AddComponent<Image>();
        img.color = CoverColor;
        img.raycastTarget = true;

        instance = go.AddComponent<SceneTransition>();
        instance.group = group;
        return instance;
    }

    private IEnumerator IntroRoutine()
    {
        group.blocksRaycasts = true;
        yield return null;
        yield return null;
        yield return Fade(0f, FadeInTime);
        group.blocksRaycasts = false;
        busy = false;
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        busy = true;
        group.blocksRaycasts = true;

        yield return Fade(1f, FadeOutTime);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError("[SceneTransition] Could not load scene '" + sceneName + "'. Is it in Build Settings?");
        }
        else
        {
            while (!op.isDone) yield return null;
            // Let the new scene's Awake/Start run before revealing it.
            yield return null;
            yield return null;
        }

        yield return Fade(0f, FadeInTime);
        group.blocksRaycasts = false;
        busy = false;
    }

    private IEnumerator Fade(float target, float duration)
    {
        float start = group.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }
        group.alpha = target;
    }
}
