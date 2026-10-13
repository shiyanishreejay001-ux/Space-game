using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the Settings panel: master volume slider and graphics quality stepper.
/// Reads/writes through GameSettings so values persist and apply in every scene.
/// </summary>
public class SettingsPanelUI : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TextMeshProUGUI volumeValueText;
    [SerializeField] private TextMeshProUGUI qualityValueText;

    private bool syncing;

    private void OnEnable()
    {
        Refresh();
    }

    private void Start()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        }
    }

    private void Refresh()
    {
        syncing = true;
        float volume = GameSettings.MasterVolume;
        if (volumeSlider != null) volumeSlider.value = volume;
        if (volumeValueText != null) volumeValueText.text = Mathf.RoundToInt(volume * 100f) + "%";
        UpdateQualityLabel();
        syncing = false;
    }

    private void OnVolumeChanged(float value)
    {
        if (syncing) return;
        GameSettings.MasterVolume = value;
        if (volumeValueText != null) volumeValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    public void NextQuality() => StepQuality(1);

    public void PreviousQuality() => StepQuality(-1);

    private void StepQuality(int direction)
    {
        int count = QualitySettings.names.Length;
        if (count == 0) return;
        GameSettings.QualityLevel = (GameSettings.QualityLevel + direction + count) % count;
        UpdateQualityLabel();
    }

    private void UpdateQualityLabel()
    {
        if (qualityValueText == null) return;
        string[] names = QualitySettings.names;
        int level = Mathf.Clamp(GameSettings.QualityLevel, 0, names.Length - 1);
        qualityValueText.text = names.Length > 0 ? names[level].ToUpperInvariant() : "-";
    }

    /// <summary>Wired to the Close/Back buttons so changes are flushed to disk.</summary>
    public void SaveSettings()
    {
        GameSettings.Save();
    }
}
