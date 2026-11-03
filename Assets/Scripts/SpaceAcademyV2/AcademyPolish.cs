using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SpaceAcademyV2
{
    // Keeps the 1920x1080 layout fully visible on any aspect ratio (never cropped).
    [RequireComponent(typeof(CanvasScaler))]
    public class AcademyScalerFit : MonoBehaviour
    {
        CanvasScaler scaler; int w, h;
        void OnEnable() { scaler = GetComponent<CanvasScaler>(); Apply(); }
        void Update() { if (Screen.width != w || Screen.height != h) Apply(); }
        void Apply()
        {
            w = Screen.width; h = Screen.height;
            if (scaler == null || h <= 0) return;
            float aspect = (float)w / h;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = aspect < 1920f / 1080f ? 0f : 1f;
        }
    }

    // Soft procedural ambient loop + mute toggle (button or M key). Saved in PlayerPrefs.
    public class AcademyMusic : MonoBehaviour
    {
        public Button toggleButton;
        public TMP_Text toggleLabel;
        public float musicVolume = 0.16f;
        const string Key = "SAV2_muted";
        static float savedSfx = -1f;
        AudioSource src;
        public static bool Muted { get; private set; }

        void Awake()
        {
            if (savedSfx < 0f) savedSfx = Sfx.Volume;
            Muted = PlayerPrefs.GetInt(Key, 0) == 1;
            src = gameObject.AddComponent<AudioSource>();
            src.clip = BuildLoop(); src.loop = true; src.playOnAwake = false; src.spatialBlend = 0f;
            if (toggleButton != null) toggleButton.onClick.AddListener(Toggle);
            Apply();
            src.Play();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.M)) Toggle();
        }

        public void Toggle()
        {
            Muted = !Muted;
            PlayerPrefs.SetInt(Key, Muted ? 1 : 0); PlayerPrefs.Save();
            Apply();
        }

        void Apply()
        {
            src.volume = Muted ? 0f : musicVolume;
            Sfx.Volume = Muted ? 0f : savedSfx;
            if (toggleLabel != null) toggleLabel.text = Muted ? "SOUND OFF" : "SOUND ON";
        }

        static AudioClip BuildLoop()
        {
            const int sr = 22050; const float len = 16f;
            int n = (int)(sr * len);
            float[] d = new float[n];
            float[] notes = { 110f, 164.81f, 220f, 261.63f, 329.63f, 392f };
            float[] amps = { 0.34f, 0.22f, 0.2f, 0.14f, 0.1f, 0.06f };
            for (int v = 0; v < notes.Length; v++)
            {
                float f = Mathf.Round(notes[v] * len) / len;          // whole cycles -> seamless loop
                float trem = (1 + v % 3) / len;                        // slow shimmer, also whole cycles
                float ph = v * 1.7f;
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / sr;
                    float env = 0.65f + 0.35f * Mathf.Sin(2f * Mathf.PI * trem * t + ph);
                    d[i] += Mathf.Sin(2f * Mathf.PI * f * t) * amps[v] * env;
                }
            }
            float peak = 0.001f; for (int i = 0; i < n; i++) peak = Mathf.Max(peak, Mathf.Abs(d[i]));
            for (int i = 0; i < n; i++) d[i] /= peak * 1.15f;
            var c = AudioClip.Create("AcademyAmbient", n, 1, sr, false);
            c.SetData(d, 0);
            return c;
        }
    }
}
