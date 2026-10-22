using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays distinct, meaningful learning progress for Space Academy's three modes.
/// Progress uses separate preference keys from the existing challenge completion save.
/// </summary>
public static class SpaceAcademyMastery
{
    private const string LegacyChallengeKey = "SpaceAcademy.CompletedChallenges";
    private const string ChallengeKey = "SpaceAcademy.Mastery.Challenge";
    private const string ExperimentKey = "SpaceAcademy.Mastery.Experiment";
    private const string GravityKey = "SpaceAcademy.Mastery.Gravity";
    private const int ExperimentGoal = 3;
    private const int GravityGoal = 3;
    private const float BaselineMass = 10f;
    private const float BaselineAcceleration = 3f;

    private static int previousChallengeCount;
    private static int previousExperimentCount;
    private static int previousGravityCount;
    private static bool hasShownInitialProgress;

    private sealed class ModeRow
    {
        public RectTransform root;
        public TextMeshProUGUI label;
        public TextMeshProUGUI state;
        public Image fill;
        public AcademyPanelMotion motion;
    }

    private static ModeRow challengeRow;
    private static ModeRow experimentRow;
    private static ModeRow gravityRow;

    public static void RefreshProgress(TextMeshProUGUI progressText)
    {
        MigrateExistingChallengeProgress();
        if (progressText == null) return;

        RectTransform panel = GameObject.Find("Canvas/ProgressStatusPanel")?.GetComponent<RectTransform>();
        if (panel == null) return;

        ConfigureProgressLayout(panel, progressText.rectTransform);
        EnsureMasteryUI(panel);

        int challengeCount = ReadIds(ChallengeKey).Count;
        int experimentCount = ReadIds(ExperimentKey).Count;
        int gravityCount = ReadIds(GravityKey).Count;

        UpdateRow(challengeRow, "CHALLENGE",
            challengeCount > 0 ? "SOLVED!" : "Solve 1",
            challengeCount > 0 ? 1f : 0f, challengeCount > 0, new Color(0.51f, 0.72f, 0.62f));
        UpdateRow(experimentRow, "EXPERIMENT",
            experimentCount >= ExperimentGoal ? "MASTERED!" : $"{experimentCount} / {ExperimentGoal} ideas",
            Mathf.Clamp01((float)experimentCount / ExperimentGoal),
            experimentCount >= ExperimentGoal, new Color(0.45f, 0.72f, 0.79f));
        UpdateRow(gravityRow, "GRAVITY LAB",
            gravityCount >= GravityGoal ? "MASTERED!" : $"{gravityCount} / {GravityGoal} worlds",
            Mathf.Clamp01((float)gravityCount / GravityGoal),
            gravityCount >= GravityGoal, new Color(0.83f, 0.68f, 0.43f));

        if (hasShownInitialProgress)
        {
            if (challengeCount > previousChallengeCount) Pulse(challengeRow);
            if (experimentCount > previousExperimentCount) Pulse(experimentRow);
            if (gravityCount > previousGravityCount) Pulse(gravityRow);
        }

        previousChallengeCount = challengeCount;
        previousExperimentCount = experimentCount;
        previousGravityCount = gravityCount;
        hasShownInitialProgress = true;
    }

    public static bool RecordExperimentCompletion(float mass, float acceleration)
    {
        string activity = null;
        if (Mathf.Approximately(mass, BaselineMass) && Mathf.Approximately(acceleration, BaselineAcceleration))
            activity = "baseline";
        else if (Mathf.Approximately(acceleration, BaselineAcceleration) && !Mathf.Approximately(mass, BaselineMass))
            activity = "mass_change";
        else if (Mathf.Approximately(mass, BaselineMass) && !Mathf.Approximately(acceleration, BaselineAcceleration))
            activity = "acceleration_change";

        bool recorded = activity != null && AddUnique(ExperimentKey, activity);
        if (recorded) RefreshCurrentProgress();
        return recorded;
    }

    public static bool RecordGravityLift(float gravity)
    {
        string world = Mathf.Abs(gravity - 9.81f) < 0.02f ? "earth" :
                       Mathf.Abs(gravity - 1.62f) < 0.02f ? "moon" :
                       Mathf.Abs(gravity - 3.71f) < 0.02f ? "mars" : null;
        bool recorded = world != null && AddUnique(GravityKey, world);
        if (recorded) RefreshCurrentProgress();
        return recorded;
    }

    private static void ConfigureProgressLayout(RectTransform panel, RectTransform progressText)
    {
        panel.sizeDelta = new Vector2(panel.sizeDelta.x, 320f);

        progressText.anchorMin = new Vector2(0f, 1f);
        progressText.anchorMax = new Vector2(1f, 1f);
        progressText.pivot = new Vector2(0.5f, 1f);
        progressText.anchoredPosition = new Vector2(0f, -48f);
        progressText.sizeDelta = new Vector2(-28f, 98f);
        progressText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;
        progressText.GetComponent<TextMeshProUGUI>().fontSize = 15f;
    }

