using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceAcademyV2
{
    // Challenge mode: 16 missions, answer F = m x a questions, correct answers launch the rocket.
    public class ChallengeController : MonoBehaviour
    {
        public AcademyManager manager;
        public Mentor mentor;
        public RocketSim sim;

        [Header("Brief panel")]
        public TMP_Text titleText, storyText, formulaText, feedbackText;
        public GameObject[] rows;          // 4 rows: mass, force, accel, gravity
        public TMP_Text[] rowTexts;

        [Header("Level chips")]
        public Button[] chips;
        public Image[] chipImages;
        public TMP_Text[] chipLabels;
        public GameObject[] chipLocks;
        public Sprite chipOpen, chipDone, chipLockedSprite;

        [Header("Mission scene art (optional, shown only for the missions that use it)")]
        public GameObject artCartA, artCartB, artRamp, artAsteroid, artFriction;

        [Header("Worked solution (shown after a wrong answer)")]
        public CanvasGroup workedPanel;
        public TMP_Text workedText;

        [Header("Missions 9-16: own scene per mission")]
        public Image missionBadge;          // emblem shown in the stage corner
        public Image trackImage;            // ground strip under the carts / sled / ramp
        public Image groundImage;           // stage ground (hidden in the asteroid missions)
        public Sprite[] badgeSprites;       // 8 emblems, mission 9..16
        public Sprite[] trackSprites;       // 8 ground strips (null = none, e.g. space)
        public Sprite[] asteroidSprites;    // 2 rocks, mission 11 and 12
        public GameObject[] hideInGround;   // rocket, tower, pad, arm, clamps (hidden in missions 9-16)

        [Header("Controls")]
        public TMP_InputField input;
        public TMP_Text placeholderText;
        public Button submit, hint, reset;

        int level;
        int attempts, hintsUsed;
        bool busy;
        bool wired;
        readonly List<string> mistakes = new List<string>();
        Coroutine workedRoutine;
        Coroutine groundRoutine;
        bool groundScene;
        Color origGroundColor = Color.white;
        bool origCaptured;

        // theme per mission 9..16
        static readonly Color[] SkyTint = {
            new Color(0.32f,0.58f,0.95f), new Color(0.18f,0.55f,0.62f), new Color(0.03f,0.04f,0.12f), new Color(0.07f,0.04f,0.16f),
            new Color(0.85f,0.52f,0.30f), new Color(0.50f,0.30f,0.55f), new Color(0.25f,0.50f,0.70f), new Color(0.55f,0.25f,0.20f) };
        static readonly Color[] GroundTint = {
            new Color(0.85f,0.95f,1f), new Color(0.80f,0.95f,0.95f), Color.white, Color.white,
            new Color(1f,0.85f,0.70f), new Color(0.85f,0.75f,0.95f), new Color(0.80f,0.90f,1f), new Color(1f,0.75f,0.65f) };
        static readonly bool[] SpaceScene = { false, false, true, true, false, false, false, false };
        static readonly float[] PxPerMeter = { 12f, 10f, 140f, 55f, 14f, 16f, 16f, 10f };
        static readonly string[] SceneNames = { "ICE TRACK", "LINKED TRACK", "DEEP SPACE", "DEEP SPACE", "ROUGH PATCH", "STICKY GROUND", "RAMP", "ROUGH RAMP" };
        const float RampX = 330f, RampDeg = 20f, GroundY = 117f;

        List<ChallengeData> Data { get { return ChallengeData.All; } }

        void Wire()
        {
            if (wired) return;
            wired = true;
            submit.onClick.AddListener(OnSubmit);
            hint.onClick.AddListener(OnHint);
            reset.onClick.AddListener(() => LoadLevel(level, false));
            input.onSubmit.AddListener(s => OnSubmit());
            for (int i = 0; i < chips.Length; i++)
            {
                int idx = i;
                chips[i].onClick.AddListener(() => OnChip(idx));
            }
        }

        public void Enter()
        {
            Wire();
            LoadLevel(Progress.FirstOpenLevel(), true);
        }

        void RefreshChips()
        {
            for (int i = 0; i < chips.Length; i++)
            {
                bool unlocked = Progress.Unlocked(i);
                bool done = Progress.GetStars(i) > 0;
                chipImages[i].sprite = !unlocked ? chipLockedSprite : (done ? chipDone : chipOpen);
                chipImages[i].color = (i == level) ? Color.white : new Color(0.75f, 0.75f, 0.8f, 1f);
                chipLocks[i].SetActive(!unlocked);
                chipLabels[i].gameObject.SetActive(unlocked);
                chipLabels[i].text = (i + 1).ToString();
                chips[i].transform.localScale = Vector3.one * (i == level ? 1.15f : 1f);
            }
        }

        void OnChip(int i)
        {
            if (busy) return;
            if (!Progress.Unlocked(i))
            {
                Sfx.Play(Snd.Wrong, 0.6f);
                StartCoroutine(Fx.Shake((RectTransform)chips[i].transform, 0.35f, 8f));
                mentor.Oops("That mission is locked. Complete the previous one first!");
                return;
            }
            LoadLevel(i, false);
        }

        public void LoadLevel(int i, bool greet)
        {
            level = Mathf.Clamp(i, 0, Data.Count - 1);
            var d = Data[level];
            attempts = 0; hintsUsed = 0; busy = false;
            mistakes.Clear();
            HideWorked();
            sim.ResetRocket();
            input.text = "";
            input.interactable = true;
            submit.interactable = true; hint.interactable = true; reset.interactable = true;
            titleText.text = "MISSION " + (level + 1) + " / " + Data.Count + "  ·  " + d.title;
            storyText.text = d.story;
            formulaText.text = !string.IsNullOrEmpty(d.formula) ? d.formula : (d.gravity ? "F = m × a      W = m × g" : "F = m × a");
            placeholderText.text = "Answer in " + d.Unit;
            feedbackText.text = "";

            string unk = "<color=#FF9F1C>?</color>";
            if (d.rows != null && d.rows.Length > 0)
            {
                // custom rows (missions 9-16): {?} marks the unknown value
                for (int r = 0; r < rows.Length; r++)
                {
                    bool has = r < d.rows.Length;
                    rows[r].SetActive(has);
                    if (has) rowTexts[r].text = d.rows[r].Replace("{?}", unk);
                }
            }
            else
            {
                rows[0].SetActive(true); rows[1].SetActive(true); rows[2].SetActive(true);
                rowTexts[0].text = "Mass<pos=215>m = " + (d.unknown == Unknown.Mass ? unk : Fmt(d.m)) + " kg";
                rowTexts[1].text = "Force<pos=215>F = " + (d.unknown == Unknown.Force ? unk : Fmt(d.F)) + " N";
                rowTexts[2].text = "Acceleration<pos=215>a = " + (d.unknown == Unknown.Accel ? unk : Fmt(d.a)) + " m/s²";
                rows[3].SetActive(d.gravity);
                rowTexts[3].text = "Gravity<pos=215>g = 9.81 m/s²";
                if (d.gravity && d.unknown == Unknown.Accel) rowTexts[2].text = "Net accel.<pos=215>a = " + unk + " m/s²";
            }
            ShowArt(d.art);
            ApplyScene(level);

            RefreshChips();
            if (greet)
                mentor.Say(level == 0 ? "Welcome to Space Academy, cadet! Solve each mission to launch the rocket. Tap HINT if you get stuck." : "Ready for the next mission? Read the data and type your answer.");
            else
                mentor.Say("Mission " + (level + 1) + ": " + d.title.ToLower() + ". You can do it!");
        }

        void ShowArt(MissionArt a)
        {
            if (artCartA != null) artCartA.SetActive((a & MissionArt.CartA) != 0);
            if (artCartB != null) artCartB.SetActive((a & MissionArt.CartB) != 0);
            if (artRamp != null) artRamp.SetActive((a & MissionArt.Ramp) != 0);
            if (artAsteroid != null) artAsteroid.SetActive((a & MissionArt.Asteroid) != 0);
            if (artFriction != null) artFriction.SetActive((a & MissionArt.Friction) != 0);
        }

        static string Fmt(float v) { return v.ToString("0.##", CultureInfo.InvariantCulture); }

        void OnHint()
        {
            if (busy) return;
            var d = Data[level];
            hintsUsed++;
            string h = d.hints[Mathf.Min(hintsUsed - 1, d.hints.Length - 1)];
            feedbackText.color = new Color(1f, 0.85f, 0.35f);
            feedbackText.text = "HINT " + Mathf.Min(hintsUsed, d.hints.Length) + ": " + h;
            mentor.Think(h);
        }

        void OnSubmit()
        {
            if (busy) return;
            var d = Data[level];
            string raw = input.text.Trim().Replace(',', '.');
            float val;
            if (string.IsNullOrEmpty(raw) || !float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out val))
            {
                feedbackText.color = new Color(1f, 0.6f, 0.3f);
                feedbackText.text = "Type a number first (units: " + d.Unit + ").";
                Sfx.Play(Snd.Wrong, 0.5f);
                StartCoroutine(Fx.Shake((RectTransform)input.transform, 0.3f, 8f));
                return;
            }
            attempts++;
            float ans = d.Answer;
            float tol = d.Tolerance;
            if (Mathf.Abs(val - ans) <= tol) OnCorrect(d, ans);
            else OnWrong(d, val, ans);
        }

        void OnWrong(ChallengeData d, float val, float ans)
        {
            feedbackText.color = new Color(1f, 0.4f, 0.4f);
            string extra = "Check the formula and the units.";
            if (d.trap > 0f && Mathf.Abs(val - d.trap) <= Mathf.Max(d.trap * 0.01f, 0.02f)) extra = d.trapMsg; else if (val > 0f && Mathf.Abs(val - 1f / ans) < 0.05f) extra = "You divided the wrong way round.";
            else if (d.resist > 0f && NearNaive(d, val)) extra = "Don't forget the opposing force (friction, ramp pull-back or the push from the other side).";
            else if (val > ans) extra = "Too high. Re-check your calculation.";
            else if (val < ans) extra = "Too low. Re-check your calculation.";
            feedbackText.text = "Not quite. " + extra;
            mistakes.Add("Attempt " + attempts + ": you typed " + Fmt(val) + ". " + extra);
            Sfx.Play(Snd.Wrong);
            StartCoroutine(Fx.Shake((RectTransform)input.transform, 0.4f, 12f));
            if (workedRoutine != null) StopCoroutine(workedRoutine);
            workedRoutine = StartCoroutine(RevealWorked(d));
            mentor.Teach(attempts >= 2 ? "No worries! Study the worked solution under the stage, then try again." : "Almost! Here is the worked solution under the stage. Compare it with your steps.");
        }

        IEnumerator RevealWorked(ChallengeData d)
        {
            if (workedPanel == null || workedText == null || string.IsNullOrEmpty(d.workedStep)) yield break;
            yield return new WaitForSecondsRealtime(0.35f);
            workedText.text = d.workedStep;
            workedPanel.gameObject.SetActive(true);
            workedPanel.alpha = 0f;
            Sfx.Play(Snd.Reveal);
            StartCoroutine(Fx.Fade(workedPanel, 1f, 0.25f));
            yield return Fx.Pop(workedPanel.transform, 0.92f, 0.3f);
            workedRoutine = null;
        }

        void HideWorked()
        {
            if (workedRoutine != null) { StopCoroutine(workedRoutine); workedRoutine = null; }
            if (workedPanel == null) return;
            workedPanel.alpha = 0f;
            workedPanel.gameObject.SetActive(false);
        }

        string BuildReview(ChallengeData d)
        {
            if (mistakes.Count == 0 && hintsUsed == 0) return null;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<color=#4FE3FF>MISSION " + (level + 1) + "  ·  " + d.title + "</color>");
            int start = Mathf.Max(0, mistakes.Count - 4);
            for (int i = start; i < mistakes.Count; i++) sb.AppendLine(mistakes[i]);
            if (hintsUsed > 0) sb.AppendLine("Hints used: " + hintsUsed);
            if (!string.IsNullOrEmpty(d.workedStep)) sb.Append("<color=#FF9F1C>Worked solution:</color> " + d.workedStep);
            return sb.ToString();
        }

        void OnCorrect(ChallengeData d, float ans)
        {
            busy = true;
            HideWorked();
            string review = BuildReview(d);
            submit.interactable = false; hint.interactable = false; reset.interactable = false; input.interactable = false;
            feedbackText.color = new Color(0.3f, 0.95f, 0.5f);
            feedbackText.text = "Correct!  " + FormatAnswer(d, ans) + "  Launching…";
            Sfx.Play(Snd.Correct);
            mentor.Cheer("Perfect answer! Stand clear, launching the rocket!");
            manager.Burst(new Vector2(1310f, -700f), 24);

            int wrongs = attempts - 1;
            int stars = (wrongs == 0 && hintsUsed == 0) ? 3 : ((wrongs <= 1 && hintsUsed <= 1) ? 2 : 1);
            float simM = d.SimMass, simF = d.SimForce;
            Action<float, float, bool> finished = (alt, vel, noLift) =>
            {
                int xp = Progress.Record(level, stars);
                RefreshChips();
                string body = (groundScene ? "Distance " : "Altitude ") + alt.ToString("0") + " m   ·   Speed " + vel.ToString("0.0") + " m/s\n" + FormatAnswer(d, ans);
                bool last = level >= Data.Count - 1;
                bool all = Progress.AllDone;
                manager.ShowResult(true, stars, xp, "MISSION COMPLETE!", body,
                    () => { if (all && last) manager.ShowCertificate(); else LoadLevel(Mathf.Min(level + 1, Data.Count - 1), false); },
                    () => LoadLevel(level, false), true, review);
                if (all && last) mentor.Cheer("Mission complete! Press NEXT to get your certificate.");
                busy = false;
                reset.interactable = true;
            };
            if (groundScene)
            {
                feedbackText.text = "Correct!  " + FormatAnswer(d, ans) + "  Go!";
                mentor.Cheer("Perfect answer! Watch the motion, cadet!");
                if (groundRoutine != null) StopCoroutine(groundRoutine);
                groundRoutine = StartCoroutine(RunGround(d, finished));
            }
            else sim.Launch(simM, simF, d.gravity, 7f, finished);
        }

        // ---------------------------------------------------------------
        // Missions 9-16: every mission has its own scene and motion
        // ---------------------------------------------------------------
        RectTransform R(GameObject g) { return g == null ? null : (RectTransform)g.transform; }

        void SetWheels(GameObject cart, bool on, float rotZ)
        {
            if (cart == null) return;
            foreach (var n in new[] { "WheelL", "WheelR" })
            {
                var w = cart.transform.Find(n);
                if (w != null) { w.gameObject.SetActive(on); w.localRotation = Quaternion.Euler(0, 0, rotZ); }
            }
        }

        void ApplyScene(int lv)
        {
            int k = lv - 8;
            bool g = k >= 0 && k < 8 && badgeSprites != null && badgeSprites.Length > k;
            if (!origCaptured && groundImage != null) { origGroundColor = groundImage.color; origCaptured = true; }
            if (groundRoutine != null) { StopCoroutine(groundRoutine); groundRoutine = null; }
            groundScene = g;
            if (sim != null) sim.enabled = !g;
            if (hideInGround != null) foreach (var o in hideInGround) if (o != null) o.SetActive(!g);
            bool space = g && SpaceScene[k];
            if (groundImage != null) { groundImage.gameObject.SetActive(!space); groundImage.color = g ? GroundTint[k] : origGroundColor; }
            if (sim != null && sim.clouds != null) foreach (var c in sim.clouds) if (c != null) c.gameObject.SetActive(!space);
            if (trackImage != null)
            {
                bool has = g && trackSprites != null && trackSprites.Length > k && trackSprites[k] != null;
                trackImage.gameObject.SetActive(has);
                if (has) trackImage.sprite = trackSprites[k];
            }
            if (missionBadge != null)
            {
                missionBadge.gameObject.SetActive(g);
                if (g) missionBadge.sprite = badgeSprites[k];
            }
            if (!g) return;

            if (sim.sky != null) sim.sky.color = SkyTint[k];
            if (sim.starsGroup != null) sim.starsGroup.alpha = space ? 1f : 0.15f;
            if (artFriction != null) artFriction.SetActive(false);
            bool carts = k == 0 || k == 1 || k >= 4;
            if (artCartA != null) artCartA.SetActive(carts);
            if (artCartB != null) artCartB.SetActive(k == 1);
            if (artAsteroid != null) artAsteroid.SetActive(space);
            if (artRamp != null) artRamp.SetActive(k >= 6);

            // starting positions
            var a = R(artCartA); var b = R(artCartB);
            if (a != null) { a.localRotation = Quaternion.identity; a.anchoredPosition = new Vector2(k == 1 ? 190f : 60f, GroundY); }
            if (b != null)
            {
                b.localRotation = Quaternion.identity; b.anchoredPosition = new Vector2(70f, GroundY);
                var link = artCartB.transform.Find("Link") as RectTransform;
                if (link != null) { link.pivot = Vector2.zero; link.anchoredPosition = new Vector2(100f, 24f); link.sizeDelta = new Vector2(20f, 6f); }
            }
            bool sled = k == 4 || k == 5;
            SetWheels(artCartA, !sled, 0f); SetWheels(artCartB, true, 0f);
            if (artCartA != null)
            {
                var body = artCartA.transform.Find("Body") as RectTransform; var rail = artCartA.transform.Find("Rail") as RectTransform;
                if (body != null) body.anchoredPosition = new Vector2(0f, sled ? 0f : 14f);
                if (rail != null) rail.anchoredPosition = new Vector2(0f, sled ? 28f : 42f);
            }
            var ramp = R(artRamp);
            if (ramp != null) ramp.anchoredPosition = new Vector2(RampX, GroundY);
            var ast = R(artAsteroid);
            if (ast != null && space)
            {
                ast.anchoredPosition = new Vector2(110f, 330f); ast.localRotation = Quaternion.identity;
                var rockRt = ast.Find("Rock") as RectTransform;
                if (rockRt != null)
                {
                    rockRt.anchorMin = rockRt.anchorMax = Vector2.zero; rockRt.pivot = new Vector2(0.5f, 0.5f);
                    rockRt.sizeDelta = new Vector2(192f, 137f); rockRt.anchoredPosition = new Vector2(96f, 68f); rockRt.localRotation = Quaternion.identity;
                }
                var rock = rockRt != null ? rockRt.GetComponent<Image>() : null;
                if (rock != null && asteroidSprites != null && asteroidSprites.Length > k - 2 && asteroidSprites[k - 2] != null) rock.sprite = asteroidSprites[k - 2];
            }
            if (trackImage != null)
            {
                var tr = (RectTransform)trackImage.transform;
                tr.anchorMin = tr.anchorMax = Vector2.zero; tr.pivot = Vector2.zero;
                tr.anchoredPosition = new Vector2(20f, 70f); tr.sizeDelta = new Vector2(1060f, 52f);
                trackImage.preserveAspect = false; trackImage.raycastTarget = false;
            }
            if (sim.statusText != null) sim.statusText.text = SceneNames[k] + (space ? "  ·  READY" : "  ·  READY");
            if (sim.countdownText != null) sim.countdownText.text = "";
            if (sim.altText != null) sim.altText.text = "DIST   0 m";
            if (sim.speedText != null) sim.speedText.text = "SPEED 0.0 m/s";
            if (sim.accText != null) sim.accText.text = "ACCEL " + Data[lv].SimAccel.ToString("0.00") + " m/s²";
            if (sim.timeText != null) sim.timeText.text = "TIME  0.0 s";
        }

        IEnumerator RunGround(ChallengeData d, Action<float, float, bool> done)
        {
            int k = level - 8;
            if (sim.statusText != null) sim.statusText.text = "COUNTDOWN";
            for (int n = 3; n >= 1; n--)
            {
                if (sim.countdownText != null) { sim.countdownText.text = n.ToString(); StartCoroutine(Fx.Pop(sim.countdownText.transform, 1.6f, 0.45f)); }
                Sfx.Play(Snd.Beep);
                yield return new WaitForSecondsRealtime(0.75f);
            }
            if (sim.countdownText != null) { sim.countdownText.text = "GO!"; StartCoroutine(Fx.Pop(sim.countdownText.transform, 1.8f, 0.5f)); }
            Sfx.Play(Snd.Go);
            if (sim.statusText != null) sim.statusText.text = SpaceScene[k] ? "THRUSTERS FIRING!" : "MOVING!";

            float acc = Mathf.Max(0.01f, d.SimAccel);
            const float T = 6f;
            bool ramp = k >= 6, space = SpaceScene[k];
            float maxPx = ramp ? 440f : (space ? 760f : 700f);
            float scale = Mathf.Min(PxPerMeter[k], maxPx / (0.5f * acc * T * T));
            float rampStart = RampX - 60f;   // path length on flat ground before the ramp
            var a = R(artCartA); var b = R(artCartB); var ast = R(artAsteroid);
            Vector2 a0 = a != null ? a.anchoredPosition : Vector2.zero, b0 = b != null ? b.anchoredPosition : Vector2.zero, s0 = ast != null ? ast.anchoredPosition : Vector2.zero;
            float t = 0f, dist = 0f, vel = 0f;
            while (t < T)
            {
                t += Time.deltaTime;
                if (t > T) t = T;
                dist = 0.5f * acc * t * t; vel = acc * t;
                float s = dist * scale;
                if (space && ast != null)
                {
                    ast.anchoredPosition = s0 + new Vector2(s, Mathf.Sin(t * 1.3f) * 6f);
                    var rk = ast.Find("Rock");
                    if (rk != null) rk.localRotation = Quaternion.Euler(0, 0, -s * 0.05f);
                }
                else if (a != null)
                {
                    if (ramp && s > rampStart)
                    {
                        float u = s - rampStart;
                        a.anchoredPosition = new Vector2(RampX + u * Mathf.Cos(RampDeg * Mathf.Deg2Rad), GroundY + u * Mathf.Sin(RampDeg * Mathf.Deg2Rad));
                        a.localRotation = Quaternion.Euler(0, 0, RampDeg);
                    }
                    else a.anchoredPosition = a0 + new Vector2(s, 0f);
                    if (b != null && artCartB.activeSelf) b.anchoredPosition = b0 + new Vector2(s, 0f);
                    float spin = -s / 13f * Mathf.Rad2Deg;
                    if (k != 4 && k != 5) { SetWheels(artCartA, true, spin); if (b != null) SetWheels(artCartB, true, spin); }
                }
                if (sim.altText != null) sim.altText.text = "DIST   " + dist.ToString("0.0") + " m";
                if (sim.speedText != null) sim.speedText.text = "SPEED " + vel.ToString("0.0") + " m/s";
                if (sim.accText != null) sim.accText.text = "ACCEL " + acc.ToString("0.00") + " m/s²";
                if (sim.timeText != null) sim.timeText.text = "TIME  " + t.ToString("0.0") + " s";
                yield return null;
            }
            if (sim.countdownText != null) sim.countdownText.text = "";
            if (sim.statusText != null) sim.statusText.text = "MISSION RUN COMPLETE";
            groundRoutine = null;
            done(dist, vel, false);
        }

        // True when the player ignored the friction / ramp pull-back (the classic mistake in missions 13-16).
        static bool NearNaive(ChallengeData d, float val)
        {
            float naive;
            if (d.unknown == Unknown.Accel) naive = d.F / d.m;
            else if (d.unknown == Unknown.Force) naive = d.m * d.a;
            else naive = d.F / d.a;
            return Mathf.Abs(val - naive) <= Mathf.Max(Mathf.Abs(naive) * 0.01f, 0.02f);
        }

        static string FormatAnswer(ChallengeData d, float ans)
        {
            string sym = d.unknown == Unknown.Mass ? "m" : d.unknown == Unknown.Force ? "F" : "a";
            return sym + " = " + ans.ToString("0.##", CultureInfo.InvariantCulture) + " " + d.Unit;
        }
    }
}
