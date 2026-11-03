using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpaceAcademyV2
{
    // ---------------------------------------------------------------------
    // Procedural sprites (circle, ring, square, triangle, gradient)
    // ---------------------------------------------------------------------
    public static class Art
    {
        static Sprite circle, ring, square, tri, grad;

        static Sprite Make(Texture2D t)
        {
            t.hideFlags = HideFlags.HideAndDontSave;
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            t.Apply();
            var s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
            s.hideFlags = HideFlags.HideAndDontSave;
            return s;
        }

        public static Sprite Square
        {
            get
            {
                if (square == null)
                {
                    var t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                    for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++) t.SetPixel(x, y, Color.white);
                    square = Make(t);
                }
                return square;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (circle == null)
                {
                    int n = 64;
                    var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                    for (int y = 0; y < n; y++)
                        for (int x = 0; x < n; x++)
                        {
                            float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                            float d = Mathf.Sqrt(dx * dx + dy * dy);
                            float a = Mathf.Clamp01((1f - d) * 4f);
                            t.SetPixel(x, y, new Color(1, 1, 1, a));
                        }
                    circle = Make(t);
                }
                return circle;
            }
        }

        public static Sprite Ring
        {
            get
            {
                if (ring == null)
                {
                    int n = 128;
                    var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                    for (int y = 0; y < n; y++)
                        for (int x = 0; x < n; x++)
                        {
                            float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                            float d = Mathf.Sqrt(dx * dx + dy * dy);
                            float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.85f) / 0.1f);
                            t.SetPixel(x, y, new Color(1, 1, 1, a));
                        }
                    ring = Make(t);
                }
                return ring;
            }
        }

        // Triangle pointing LEFT.
        public static Sprite Triangle
        {
            get
            {
                if (tri == null)
                {
                    int n = 64;
                    var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                    for (int y = 0; y < n; y++)
                        for (int x = 0; x < n; x++)
                        {
                            float fx = (x + 0.5f) / n, fy = (y + 0.5f) / n;
                            float half = fx * 0.5f; // grows to the right
                            bool inside = Mathf.Abs(fy - 0.5f) <= half;
                            t.SetPixel(x, y, new Color(1, 1, 1, inside ? 1f : 0f));
                        }
                    tri = Make(t);
                }
                return tri;
            }
        }

        // White with alpha 0 at bottom -> 1 at top (tint it black to darken the top).
        public static Sprite GradientAlpha
        {
            get
            {
                if (grad == null)
                {
                    var t = new Texture2D(4, 64, TextureFormat.RGBA32, false);
                    for (int y = 0; y < 64; y++)
                        for (int x = 0; x < 4; x++)
                            t.SetPixel(x, y, new Color(1, 1, 1, y / 63f));
                    grad = Make(t);
                }
                return grad;
            }
        }
    }

    // ---------------------------------------------------------------------
    // Procedural sound effects (no audio files needed)
    // ---------------------------------------------------------------------
    public enum Snd { Click, Hover, Tab, Tick, Correct, Wrong, Fanfare, Beep, Go, Thud, Motor, Boom, Reveal }

    public static class Sfx
    {
        static AudioSource src, loop;
        static readonly Dictionary<Snd, AudioClip> clips = new Dictionary<Snd, AudioClip>();
        static AudioClip engineClip;
        public static float Volume = 0.55f;
        const int SR = 44100;

        static void Ensure()
        {
            if (src != null && loop != null) return;
            var go = new GameObject("SAV2_Audio");
            go.hideFlags = HideFlags.HideAndDontSave;
            src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            loop = go.AddComponent<AudioSource>();
            loop.playOnAwake = false;
            loop.loop = true;
            loop.spatialBlend = 0f;
        }

        static AudioClip Tone(string name, float[] freqs, float segDur, float vol, int wave, float decay)
        {
            int segSamples = (int)(segDur * SR);
            int total = segSamples * freqs.Length;
            var data = new float[total];
            double phase = 0;
            for (int s = 0; s < freqs.Length; s++)
            {
                for (int i = 0; i < segSamples; i++)
                {
                    float t = i / (float)segSamples;
                    phase += 2.0 * Math.PI * freqs[s] / SR;
                    float v;
                    if (wave == 0) v = (float)Math.Sin(phase);
                    else if (wave == 1) v = (float)(((phase / (2 * Math.PI)) % 1.0) * 2.0 - 1.0);
                    else v = Math.Sign(Math.Sin(phase)) * 0.6f;
                    float env = Mathf.Clamp01(t * 20f) * Mathf.Exp(-decay * t);
                    data[s * segSamples + i] = v * env * vol;
                }
            }
            var c = AudioClip.Create(name, total, 1, SR, false);
            c.SetData(data, 0);
            return c;
        }

        static AudioClip Get(Snd s)
        {
            AudioClip c;
            if (clips.TryGetValue(s, out c) && c != null) return c;
            switch (s)
            {
                case Snd.Click: c = Tone("click", new[] { 900f, 1300f }, 0.035f, 0.5f, 0, 3f); break;
                case Snd.Hover: c = Tone("hover", new[] { 1500f }, 0.03f, 0.18f, 0, 3f); break;
                case Snd.Tab: c = Tone("tab", new[] { 520f, 780f, 1040f }, 0.045f, 0.45f, 0, 2f); break;
                case Snd.Tick: c = Tone("tick", new[] { 1100f }, 0.02f, 0.25f, 0, 2f); break;
                case Snd.Correct: c = Tone("correct", new[] { 523f, 659f, 784f, 1047f }, 0.09f, 0.5f, 0, 2.5f); break;
                case Snd.Wrong: c = Tone("wrong", new[] { 180f, 140f, 110f }, 0.13f, 0.45f, 2, 1.5f); break;
                case Snd.Fanfare: c = Tone("fanfare", new[] { 523f, 659f, 784f, 659f, 784f, 1047f, 1319f }, 0.12f, 0.5f, 0, 1.8f); break;
                case Snd.Beep: c = Tone("beep", new[] { 880f }, 0.18f, 0.5f, 0, 2f); break;
                case Snd.Go: c = Tone("go", new[] { 1320f }, 0.45f, 0.55f, 0, 2.5f); break;
                case Snd.Thud: c = Tone("thud", new[] { 90f, 70f, 55f }, 0.07f, 0.8f, 0, 3f); break;
                case Snd.Motor: c = Tone("motor", new[] { 240f, 280f, 320f, 360f }, 0.1f, 0.3f, 1, 1f); break;
                case Snd.Reveal: c = Tone("reveal", new[] { 440f, 554f, 659f }, 0.07f, 0.35f, 0, 2f); break;
                default: c = Tone("boom", new[] { 70f, 55f, 45f, 38f }, 0.18f, 0.9f, 1, 2f); break;
            }
            clips[s] = c;
            return c;
        }

        public static void Play(Snd s, float vol = 1f)
        {
            if (!Application.isPlaying) return;
            Ensure();
            src.PlayOneShot(Get(s), Volume * vol);
        }

        public static void StartEngine()
        {
            if (!Application.isPlaying) return;
            Ensure();
            if (engineClip == null)
            {
                int n = SR; // 1 second loop
                var d = new float[n];
                var rnd = new System.Random(7);
                float lp = 0f;
                for (int i = 0; i < n; i++)
                {
                    float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                    lp += (noise - lp) * 0.06f;
                    float rumble = Mathf.Sin(2f * Mathf.PI * 55f * i / SR) * 0.5f + Mathf.Sin(2f * Mathf.PI * 83f * i / SR) * 0.25f;
                    d[i] = (lp * 2.2f + rumble) * 0.35f;
                }
                engineClip = AudioClip.Create("engine", n, 1, SR, false);
                engineClip.SetData(d, 0);
            }
            loop.clip = engineClip;
            loop.volume = Volume * 0.8f;
            if (!loop.isPlaying) loop.Play();
        }

        public static void StopEngine()
        {
            if (loop != null && loop.isPlaying) loop.Stop();
        }
    }

    // ---------------------------------------------------------------------
    // Progress (stars, XP, unlocks) saved with PlayerPrefs
    // ---------------------------------------------------------------------
    public static class Progress
    {
        public const int Levels = 16;
        public static event Action Changed;

        public static int GetStars(int level) { return PlayerPrefs.GetInt("SAV2_stars_" + level, 0); }
        public static int XP { get { return PlayerPrefs.GetInt("SAV2_xp", 0); } }
        public static bool Unlocked(int level) { return level <= 0 || GetStars(level - 1) > 0; }

        public static int TotalStars
        {
            get { int s = 0; for (int i = 0; i < Levels; i++) s += GetStars(i); return s; }
        }

        public static bool AllDone
        {
            get { for (int i = 0; i < Levels; i++) if (GetStars(i) <= 0) return false; return true; }
        }

        public static int FirstOpenLevel()
        {
            for (int i = 0; i < Levels; i++) if (GetStars(i) <= 0) return i;
            return 0;
        }

        // Returns XP gained (only when stars improve).
        public static int Record(int level, int stars)
        {
            int old = GetStars(level);
            int gain = 0;
            if (stars > old)
            {
                gain = (stars - old) * 40;
                PlayerPrefs.SetInt("SAV2_stars_" + level, stars);
                PlayerPrefs.SetInt("SAV2_xp", XP + gain);
                PlayerPrefs.Save();
            }
            if (Changed != null) Changed();
            return gain;
        }

        public static bool AddXPOnce(string key, int amount)
        {
            if (PlayerPrefs.GetInt("SAV2_once_" + key, 0) == 1) return false;
            PlayerPrefs.SetInt("SAV2_once_" + key, 1);
            PlayerPrefs.SetInt("SAV2_xp", XP + amount);
            PlayerPrefs.Save();
            if (Changed != null) Changed();
            return true;
        }

        public static string Rank
        {
            get
            {
                int xp = XP;
                if (xp >= 700) return "COMMANDER";
                if (xp >= 400) return "NAVIGATOR";
                if (xp >= 150) return "PILOT";
                return "CADET";
            }
        }

        public static readonly int[] RankXP = { 0, 150, 400, 700 };
        public static readonly string[] RankNames = { "CADET", "PILOT", "NAVIGATOR", "COMMANDER" };

        public static int RankIndex
        {
            get
            {
                int xp = XP, r = 0;
                for (int i = 0; i < RankXP.Length; i++) if (xp >= RankXP[i]) r = i;
                return r;
            }
        }

        public static void ResetAll()
        {
            for (int i = 0; i < Levels; i++) PlayerPrefs.DeleteKey("SAV2_stars_" + i);
            PlayerPrefs.DeleteKey("SAV2_xp");
            PlayerPrefs.DeleteKey("SAV2_once_firstlift");
            PlayerPrefs.DeleteKey("SAV2_skin"); PlayerPrefs.DeleteKey("SAV2_rankseen");
            for (int i = 0; i < 8; i++) PlayerPrefs.DeleteKey("SAV2_skinU_" + i);
            PlayerPrefs.Save();
            if (Changed != null) Changed();
        }
    }

    // ---------------------------------------------------------------------
    // Small tween / effect helpers (all use unscaled time)
    // ---------------------------------------------------------------------
    public static class Fx
    {
        public static IEnumerator Run(float dur, Action<float> step)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                step(Mathf.Clamp01(t / dur));
                yield return null;
            }
            step(1f);
        }

        public static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        public static float EaseOutCubic(float x) { return 1f - Mathf.Pow(1f - x, 3f); }

        public static IEnumerator Pop(Transform t, float from = 0.7f, float dur = 0.35f)
        {
            Vector3 baseScale = Vector3.one;
            yield return Run(dur, k => { if (t != null) t.localScale = baseScale * Mathf.LerpUnclamped(from, 1f, EaseOutBack(k)); });
        }

        public static IEnumerator Shake(RectTransform rt, float dur = 0.4f, float amp = 14f)
        {
            if (rt == null) yield break;
            Vector2 start = rt.anchoredPosition;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - t / dur;
                if (rt != null) rt.anchoredPosition = start + new Vector2(Mathf.Sin(t * 70f) * amp * k, 0f);
                yield return null;
            }
            if (rt != null) rt.anchoredPosition = start;
        }

        public static IEnumerator Fade(CanvasGroup g, float to, float dur)
        {
            if (g == null) yield break;
            float from = g.alpha;
            yield return Run(dur, k => { if (g != null) g.alpha = Mathf.Lerp(from, to, k); });
        }

        public static IEnumerator Burst(RectTransform parent, Vector2 pos, int count, bool confetti)
        {
            var items = new List<RectTransform>();
            var vel = new List<Vector2>();
            var imgs = new List<Image>();
            Color[] cols = { new Color(0.2f, 0.85f, 1f), new Color(1f, 0.62f, 0.1f), new Color(0.3f, 0.95f, 0.5f), new Color(1f, 0.9f, 0.3f), new Color(0.9f, 0.4f, 1f) };
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("fx", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = pos;
                float s = confetti ? UnityEngine.Random.Range(10f, 20f) : UnityEngine.Random.Range(8f, 16f);
                rt.sizeDelta = new Vector2(s, confetti ? s * 1.6f : s);
                var im = go.GetComponent<Image>();
                im.sprite = confetti ? Art.Square : Art.Circle;
                im.color = cols[UnityEngine.Random.Range(0, cols.Length)];
                im.raycastTarget = false;
                float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float spd = UnityEngine.Random.Range(250f, 900f);
                items.Add(rt); imgs.Add(im);
                vel.Add(new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd);
                rt.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0f, 360f));
            }
            float t = 0f, life = 1.6f;
            while (t < life)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] == null) continue;
                    var v = vel[i];
                    v.y -= 1200f * dt;
                    v *= (1f - 1.2f * dt);
                    vel[i] = v;
                    items[i].anchoredPosition += v * dt;
                    items[i].Rotate(0, 0, 220f * dt);
                    var c = imgs[i].color; c.a = Mathf.Clamp01(1f - (t / life - 0.4f) / 0.6f); imgs[i].color = c;
                }
                yield return null;
            }
            for (int i = 0; i < items.Count; i++) if (items[i] != null) UnityEngine.Object.Destroy(items[i].gameObject);
        }
    }

    // ---------------------------------------------------------------------
    // Hover / press scale + click sound for any Selectable
    // ---------------------------------------------------------------------
    public class ButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public float hoverScale = 1.06f;
        public float pressScale = 0.94f;
        public bool silent;
        Vector3 baseScale = Vector3.one;
        bool hover, down;
        Selectable sel;

        void Awake() { baseScale = transform.localScale; sel = GetComponent<Selectable>(); }
        void OnDisable() { hover = down = false; transform.localScale = baseScale; }

        bool Active { get { return sel == null || sel.interactable; } }

        void Update()
        {
            float k = 1f;
            if (Active) k = down ? pressScale : (hover ? hoverScale : 1f);
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale * k, 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime));
        }

        public void OnPointerEnter(PointerEventData e) { hover = true; if (Active && !silent) Sfx.Play(Snd.Hover); }
        public void OnPointerExit(PointerEventData e) { hover = false; down = false; }
        public void OnPointerDown(PointerEventData e) { down = true; }
        public void OnPointerUp(PointerEventData e) { down = false; }
        public void OnPointerClick(PointerEventData e) { if (Active && !silent) Sfx.Play(Snd.Click); }
    }

    // ---------------------------------------------------------------------
    // Twinkling / drifting background stars
    // ---------------------------------------------------------------------
    public class StarField : MonoBehaviour
    {
        public int count = 80;
        public Vector2 area = new Vector2(1920, 1080);
        RectTransform[] stars;
        Image[] imgs;
        float[] speed, phase, baseAlpha;

        void Start()
        {
            stars = new RectTransform[count];
            imgs = new Image[count];
            speed = new float[count]; phase = new float[count]; baseAlpha = new float[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("star", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
                rt.pivot = new Vector2(0.5f, 0.5f);
                float s = UnityEngine.Random.Range(2f, 6f);
                rt.sizeDelta = new Vector2(s, s);
                rt.anchoredPosition = new Vector2(UnityEngine.Random.Range(0f, area.x), UnityEngine.Random.Range(0f, area.y));
                var im = go.GetComponent<Image>();
                im.sprite = Art.Circle; im.raycastTarget = false;
                imgs[i] = im; stars[i] = rt;
                speed[i] = s * 2.5f;
                phase[i] = UnityEngine.Random.Range(0f, 6.28f);
                baseAlpha[i] = UnityEngine.Random.Range(0.35f, 0.95f);
                im.color = new Color(0.85f, 0.95f, 1f, baseAlpha[i]);
            }
        }

        void Update()
        {
            if (stars == null) return;
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < count; i++)
            {
                var p = stars[i].anchoredPosition;
                p.x -= speed[i] * dt;
                if (p.x < -10f) { p.x = area.x + 10f; p.y = UnityEngine.Random.Range(0f, area.y); }
                stars[i].anchoredPosition = p;
                var c = imgs[i].color;
                c.a = baseAlpha[i] * (0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 2f + phase[i]));
                imgs[i].color = c;
            }
        }
    }

    // ---------------------------------------------------------------------
    // Challenge data
    // ---------------------------------------------------------------------
    public enum Unknown { Mass, Force, Accel }

    // Optional scene art that is only shown for some missions (objects live under the Stage).
    [Flags]
    public enum MissionArt { None = 0, CartA = 1, CartB = 2, Ramp = 4, Asteroid = 8, Friction = 16 }

    [Serializable]
    public class ChallengeData
    {
        public const float G = 9.81f;
        public string title, story;
        public string formula;          // optional: replaces the default formula line
        public string[] rows;           // optional: custom data rows (max 4). Put {?} where the unknown value goes
        public Unknown unknown;
        public bool gravity;
        public float m, F, a;           // given values (the unknown one is ignored)
        public float resist;            // friction / ramp pull-back that opposes the push (N)
        public float tolerance = -1f;   // <= 0 means 1% of the answer (minimum 0.02)
        public MissionArt art;
        public string[] hints;
        public float trap;              // a typical wrong answer to catch (0 = none)
        public string trapMsg;          // message shown when the player types the trap value
        public string workedStep;       // shown under the stage after a wrong answer

        public float Answer
        {
            get
            {
                switch (unknown)
                {
                    case Unknown.Accel: return gravity ? (F - m * G) / m : (F - resist) / m;
                    case Unknown.Force: return gravity ? m * (a + G) : m * a + resist;
                    default: return (F - resist) / a;
                }
            }
        }

        public float Tolerance { get { return tolerance > 0f ? tolerance : Mathf.Max(Mathf.Abs(Answer) * 0.01f, 0.02f); } }

        public string Unit { get { return unknown == Unknown.Mass ? "kg" : unknown == Unknown.Force ? "N" : "m/s²"; } }

        // Values used for the rocket simulation after a correct answer.
        public float SimMass { get { return unknown == Unknown.Mass ? Answer : m; } }
        public float SimAccel { get { return unknown == Unknown.Accel ? Answer : a; } }
        // Force that reaches the sim: applied thrust, minus friction/ramp pull when there is no gravity mode.
        public float SimForce
        {
            get
            {
                float applied = unknown == Unknown.Force ? Answer : F;
                return gravity ? applied : Mathf.Max(0f, applied - resist);
            }
        }

        static ChallengeData Make(string title, string story, Unknown u, bool g, float m, float F, float a, params string[] hints)
        {
            return new ChallengeData { title = title, story = story, unknown = u, gravity = g, m = m, F = F, a = a, hints = hints };
        }

        static List<ChallengeData> all;
        public static List<ChallengeData> All
        {
            get
            {
                if (all != null) return all;
                all = new List<ChallengeData>();
                all.Add(Make("FIRST BURN", "The Starflare probe fires its engine in deep space. How fast does it speed up?",
                    Unknown.Accel, false, 1000f, 5000f, 0f,
                    "Newton's second law: F = m × a. To find a, divide the force by the mass.",
                    "a = F ÷ m = 5000 ÷ 1000"));
                all.Add(Make("HEAVY PUSH", "A 500 kg rover must accelerate at 8 m/s². How much force must its motor push with?",
                    Unknown.Force, false, 500f, 0f, 8f,
                    "Newton's second law: F = m × a. Multiply mass by acceleration.",
                    "F = 500 × 8"));
                all.Add(Make("CARGO CHECK", "A cargo pod accelerates at 6 m/s² under a 12000 N push. What is its mass?",
                    Unknown.Mass, false, 0f, 12000f, 6f,
                    "Rearrange F = m × a to get m = F ÷ a.",
                    "m = 12000 ÷ 6"));
                all.Add(Make("BOOSTER TEST", "A 2500 kg booster is pushed by a 30000 N engine in space. Find its acceleration.",
                    Unknown.Accel, false, 2500f, 30000f, 0f,
                    "Divide the force by the mass: a = F ÷ m.",
                    "a = 30000 ÷ 2500"));
                all.Add(Make("STRONG THRUSTER", "A 1200 kg satellite must reach an acceleration of 15 m/s². What thrust is needed?",
                    Unknown.Force, false, 1200f, 0f, 15f,
                    "F = m × a.",
                    "F = 1200 × 15"));
                all.Add(Make("MYSTERY PAYLOAD", "An unknown payload speeds up at 9 m/s² when pushed with 45000 N. Find its mass.",
                    Unknown.Mass, false, 0f, 45000f, 9f,
                    "m = F ÷ a.",
                    "m = 45000 ÷ 9"));
                all.Add(Make("LIFTOFF!", "On Earth a 2000 kg rocket fires a 30000 N engine. Gravity pulls it down with W = m × g (g = 9.81). Find the NET acceleration upward.",
                    Unknown.Accel, true, 2000f, 30000f, 0f,
                    "Net force = thrust − weight, where weight W = m × g. Then a = F_net ÷ m.",
                    "W = 2000 × 9.81 = 19620 N, so F_net = 30000 − 19620. Then divide by 2000."));
                all.Add(Make("MISSION PLANNER", "A 1500 kg rocket must climb at 4 m/s² on Earth (g = 9.81). What thrust must the engine give?",
                    Unknown.Force, true, 1500f, 0f, 4f,
                    "Thrust must beat gravity AND accelerate the rocket: F = m × (a + g).",
                    "F = 1500 × (4 + 9.81)"));

                // ---- Missions 9-16 ----
                all.Add(new ChallengeData
                {
                    title = "TWO PUSHERS",
                    story = "Two cadets push a 40 kg cart on a smooth track. One pushes forward with 150 N, the other pushes back with 30 N. Find the cart's acceleration.",
                    formula = "F_net = push forward − push back      F_net = m × a", rows = new[] { "Mass<pos=215>m = 40 kg", "Push forward<pos=215>F = 150 N", "Push back<pos=215>F = 30 N", "Net accel.<pos=215>a = {?} m/s²" }, unknown = Unknown.Accel, m = 40f, F = 150f, resist = 30f, art = MissionArt.CartA,
                    hints = new[] { "The two pushes act in opposite directions, so subtract them to get the net force.", "F_net = 150 − 30 = 120 N. Then a = F_net ÷ m = 120 ÷ 40." }
                });
                all.Add(new ChallengeData
                {
                    title = "LINKED CARTS",
                    story = "Cart A (30 kg) pulls cart B (20 kg) with a 200 N force. Find the acceleration of the linked pair.",
                    formula = "F = (mass A + mass B) × a",
                    rows = new[] { "Cart A<pos=215>m = 30 kg", "Cart B<pos=215>m = 20 kg", "Pull force<pos=215>F = 200 N", "Acceleration<pos=215>a = {?} m/s²" },
                    unknown = Unknown.Accel, m = 50f, F = 200f, art = MissionArt.CartA | MissionArt.CartB, trap = 6.67f, trapMsg = "You used only cart A's mass. Both carts move together, so add the masses first.",
                    hints = new[] { "Both carts move together, so add their masses first.", "a = F ÷ (30 + 20) = 200 ÷ 50" }
                });
                all.Add(new ChallengeData
                {
                    title = "ASTEROID NUDGE",
                    story = "A 5 tonne asteroid must speed up by 0.2 m/s². How much thrust must the tug engine give? (1 tonne = 1000 kg)",
                    formula = "F = m × a      (mass must be in kg)", rows = new[] { "Mass<pos=215>m = 5 tonnes", "Conversion<pos=215>1 tonne = 1000 kg", "Acceleration<pos=215>a = 0.2 m/s²", "Thrust<pos=215>F = {?} N" }, unknown = Unknown.Force, m = 5000f, a = 0.2f, trap = 1f, trapMsg = "Convert tonnes to kg first: 5 tonnes = 5000 kg.", art = MissionArt.Asteroid,
                    hints = new[] { "Convert the mass to kilograms first: 1 tonne = 1000 kg.", "F = m × a = 5000 × 0.2" }
                });
                all.Add(new ChallengeData
                {
                    title = "DOUBLE TUG",
                    story = "Two tugs push the same asteroid forward, one with 15000 N and one with 9000 N. It accelerates at 0.8 m/s². What is the asteroid's mass?",
                    formula = "F_total = F1 + F2      m = F_total ÷ a", rows = new[] { "Tug 1<pos=215>F = 15000 N", "Tug 2<pos=215>F = 9000 N", "Acceleration<pos=215>a = 0.8 m/s²", "Mass<pos=215>m = {?} kg" }, unknown = Unknown.Mass, F = 24000f, a = 0.8f, trap = 18750f, trapMsg = "You used only one tug. Add both pushes to get the total force.", art = MissionArt.Asteroid,
                    hints = new[] { "Both tugs push the same way, so add their forces first.", "F = 15000 + 9000 = 24000 N. Then m = F ÷ a = 24000 ÷ 0.8." }
                });
                all.Add(new ChallengeData
                {
                    title = "ROUGH PATCH",
                    story = "A 50 kg crate is pushed with 200 N across a rough patch. Friction pulls back with 80 N. Find the net acceleration.",
                    formula = "F_net = F − friction      F_net = m × a",
                    rows = new[] { "Mass<pos=215>m = 50 kg", "Push force<pos=215>F = 200 N", "Friction<pos=215>f = 80 N", "Net accel.<pos=215>a = {?} m/s²" },
                    unknown = Unknown.Accel, m = 50f, F = 200f, resist = 80f, tolerance = 0.05f, art = MissionArt.Friction,
                    hints = new[] { "Friction pulls backwards. First find the net force: F_net = F − friction.", "F_net = 200 − 80 = 120 N. Then a = F_net ÷ m." }
                });
                all.Add(new ChallengeData
                {
                    title = "STICKY SLED",
                    story = "An 80 kg sled must accelerate at 2 m/s² on sticky ground. Friction is 60 N. How big must the push be?",
                    formula = "F = m × a + friction",
                    rows = new[] { "Mass<pos=215>m = 80 kg", "Acceleration<pos=215>a = 2 m/s²", "Friction<pos=215>f = 60 N", "Push force<pos=215>F = {?} N" },
                    unknown = Unknown.Force, m = 80f, a = 2f, resist = 60f, art = MissionArt.Friction,
                    hints = new[] { "The push must beat friction AND accelerate the sled: F = m × a + friction.", "F = 80 × 2 + 60" }
                });
                all.Add(new ChallengeData
                {
                    title = "RAMP CLIMB",
                    story = "A 100 kg cart is pushed up a ramp with 800 N. Gravity pulls it back down the slope with 300 N. Find its acceleration.",
                    formula = "F_net = push − ramp pull      F_net = m × a",
                    rows = new[] { "Mass<pos=215>m = 100 kg", "Push force<pos=215>F = 800 N", "Ramp pull-back<pos=215>300 N", "Net accel.<pos=215>a = {?} m/s²" },
                    unknown = Unknown.Accel, m = 100f, F = 800f, resist = 300f, art = MissionArt.Ramp,
                    hints = new[] { "Subtract the ramp pull-back from the push to get the net force, then divide by the mass.", "F_net = 800 − 300 = 500 N. Then a = 500 ÷ 100." }
                });
                all.Add(new ChallengeData
                {
                    title = "RAMP LANDER",
                    story = "A 2000 kg lander must climb a rough ramp at 2.5 m/s². The ramp pulls back 3000 N and friction adds 1000 N. What thrust must the engine give?",
                    formula = "F = m × a + (ramp + friction)",
                    rows = new[] { "Mass<pos=215>m = 2000 kg", "Acceleration<pos=215>a = 2.5 m/s²", "Ramp + friction<pos=215>3000 + 1000 N", "Engine thrust<pos=215>F = {?} N" },
                    unknown = Unknown.Force, m = 2000f, a = 2.5f, resist = 4000f, art = MissionArt.Ramp | MissionArt.Friction,
                    hints = new[] { "The engine must give the acceleration force AND beat everything that pulls back: F = m × a + (ramp + friction).", "F = 2000 × 2.5 + (3000 + 1000) = 5000 + 4000" }
                });
                string[] steps =
                {
                    "a = F ÷ m = 5000 ÷ 1000 = 5 m/s²",
                    "F = m × a = 500 × 8 = 4000 N",
                    "m = F ÷ a = 12000 ÷ 6 = 2000 kg",
                    "a = F ÷ m = 30000 ÷ 2500 = 12 m/s²",
                    "F = m × a = 1200 × 15 = 18000 N",
                    "m = F ÷ a = 45000 ÷ 9 = 5000 kg",
                    "W = m × g = 2000 × 9.81 = 19620 N   →   F_net = 30000 − 19620 = 10380 N   →   a = 10380 ÷ 2000 = 5.19 m/s²",
                    "F = m × (a + g) = 1500 × (4 + 9.81) = 1500 × 13.81 = 20715 N",
                    "F_net = 150 − 30 = 120 N   →   a = F_net ÷ m = 120 ÷ 40 = 3 m/s²",
                    "m = 30 + 20 = 50 kg   →   a = F ÷ m = 200 ÷ 50 = 4 m/s²",
                    "5 tonnes = 5000 kg   →   F = m × a = 5000 × 0.2 = 1000 N",
                    "F = 15000 + 9000 = 24000 N   →   m = F ÷ a = 24000 ÷ 0.8 = 30000 kg",
                    "F_net = 200 − 80 = 120 N   →   a = F_net ÷ m = 120 ÷ 50 = 2.4 m/s²",
                    "F = m × a + friction = 80 × 2 + 60 = 220 N",
                    "F_net = 800 − 300 = 500 N   →   a = F_net ÷ m = 500 ÷ 100 = 5 m/s²",
                    "F_net = m × a = 2000 × 2.5 = 5000 N   →   F = 5000 + (3000 + 1000) = 9000 N"
                };
                for (int i = 0; i < all.Count && i < steps.Length; i++) all[i].workedStep = steps[i];
                return all;
            }
        }
    }
}
