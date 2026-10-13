using UnityEngine;

/// <summary>
/// Persistent, game-wide player settings (master volume, graphics quality).
/// Values are stored in PlayerPrefs and re-applied automatically at startup,
/// so they take effect in every scene, not just the Main Menu.
/// </summary>
public static class GameSettings
{
    private const string VolumeKey = "settings.masterVolume";
    private const string QualityKey = "settings.qualityLevel";

    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(VolumeKey, 1f);
        set
        {
            value = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolumeKey, value);
            AudioListener.volume = value;
        }
    }

    public static int QualityLevel
    {
        get => PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel());
        set
        {
            value = Mathf.Clamp(value, 0, QualitySettings.names.Length - 1);
            PlayerPrefs.SetInt(QualityKey, value);
            QualitySettings.SetQualityLevel(value, true);
        }
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedSettings()
    {
        if (PlayerPrefs.HasKey(VolumeKey))
        {
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        }

        if (PlayerPrefs.HasKey(QualityKey))
        {
            int level = Mathf.Clamp(PlayerPrefs.GetInt(QualityKey), 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(level, true);
        }
    }
}
