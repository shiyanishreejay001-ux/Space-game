using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceAcademyV2
{
    // 2D rocket launch stage with real F = m*a motion (optionally with gravity).
    public class RocketSim : MonoBehaviour
    {
        [Header("Stage pieces")]
        public RectTransform stage;      // viewport (masked)
        public RectTransform world;      // pad, tower, ground... (scrolls down while climbing)
        public RectTransform rocket;     // rocket root (pivot bottom-center)
        public RectTransform flame;      // pivot top-center
        public Image flameImage;
        public RectTransform clampL, clampR, arm;
        public RectTransform[] clouds;
        public Image sky;
        public CanvasGroup starsGroup;
        public RectTransform marker;     // altitude marker beside the ruler
        public RectTransform padPoint;   // where smoke starts (inside world)

        [Header("Frames")]
        public Sprite[] flameSmall;
        public Sprite[] flameBig;

        [Header("HUD")]
        public TMP_Text altText, speedText, accText, timeText;
        public TMP_Text countdownText, statusText;

        [Header("Tuning")]
        public float rocketStartY = 178f;
        public float padOffset = -2f;
        public float flameTopPad = 45f;   // transparent px at top of flame sprites (so the nozzle ring meets the rocket base)
        public float flameAttach = 6f;    // how far the nozzle ring sits above the rocket's base
        public float flameMaxScale = 0.42f;    // rocket sits on the pad deck (negative = sinks slightly into it)
        public float maxRise = 120f;
        public float scrollPerMeter = 1.2f;
        public float rulerBottom = 110f, rulerHeight = 420f, markerX = 945f;
        public Color skyLow = new Color(0.30f, 0.55f, 0.95f), skyHigh = new Color(0.02f, 0.03f, 0.09f);

        enum Phase { Idle, Count, Flight, Done }
        Phase phase = Phase.Idle;

        float t, alt, vel, acc, mass, thrust, duration;
        bool grav, noLift;
        Action<float, float, bool> onDone;

        Vector2 clampLBase, clampRBase, armBase;
        float[] cloudSpeed;
        float shakeAmt;
        float stageW = 1100f, stageH = 655f;

        class Puff { public RectTransform rt; public Image im; public Vector2 v; public float age, life; }
        readonly System.Collections.Generic.List<Puff> puffs = new System.Collections.Generic.List<Puff>();
        float trailTimer;

        public bool Busy { get { return phase == Phase.Count || phase == Phase.Flight; } }
        public float Altitude { get { return alt; } }
        public float Velocity { get { return vel; } }

        void Awake()
        {
            if (clampL != null) clampLBase = clampL.anchoredPosition;
            if (clampR != null) clampRBase = clampR.anchoredPosition;
            if (arm != null) armBase = arm.anchoredPosition;
            if (stage != null) { stageW = stage.rect.width; stageH = stage.rect.height; }
            if (clouds != null)
            {
                cloudSpeed = new float[clouds.Length];
                for (int i = 0; i < clouds.Length; i++) cloudSpeed[i] = 0.6f + 0.5f * i;
            }
            SnapToPad();
            ResetRocket();
        }

        // Keeps the rocket standing exactly on the pad, whatever the pad's position/size in the scene.
        void SnapToPad()
        {
            if (world == null) return;
            var pad = world.Find("Pad") as RectTransform;
            if (pad == null) return;
            rocketStartY = pad.anchoredPosition.y + pad.sizeDelta.y * (1f - pad.pivot.y) + padOffset;
            if (padPoint != null)
            {
                var pp = padPoint.anchoredPosition; pp.y = rocketStartY; padPoint.anchoredPosition = pp;
            }
        }

        void OnDisable() { Sfx.StopEngine(); }

        public void ResetRocket()
        {
            StopAllCoroutines();
            Sfx.StopEngine();
            phase = Phase.Idle;
            t = alt = vel = acc = 0f;
            noLift = false;
            shakeAmt = 0f; trailTimer = 0f; puffs.Clear();
            if (clampL != null) { clampL.anchoredPosition = clampLBase; clampL.localRotation = Quaternion.identity; }
            if (clampR != null) { clampR.anchoredPosition = clampRBase; clampR.localRotation = Quaternion.identity; }
            if (arm != null) arm.anchoredPosition = armBase;
            if (flame != null) flame.gameObject.SetActive(false);
            if (countdownText != null) countdownText.text = "";
            if (statusText != null) statusText.text = "READY ON THE PAD";
            if (world != null)
                for (int i = world.childCount - 1; i >= 0; i--)
                    if (world.GetChild(i).name == "fx") Destroy(world.GetChild(i).gameObject);
            Apply(0f);
        }

        // m kg, F newtons, optional gravity, duration seconds of flight.
        public void Launch(float m, float F, bool gravity, float flightSeconds, Action<float, float, bool> done)
        {
            ResetRocket();
            mass = Mathf.Max(0.01f, m); thrust = Mathf.Max(0f, F); grav = gravity; duration = flightSeconds; onDone = done;
            noLift = grav && thrust <= mass * ChallengeData.G;
            StartCoroutine(Sequence());
        }

        IEnumerator Sequence()
        {
            phase = Phase.Count;
            if (statusText != null) statusText.text = "COUNTDOWN";
            for (int n = 3; n >= 1; n--)
            {
                if (countdownText != null) { countdownText.text = n.ToString(); StartCoroutine(Fx.Pop(countdownText.transform, 1.6f, 0.45f)); }
                Sfx.Play(Snd.Beep);
                yield return new WaitForSecondsRealtime(0.75f);
            }
            if (countdownText != null) { countdownText.text = "GO!"; StartCoroutine(Fx.Pop(countdownText.transform, 1.8f, 0.5f)); }
            Sfx.Play(Snd.Go);
            Sfx.StartEngine();
            if (flame != null) flame.gameObject.SetActive(true);
            phase = Phase.Flight;
            t = 0f;
            if (statusText != null) statusText.text = noLift ? "THRUST TOO LOW!" : "LIFTOFF!";
            StartCoroutine(Fx.Run(0.6f, k =>
            {
                if (clampL != null) { clampL.anchoredPosition = clampLBase + new Vector2(-70f * k, -6f * k); clampL.localRotation = Quaternion.Euler(0, 0, 25f * k); }
                if (clampR != null) { clampR.anchoredPosition = clampRBase + new Vector2(70f * k, -6f * k); clampR.localRotation = Quaternion.Euler(0, 0, -25f * k); }
            }));
            StartCoroutine(Fx.Run(1.0f, k => { if (arm != null) arm.anchoredPosition = armBase + new Vector2(-150f * Fx.EaseOutCubic(k), 0f); }));
            StartCoroutine(SmokeAndRing());
            yield return new WaitForSecondsRealtime(0.8f);
            if (countdownText != null) countdownText.text = "";
        }

        IEnumerator SmokeAndRing()
        {
            if (world == null || padPoint == null) yield break;
            var ring = new GameObject("fx", typeof(RectTransform), typeof(Image));
            ring.transform.SetParent(world, false);
            var rrt = (RectTransform)ring.transform;
            rrt.anchorMin = rrt.anchorMax = Vector2.zero; rrt.pivot = new Vector2(0.5f, 0.5f);
            rrt.anchoredPosition = padPoint.anchoredPosition; rrt.sizeDelta = new Vector2(300, 90);
            var rim = ring.GetComponent<Image>(); rim.sprite = Art.Ring; rim.raycastTarget = false;

            int n = noLift ? 8 : 18;
            var puffs = new RectTransform[n]; var pim = new Image[n]; var pv = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("fx", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(world, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = padPoint.anchoredPosition + new Vector2(UnityEngine.Random.Range(-40f, 40f), UnityEngine.Random.Range(0f, 15f));
                rt.sizeDelta = Vector2.one * UnityEngine.Random.Range(50f, 90f);
                var im = go.GetComponent<Image>(); im.sprite = Art.Circle; im.raycastTarget = false;
                im.color = new Color(0.92f, 0.92f, 0.95f, 0.8f);
                puffs[i] = rt; pim[i] = im;
                pv[i] = new Vector2(UnityEngine.Random.Range(-260f, 260f), UnityEngine.Random.Range(10f, 80f));
            }
            float life = 1.8f, tt = 0f;
            while (tt < life)
            {
                tt += Time.deltaTime;
                float k = tt / life;
                if (rrt != null) { rrt.localScale = Vector3.one * Mathf.Lerp(0.3f, 4f, Fx.EaseOutCubic(k)); var rc = rim.color; rc.a = 1f - k; rim.color = rc; }
                for (int i = 0; i < n; i++)
                {
                    if (puffs[i] == null) continue;
                    puffs[i].anchoredPosition += pv[i] * Time.deltaTime;
                    pv[i] *= (1f - 1.5f * Time.deltaTime);
                    puffs[i].localScale = Vector3.one * Mathf.Lerp(0.6f, 2.6f, k);
                    var c = pim[i].color; c.a = 0.8f * (1f - k); pim[i].color = c;
                }
                yield return null;
            }
            if (ring != null) Destroy(ring);
            for (int i = 0; i < n; i++) if (puffs[i] != null) Destroy(puffs[i].gameObject);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (phase == Phase.Flight)
            {
                t += dt;
                if (noLift)
                {
                    acc = 0f; vel = 0f; alt = 0f; shakeAmt = 1f;
                }
                else
                {
                    acc = grav ? (thrust / mass - ChallengeData.G) : (thrust / mass);
                    alt += vel * dt + 0.5f * acc * dt * dt;
                    vel += acc * dt;
                    shakeAmt = Mathf.Lerp(0.6f, 0.15f, Mathf.Clamp01(t / 3f));
                }
                float dur = noLift ? 3f : duration;
                if (t >= dur) Finish();
            }
            else
            {
                shakeAmt = Mathf.MoveTowards(shakeAmt, 0f, dt * 2f);
                if (phase == Phase.Count) shakeAmt = Mathf.Max(shakeAmt, 0.12f);   // pre-ignition rumble
            }
            Apply(dt);
            UpdateTrail(dt);
        }

        void UpdateTrail(float dt)
        {
            if (world == null || rocket == null) return;
            if (phase == Phase.Flight && !noLift)
            {
                trailTimer -= dt;
                if (trailTimer <= 0f) { trailTimer = 0.035f; SpawnPuff(); }
            }
            for (int i = puffs.Count - 1; i >= 0; i--)
            {
                var p = puffs[i];
                if (p.rt == null) { puffs.RemoveAt(i); continue; }
                p.age += dt;
                float k = p.age / p.life;
                if (k >= 1f) { Destroy(p.rt.gameObject); puffs.RemoveAt(i); continue; }
                p.rt.anchoredPosition += p.v * dt;
                p.rt.localScale = Vector3.one * Mathf.Lerp(0.5f, 2.2f, k);
                var c = p.im.color; c.a = 0.55f * (1f - k); p.im.color = c;
            }
        }

        void SpawnPuff()
        {
            var go = new GameObject("fx", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(world, false);
            go.transform.SetAsFirstSibling();   // behind pad/tower, never over the rocket
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.one * UnityEngine.Random.Range(28f, 50f);
            var rp = rocket.anchoredPosition;
            rt.anchoredPosition = new Vector2(rp.x + UnityEngine.Random.Range(-12f, 12f), rp.y - world.anchoredPosition.y - 34f);
            var im = go.GetComponent<Image>(); im.sprite = Art.Circle; im.raycastTarget = false;
            float g = UnityEngine.Random.Range(0.82f, 0.95f); im.color = new Color(g, g, g, 0.55f);
            puffs.Add(new Puff { rt = rt, im = im, v = new Vector2(UnityEngine.Random.Range(-45f, 45f), UnityEngine.Random.Range(-70f, -25f)), life = UnityEngine.Random.Range(0.9f, 1.4f) });
        }

        void Finish()
        {
            phase = Phase.Done;
            Sfx.StopEngine();
            if (flame != null) flame.gameObject.SetActive(false);
            if (statusText != null) statusText.text = noLift ? "NO LIFTOFF" : "ENGINE CUT-OFF";
            if (onDone != null) { var cb = onDone; onDone = null; cb(alt, vel, noLift); }
        }

        void Apply(float dt)
        {
            float rise = maxRise * (1f - Mathf.Exp(-alt / 120f));
            float jitter = shakeAmt * 3.2f;
            if (rocket != null)
            {
                rocket.anchoredPosition = new Vector2(stageW * 0.5f + (shakeAmt > 0f ? Mathf.Sin(Time.time * 85f) * jitter : 0f), rocketStartY + rise);
                rocket.localRotation = Quaternion.Euler(0, 0, shakeAmt > 0f ? Mathf.Sin(Time.time * 63f) * 0.5f * shakeAmt : 0f);
            }
            if (world != null)
            {
                Vector2 cam = shakeAmt > 0f ? new Vector2(Mathf.Sin(Time.time * 71f), Mathf.Cos(Time.time * 93f)) * shakeAmt * 3f : Vector2.zero;
                world.anchoredPosition = new Vector2(cam.x, -alt * scrollPerMeter + cam.y);
            }

            if (flame != null && flame.gameObject.activeSelf && flameImage != null)
            {
                float ratio = grav ? thrust / Mathf.Max(0.01f, mass * ChallengeData.G) : Mathf.Max(acc, 1f) / 6f;
                bool big = ratio > 2.2f;
                var set = big && flameBig != null && flameBig.Length > 0 ? flameBig : flameSmall;
                if (set != null && set.Length > 0)
                {
                    int idx = (int)(Time.time * 14f) % set.Length;
                    flameImage.sprite = set[idx];
                    Vector2 native = new Vector2(set[idx].rect.width, set[idx].rect.height);
                    float s = Mathf.Clamp(0.30f + ratio * 0.025f, 0.34f, flameMaxScale) * (noLift ? 0.6f : 1f);
                    flame.sizeDelta = native * s;
                    flame.anchoredPosition = new Vector2(flame.anchoredPosition.x, flameAttach + flameTopPad * s);
                }
            }

            if (sky != null) sky.color = Color.Lerp(skyLow, skyHigh, Mathf.Clamp01(alt / 700f));
            if (starsGroup != null) starsGroup.alpha = Mathf.InverseLerp(150f, 600f, alt);

            if (clouds != null && dt > 0f && cloudSpeed != null)
            {
                for (int i = 0; i < clouds.Length; i++)
                {
                    if (clouds[i] == null) continue;
                    var p = clouds[i].anchoredPosition;
                    p.y -= (vel * 1.4f + 3f) * cloudSpeed[i] * dt;
                    p.x += 6f * cloudSpeed[i] * dt;
                    if (p.y < -160f) { p.y = stageH + UnityEngine.Random.Range(60f, 260f); p.x = UnityEngine.Random.Range(60f, stageW - 200f); }
                    if (p.x > stageW + 200f) p.x = -200f;
                    clouds[i].anchoredPosition = p;
                    var im = clouds[i].GetComponent<Image>();
                    if (im != null) { var c = im.color; c.a = 1f - Mathf.InverseLerp(250f, 550f, alt); im.color = c; }
                }
            }

            if (marker != null)
                marker.anchoredPosition = new Vector2(markerX, rulerBottom + rulerHeight * (0.07f + 0.86f * Mathf.Clamp01(alt / 1000f)));

            if (altText != null) altText.text = "ALT   " + alt.ToString("0") + " m";
            if (speedText != null) speedText.text = "SPEED " + vel.ToString("0.0") + " m/s";
            if (accText != null) accText.text = "ACCEL " + acc.ToString("0.00") + " m/s²";
            if (timeText != null) timeText.text = "TIME  " + t.ToString("0.0") + " s";
        }
    }
}
