using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceAcademyV2
{
    // Gravity Lab: W = m x g on Earth / Moon / Mars with crate, lift, spring scale, gauge and jump demo.
    public class GravityController : MonoBehaviour
    {
        public AcademyManager manager;
        public Mentor mentor;

        [Header("Planets")]
        public Button[] planetButtons;
        public Image[] planetImages;
        public Sprite[] planetActive, planetInactive;
        public Image skyImage, surfaceImage, atmosphereImage;
        public Sprite[] atmosphereSprites;   // optional overlay per planet (null = none)
        public Sprite[] surfaceSprites;
        public CanvasGroup starsGroup;

        [Header("Mass control")]
        public Slider massSlider;
        public TMP_Text massLabel;
        public Button liftButton, resetButton;

        [Header("Stage objects")]
        public RectTransform platform, crate, astronaut;
        public TMP_Text crateLabel;
        public Button astronautButton;
        public TMP_Text astronautHint;

        [Header("Gauge + spring")]
        public RectTransform needle, spring, miniCrate;
        public TMP_Text gaugeText;

        [Header("Formula card")]
        public TMP_Text massRow, gravRow, weightRow, compareText, jumpText, liftText, planetTitle;

        static readonly string[] names = { "EARTH", "MOON", "MARS", "JUPITER", "VENUS", "SUN" };
        static readonly float[] gs = { 9.81f, 1.62f, 3.71f, 24.79f, 8.87f, 274f };
        static readonly Color[] skies = { new Color(0.30f, 0.55f, 0.92f), new Color(0.02f, 0.03f, 0.07f), new Color(0.62f, 0.34f, 0.24f), new Color(0.45f, 0.32f, 0.24f), new Color(0.55f, 0.38f, 0.14f), new Color(0.38f, 0.08f, 0.03f) };

        int planet;
        bool wired, lifting, jumping;
        Vector2 platformBase, crateBase, astronautBase, springBase;
        float springBaseH;
        float needleAngle, needleVel;

        const float LiftPixels = 190f;
        const float LiftMeters = 2f;

        float Mass { get { return massSlider.value; } }
        float G { get { return gs[planet]; } }
        float Weight { get { return Mass * G; } }

        void Wire()
        {
            if (wired) return;
            wired = true;
            platformBase = platform.anchoredPosition; crateBase = crate.anchoredPosition;
            astronautBase = astronaut.anchoredPosition; springBase = spring.anchoredPosition; springBaseH = spring.sizeDelta.y;
            for (int i = 0; i < planetButtons.Length; i++)
            {
                int idx = i;
                planetButtons[i].onClick.AddListener(() => SelectPlanet(idx, true));
            }
            massSlider.onValueChanged.AddListener(v => { Sfx.Play(Snd.Tick, 0.4f); Refresh(); });
            liftButton.onClick.AddListener(Lift);
            resetButton.onClick.AddListener(() => { StopAllCoroutines(); lifting = jumping = false; massSlider.value = 10f; SelectPlanet(0, true); });
            astronautButton.onClick.AddListener(Jump);
        }

        public void Enter()
        {
            Wire();
            StopAllCoroutines();
            lifting = jumping = false;
            platform.anchoredPosition = platformBase; crate.anchoredPosition = crateBase; astronaut.anchoredPosition = astronautBase;
            massSlider.value = Mathf.Max(massSlider.value, 10f);
            SelectPlanet(planet, false);
            mentor.Say("Welcome to the Gravity Lab! Pick a planet, change the crate's mass, and watch the weight change. Tap the astronaut to see a jump!");
        }

        void SelectPlanet(int p, bool speak)
        {
            if (lifting || jumping) return;
            planet = p;
            for (int i = 0; i < planetImages.Length; i++) planetImages[i].sprite = i == p ? planetActive[i] : planetInactive[i];
            skyImage.color = skies[p];
            surfaceImage.sprite = surfaceSprites[p];
            if (starsGroup != null) starsGroup.alpha = p == 1 ? 1f : (p == 2 ? 0.25f : 0f);
            if (atmosphereImage != null)
            {
                var at = atmosphereSprites != null && p < atmosphereSprites.Length ? atmosphereSprites[p] : null;
                atmosphereImage.sprite = at; atmosphereImage.enabled = at != null;
            }
            planetTitle.text = names[p] + "  ·  g = " + G.ToString("0.00", CultureInfo.InvariantCulture) + " m/s²";
            Refresh();
            if (speak)
            {
                Sfx.Play(Snd.Tab);
                if (p == 0) mentor.Say("Earth: g = 9.81 m/s². Every kilogram weighs about 9.8 newtons here.");
                else if (p == 1) mentor.Say("The Moon: g = 1.62 m/s², only one sixth of Earth's. The same crate feels much lighter!");
                else if (p == 2) mentor.Say("Mars: g = 3.71 m/s², about 38% of Earth's gravity.");
                else if (p == 3) mentor.Say("Jupiter: g = 24.79 m/s², about 2.5 times Earth's. The same crate is much heavier here!");
                else if (p == 4) mentor.Say("Venus: g = 8.87 m/s², close to Earth's. But the air is crushing hot, so no picnic!");
                else mentor.Say("The Sun's surface: g = 274 m/s², about 28 times Earth's. Weight is huge, but no astronaut could ever stand there!");
            }
        }

        void Refresh()
        {
            float m = Mass, w = Weight;
            massLabel.text = "Mass: " + m.ToString("0") + " kg";
            crateLabel.text = m.ToString("0") + " kg";
            massRow.text = "m = " + m.ToString("0") + " kg";
            gravRow.text = "g = " + G.ToString("0.00", CultureInfo.InvariantCulture) + " m/s²";
            weightRow.text = "W = " + w.ToString("0.0", CultureInfo.InvariantCulture) + " N";
            gaugeText.text = "WEIGHT  " + w.ToString("0.0", CultureInfo.InvariantCulture) + " N";

            string cmp = "SAME CRATE ON:\n";
            for (int i = 0; i < names.Length; i++)
            {
                string line = names[i] + "<pos=135>" + (m * gs[i]).ToString("0.0", CultureInfo.InvariantCulture) + " N";
                cmp += (i == planet ? "<color=#FF9F1C>" + line + "</color>" : line) + "\n";
            }
            compareText.text = cmp;
            float v0 = 3f;
            float h = v0 * v0 / (2f * G);
            jumpText.text = "Astronaut jump: " + h.ToString("0.00", CultureInfo.InvariantCulture) + " m";
            liftText.text = lifting ? liftText.text : "Lift work: W × h = " + (w * LiftMeters).ToString("0", CultureInfo.InvariantCulture) + " J";

            float scale = Mathf.Lerp(0.8f, 1.35f, Mathf.Clamp01(w / (100f * 9.81f)));
            crate.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.1f, Mathf.Clamp01(m / 100f));
        }

        void Update()
        {
            if (needle == null) return;
            float kgEq = Mathf.Clamp(Weight / 9.81f, 0f, 100f);
            float target = 120f - 2.4f * kgEq;
            float dt = Time.unscaledDeltaTime;
            float acc = 90f * (target - needleAngle) - 11f * needleVel;
            needleVel += acc * dt;
            needleAngle += needleVel * dt;
            needle.localRotation = Quaternion.Euler(0f, 0f, needleAngle);

            // spring stretch follows weight
            float stretch = Mathf.Lerp(0.85f, 1.45f, Mathf.Clamp01(kgEq / 100f));
            float h = springBaseH * stretch;
            spring.sizeDelta = new Vector2(spring.sizeDelta.x, h);
            if (miniCrate != null) miniCrate.anchoredPosition = new Vector2(springBase.x, springBase.y - h + 8f);
        }

        void Lift()
        {
            if (lifting || jumping) return;
            StartCoroutine(LiftRoutine());
        }

        IEnumerator LiftRoutine()
        {
            lifting = true;
            liftButton.interactable = false;
            float w = Weight;
            Sfx.Play(Snd.Motor);
            mentor.Say("Lifting needs a force equal to the weight: " + w.ToString("0.0", CultureInfo.InvariantCulture) + " N. Work = force × height.");
            float dur = 1.6f + Mathf.Clamp01(w / 981f) * 1.2f; // heavier = slower motor
            liftText.text = "Lifting " + w.ToString("0") + " N …";
            yield return Fx.Run(dur, k =>
            {
                float e = Mathf.SmoothStep(0f, 1f, k);
                platform.anchoredPosition = platformBase + new Vector2(0f, LiftPixels * e);
                crate.anchoredPosition = crateBase + new Vector2(0f, LiftPixels * e);
            });
            liftText.text = "Work done = " + w.ToString("0.0", CultureInfo.InvariantCulture) + " N × " + LiftMeters + " m = " + (w * LiftMeters).ToString("0", CultureInfo.InvariantCulture) + " J";
            Sfx.Play(Snd.Thud);
            yield return new WaitForSecondsRealtime(0.7f);
            yield return Fx.Run(1.1f, k =>
            {
                float e = Mathf.SmoothStep(0f, 1f, k);
                platform.anchoredPosition = platformBase + new Vector2(0f, LiftPixels * (1f - e));
                crate.anchoredPosition = crateBase + new Vector2(0f, LiftPixels * (1f - e));
            });
            Sfx.Play(Snd.Thud);
            lifting = false;
            liftButton.interactable = true;
        }

        void Jump()
        {
            if (jumping || lifting) return;
            StartCoroutine(JumpRoutine());
        }

        IEnumerator JumpRoutine()
        {
            jumping = true;
            float v0 = 3f, g = G;
            float total = 2f * v0 / g;
            float pxPerM = 75f;
            if (astronautHint != null) astronautHint.gameObject.SetActive(false);
            Sfx.Play(Snd.Tab);
            float t = 0f;
            while (t < total)
            {
                t += Time.unscaledDeltaTime;
                float y = v0 * t - 0.5f * g * t * t;
                astronaut.anchoredPosition = astronautBase + new Vector2(0f, Mathf.Max(0f, y) * pxPerM);
                yield return null;
            }
            astronaut.anchoredPosition = astronautBase;
            Sfx.Play(Snd.Thud, 0.6f);
            float h = v0 * v0 / (2f * g);
            mentor.Say("Jump height = v² ÷ (2g) = " + h.ToString("0.00", CultureInfo.InvariantCulture) + " m on " + names[planet].ToLower() + ". Lower gravity means higher jumps!");
            if (astronautHint != null) astronautHint.gameObject.SetActive(true);
            jumping = false;
        }
    }
}
