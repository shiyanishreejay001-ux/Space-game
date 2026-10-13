using UnityEngine;

/// <summary>
/// Tiny UI sound helper. The project ships no UI audio, so the hover / click / back
/// sounds are synthesized once at runtime (short, soft blips) and played through a
/// persistent AudioSource. Respects the master volume from GameSettings
/// (AudioListener.volume). Swap in real clips later by assigning the Override* fields.
/// </summary>
public static class UISound
{
    public static AudioClip OverrideHover;
    public static AudioClip OverrideClick;
    public static AudioClip OverrideBack;

    private const int SampleRate = 44100;

    private static AudioSource source;
    private static AudioClip hoverClip;
    private static AudioClip clickClip;
    private static AudioClip backClip;

    public static void PlayHover()
    {
        if (hoverClip == null) hoverClip = BuildTone("UI_Hover", 1400f, 1550f, 0.045f, 0.22f);
        Play(OverrideHover != null ? OverrideHover : hoverClip, 0.6f);
    }

    public static void PlayClick()
    {
        if (clickClip == null) clickClip = BuildTone("UI_Click", 620f, 940f, 0.09f, 0.45f);
        Play(OverrideClick != null ? OverrideClick : clickClip, 0.8f);
    }

    public static void PlayBack()
    {
        if (backClip == null) backClip = BuildTone("UI_Back", 900f, 520f, 0.09f, 0.4f);
        Play(OverrideBack != null ? OverrideBack : backClip, 0.8f);
    }

    private static void Play(AudioClip clip, float volume)
    {
        EnsureSource();
        source.PlayOneShot(clip, volume);
    }

    private static void EnsureSource()
    {
        if (source != null) return;

        var go = new GameObject("UISound");
        Object.DontDestroyOnLoad(go);
        source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
    }

    private static AudioClip BuildTone(string clipName, float startHz, float endHz, float seconds, float amplitude)
    {
        int count = Mathf.CeilToInt(SampleRate * seconds);
        var data = new float[count];
        double phase = 0.0;
        float attackSamples = SampleRate * 0.004f;

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float hz = Mathf.Lerp(startHz, endHz, t);
            phase += 2.0 * System.Math.PI * hz / SampleRate;

            float attack = Mathf.Min(1f, i / attackSamples);
            float decay = (1f - t) * (1f - t);
            float tone = Mathf.Sin((float)phase) + 0.25f * Mathf.Sin((float)phase * 2f);

            data[i] = tone * attack * decay * amplitude;
        }

        var clip = AudioClip.Create(clipName, count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