    private static void EnsureMasteryUI(RectTransform panel)
    {
        Transform existing = panel.Find("ModeMasteryPanel");
        if (existing != null)
        {
            challengeRow = FindRow(existing, "Challenge");
            experimentRow = FindRow(existing, "Experiment");
            gravityRow = FindRow(existing, "Gravity");
            return;
        }

        GameObject masteryRoot = new GameObject("ModeMasteryPanel", typeof(RectTransform));
        masteryRoot.transform.SetParent(panel, false);
        RectTransform masteryRect = masteryRoot.GetComponent<RectTransform>();
        masteryRect.anchorMin = Vector2.zero;
        masteryRect.anchorMax = new Vector2(1f, 0f);
        masteryRect.pivot = new Vector2(0.5f, 0f);
        masteryRect.anchoredPosition = new Vector2(0f, 8f);
        masteryRect.sizeDelta = new Vector2(-24f, 150f);

        TextMeshProUGUI header = CreateText("MasteryHeading", masteryRect, 14f, FontStyles.Bold, TextAlignmentOptions.Left);
        SetTopRect(header.rectTransform, 0f, 128f, new Vector2(300f, 20f));
        header.text = "MODE MASTERY";
        header.color = new Color32(130, 183, 157, 255);

        challengeRow = CreateRow(masteryRect, "Challenge", "CHALLENGE", -27f);
        experimentRow = CreateRow(masteryRect, "Experiment", "EXPERIMENT", -63f);
        gravityRow = CreateRow(masteryRect, "Gravity", "GRAVITY LAB", -99f);
    }

    private static ModeRow FindRow(Transform parent, string name)
    {
        Transform root = parent.Find(name + "MasteryRow");
        if (root == null) return null;
        return new ModeRow
        {
            root = root.GetComponent<RectTransform>(),
            label = root.Find("ModeLabel").GetComponent<TextMeshProUGUI>(),
            state = root.Find("MilestoneState").GetComponent<TextMeshProUGUI>(),
            fill = root.Find("ProgressBar/ProgressFill").GetComponent<Image>(),
            motion = root.GetComponent<AcademyPanelMotion>()
        };
    }

    private static ModeRow CreateRow(Transform parent, string id, string labelText, float topOffset)
    {
        GameObject rowObject = new GameObject(id + "MasteryRow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        rowObject.transform.SetParent(parent, false);
        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        SetTopRect(rowRect, topOffset, 34f, new Vector2(300f, 34f));

        Image rowImage = rowObject.GetComponent<Image>();
        rowImage.color = new Color(0.035f, 0.07f, 0.09f, 0.28f);
        rowImage.raycastTarget = false;
        Outline outline = rowObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.28f, 0.48f, 0.5f, 0.12f);
        outline.effectDistance = new Vector2(1f, -1f);

        TextMeshProUGUI label = CreateText("ModeLabel", rowRect, 13f, FontStyles.Bold, TextAlignmentOptions.Left);
        SetTopRect(label.rectTransform, 1f, 20f, new Vector2(130f, 21f));
        label.text = labelText;
        label.color = new Color32(190, 208, 211, 255);

        TextMeshProUGUI state = CreateText("MilestoneState", rowRect, 12f, FontStyles.Bold, TextAlignmentOptions.Right);
        RectTransform stateRect = state.rectTransform;
        stateRect.anchorMin = new Vector2(1f, 1f);
        stateRect.anchorMax = new Vector2(1f, 1f);
        stateRect.pivot = new Vector2(1f, 1f);
        stateRect.anchoredPosition = new Vector2(-3f, -1f);
        stateRect.sizeDelta = new Vector2(150f, 21f);
        state.color = new Color32(156, 196, 204, 255);

        GameObject barObject = new GameObject("ProgressBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        barObject.transform.SetParent(rowRect, false);
        RectTransform barRect = barObject.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(1f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.anchoredPosition = new Vector2(0f, 3f);
        barRect.sizeDelta = new Vector2(-8f, 6f);
        Image barImage = barObject.GetComponent<Image>();
        barImage.color = new Color32(38, 58, 65, 255);
        barImage.raycastTarget = false;

        GameObject fillObject = new GameObject("ProgressFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObject.transform.SetParent(barObject.transform, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fill = fillObject.GetComponent<Image>();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;
        fill.color = new Color32(130, 183, 157, 255);
        fill.raycastTarget = false;

        AcademyPanelMotion motion = rowObject.AddComponent<AcademyPanelMotion>();
        return new ModeRow { root = rowRect, label = label, state = state, fill = fill, motion = motion };
    }

    private static TextMeshProUGUI CreateText(string objectName, Transform parent, float size, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return text;
    }

    private static void SetTopRect(RectTransform rect, float topOffset, float height, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, topOffset);
        rect.sizeDelta = size.y == 0f ? new Vector2(size.x, height) : size;
    }

    private static void UpdateRow(ModeRow row, string label, string milestone, float progress, bool complete, Color completeColor)
    {
        if (row == null) return;
        row.label.text = label;
        row.state.text = milestone;
        row.state.color = complete ? new Color32(130, 183, 157, 255) : new Color32(156, 196, 204, 255);
        row.fill.color = completeColor;
        row.fill.fillAmount = Mathf.Clamp01(progress);
    }

    private static void Pulse(ModeRow row)
    {
        if (row?.motion != null) row.motion.PlayFeedbackPulse(row.fill.color);
    }

    private static void MigrateExistingChallengeProgress()
    {
        string legacy = PlayerPrefs.GetString(LegacyChallengeKey, string.Empty);
        string[] completed = legacy.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string challengeId in completed)
            AddUnique(ChallengeKey, challengeId.Trim());
    }

    private static bool AddUnique(string key, string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        HashSet<string> ids = ReadIds(key);
        if (!ids.Add(id)) return false;

        PlayerPrefs.SetString(key, string.Join(",", ids));
        PlayerPrefs.Save();
        return true;
    }

    private static HashSet<string> ReadIds(string key)
    {
        return new HashSet<string>(
            PlayerPrefs.GetString(key, string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
            StringComparer.OrdinalIgnoreCase);
    }

    private static void RefreshCurrentProgress()
    {
        TextMeshProUGUI progress = GameObject.Find("Canvas/ProgressStatusPanel/ProgressInfo")
            ?.GetComponent<TextMeshProUGUI>();
        RefreshProgress(progress);
    }
}
