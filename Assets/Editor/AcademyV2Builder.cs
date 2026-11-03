#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpaceAcademyV2;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class AcademyV2Builder
{
    static readonly Dictionary<string, string> kw = new Dictionary<string, string>
    {
        {"LP","Launchpad"},{"RP","Rocket Parts"},{"FL","Thruster Flame"},{"MN","Robot Mentor"},{"GL","Gravity Lab"},
        {"BT","Button State"},{"UE","Sci-Fi UI Elements"},{"GU","Game UI Asset Sheet"},{"HD","HUD UI Asset Sheet"},
        {"CR","Control Room"},{"HI","HUD Interface Set"},{"IG","Icon Grid"},{"GI","Game Icon Set"}
    };
    static readonly Dictionary<string, Dictionary<string, Sprite>> cache = new Dictionary<string, Dictionary<string, Sprite>>();

    static Sprite S(string key, int i)
    {
        if (!cache.ContainsKey(key))
        {
            var file = Directory.GetFiles("Assets/UI/images", "*.png").First(f => f.Contains(kw[key])).Replace('\\', '/');
            cache[key] = AssetDatabase.LoadAllAssetsAtPath(file).OfType<Sprite>().ToDictionary(s => s.name, s => s);
        }
        var d = cache[key];
        var name = d.Keys.First(k => k.EndsWith("_" + i));
        return d[name];
    }

    static readonly Color Cyan = new Color(0.35f, 0.9f, 1f);
    static readonly Color Orange = new Color(1f, 0.62f, 0.11f);
    static readonly Color White = Color.white;

    // ---------------- helpers ----------------
    static RectTransform NewRT(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }
    static RectTransform TL(string name, Transform parent, float x, float y, float w, float h)
    {
        var rt = NewRT(name, parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
        return rt;
    }
    static RectTransform BL(string name, Transform parent, float x, float y, float w, float h, Vector2 pivot)
    {
        var rt = NewRT(name, parent);
        rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = pivot;
        rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h);
        return rt;
    }
    static RectTransform Fill(string name, Transform parent)
    {
        var rt = NewRT(name, parent);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }
    static Image Im(RectTransform rt, Sprite s, Color c, bool raycast = false)
    {
        var im = rt.gameObject.AddComponent<Image>();
        im.sprite = s; im.color = c; im.raycastTarget = raycast;
        return im;
    }
    static TMP_Text Tx(Transform parent, string name, string text, float size, Color col, TextAlignmentOptions al, float x, float y, float w, float h, bool bold = false)
    {
        var rt = TL(name, parent, x, y, w, h);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = col; t.alignment = al; t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Overflow;
        if (bold) t.fontStyle = FontStyles.Bold;
        return t;
    }
    static Button Btn(string name, Transform parent, string key, int n, int hv, int pr, int ds, float x, float y, float w, float h)
    {
        var rt = TL(name, parent, x, y, w, h);
        var im = Im(rt, S(key, n), White, true);
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = im; b.transition = Selectable.Transition.SpriteSwap;
        var ss = new SpriteState { highlightedSprite = S(key, hv), pressedSprite = S(key, pr), selectedSprite = S(key, n), disabledSprite = S(key, ds) };
        b.spriteState = ss;
        rt.gameObject.AddComponent<ButtonFx>();
        return b;
    }
    static Slider Sld(string name, Transform parent, float x, float y, float w, float min, float max, float val)
    {
        var rt = TL(name, parent, x, y, w, 36);
        var s = rt.gameObject.AddComponent<Slider>();
        var bg = NewRT("Background", rt);
        bg.anchorMin = new Vector2(0, 0.5f); bg.anchorMax = new Vector2(1, 0.5f); bg.sizeDelta = new Vector2(0, 10); bg.anchoredPosition = Vector2.zero;
        Im(bg, null, new Color(0.04f, 0.14f, 0.28f, 1f));
        var fa = NewRT("Fill Area", rt);
        fa.anchorMin = new Vector2(0, 0.5f); fa.anchorMax = new Vector2(1, 0.5f); fa.offsetMin = new Vector2(8, -5); fa.offsetMax = new Vector2(-8, 5);
        var fill = NewRT("Fill", fa); fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0, 1); fill.sizeDelta = new Vector2(10, 0);
        Im(fill, null, new Color(0.1f, 0.85f, 1f, 1f));
        var sa = NewRT("Handle Slide Area", rt);
        sa.anchorMin = Vector2.zero; sa.anchorMax = Vector2.one; sa.offsetMin = new Vector2(17, 0); sa.offsetMax = new Vector2(-17, 0);
        var hd = NewRT("Handle", sa); hd.anchorMin = hd.anchorMax = new Vector2(0, 0.5f); hd.sizeDelta = new Vector2(36, 36);
        var him = Im(hd, S("HD", 7), White, true);
        s.fillRect = fill; s.handleRect = hd; s.targetGraphic = him; s.direction = Slider.Direction.LeftToRight;
        s.minValue = min; s.maxValue = max; s.wholeNumbers = true; s.value = val;
        return s;
    }
    static TMP_InputField Inp(string name, Transform parent, float x, float y, float w, float h, out TMP_Text placeholder)
    {
        var rt = TL(name, parent, x, y, w, h);
        var im = Im(rt, S("HD", 3), new Color(1f, 1f, 1f, 1f), true);
        var f = rt.gameObject.AddComponent<TMP_InputField>();
        var vp = NewRT("Text Area", rt); vp.anchorMin = Vector2.zero; vp.anchorMax = Vector2.one; vp.offsetMin = new Vector2(22, 6); vp.offsetMax = new Vector2(-22, -6);
        vp.gameObject.AddComponent<RectMask2D>();
        var ph = NewRT("Placeholder", vp); ph.anchorMin = Vector2.zero; ph.anchorMax = Vector2.one; ph.offsetMin = ph.offsetMax = Vector2.zero;
        var pt = ph.gameObject.AddComponent<TextMeshProUGUI>(); pt.text = ""; pt.fontSize = 24; pt.fontStyle = FontStyles.Italic; pt.color = new Color(0.6f, 0.75f, 0.9f, 0.9f); pt.alignment = TextAlignmentOptions.MidlineLeft; pt.raycastTarget = false;
        var tr = NewRT("Text", vp); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = tr.offsetMax = Vector2.zero;
        var tt = tr.gameObject.AddComponent<TextMeshProUGUI>(); tt.fontSize = 28; tt.color = White; tt.alignment = TextAlignmentOptions.MidlineLeft; tt.raycastTarget = false;
        f.textViewport = vp; f.textComponent = tt; f.placeholder = pt; f.targetGraphic = im;
        f.characterLimit = 12; f.lineType = TMP_InputField.LineType.SingleLine;
        placeholder = pt;
        return f;
    }
    static RectTransform Panel(string name, Transform parent, out CanvasGroup cg)
    {
        var rt = Fill(name, parent);
        cg = rt.gameObject.AddComponent<CanvasGroup>();
        return rt;
    }

    // ---------------- rocket stage (used by Challenge + Experiment) ----------------
    static RocketSim BuildStage(Transform panel)
    {
        // HUD readout bar
        var bar = TL("Readout", panel, 760, 205, 1100, 100);
        Im(bar, S("HI", 4), White);
        var alt = Tx(bar, "Alt", "ALT   0 m", 27, Cyan, TextAlignmentOptions.MidlineLeft, 40, 0, 270, 100, true);
        var spd = Tx(bar, "Speed", "SPEED 0.0 m/s", 27, White, TextAlignmentOptions.MidlineLeft, 300, 0, 290, 100, true);
        var acc = Tx(bar, "Accel", "ACCEL 0.00 m/s²", 27, Orange, TextAlignmentOptions.MidlineLeft, 600, 0, 300, 100, true);
        var tim = Tx(bar, "Time", "TIME  0.0 s", 27, White, TextAlignmentOptions.MidlineLeft, 905, 0, 190, 100, true);

        var frame = TL("StageFrame", panel, 756, 311, 1108, 663);
        Im(frame, null, new Color(0.2f, 0.85f, 1f, 0.9f));
        var st = TL("Stage", panel, 760, 315, 1100, 655);
        st.gameObject.AddComponent<RectMask2D>();
        var sim = st.gameObject.AddComponent<RocketSim>();
        sim.stage = st;

        var sky = Fill("Sky", st); sim.sky = Im(sky, null, new Color(0.30f, 0.55f, 0.95f));
        var starsRT = Fill("Stars", st); var cg = starsRT.gameObject.AddComponent<CanvasGroup>(); cg.alpha = 0f; cg.blocksRaycasts = false;
        var sf = starsRT.gameObject.AddComponent<StarField>(); sf.count = 55; sf.area = new Vector2(1100, 655);
        sim.starsGroup = cg;

        var clouds = new List<RectTransform>();
        float[,] cd = { { 70, 430, 260, 68, 7 }, { 520, 520, 300, 60, 8 }, { 780, 330, 240, 47, 9 } };
        for (int i = 0; i < 3; i++)
        {
            var c = BL("Cloud" + i, st, cd[i, 0], cd[i, 1], cd[i, 2], cd[i, 3], Vector2.zero);
            Im(c, S("LP", (int)cd[i, 4]), White);
            clouds.Add(c);
        }
        sim.clouds = clouds.ToArray();

        // world (scrolls)
        var world = BL("World", st, 0, 0, 1100, 655, Vector2.zero);
        sim.world = world;
        Im(BL("Ground", world, 0, 0, 1100, 118, Vector2.zero), S("LP", 10), White);
        var arm = BL("Arm", world, 440, 330, 110, 30, new Vector2(0, 0.5f)); Im(arm, S("LP", 2), White); sim.arm = arm;
        Im(BL("Tower", world, 330, 112, 242, 480, new Vector2(0.5f, 0)), S("LP", 1), White);
        Im(BL("Pad", world, 550, 108, 380, 68, new Vector2(0.5f, 0)), S("LP", 0), White);
        var cl = BL("ClampL", world, 484, 235, 56, 48, new Vector2(0.5f, 0.5f)); Im(cl, S("LP", 5), White); sim.clampL = cl;
        var cr = BL("ClampR", world, 616, 235, 48, 52, new Vector2(0.5f, 0.5f)); Im(cr, S("LP", 6), White); sim.clampR = cr;
        var pp = BL("PadPoint", world, 550, 178, 4, 4, new Vector2(0.5f, 0.5f)); sim.padPoint = pp;

        // rocket
        var rk = BL("Rocket", st, 550, 178, 100, 334, new Vector2(0.5f, 0)); sim.rocket = rk;
        var fl = NewRT("Flame", rk); fl.anchorMin = fl.anchorMax = new Vector2(0.5f, 0); fl.pivot = new Vector2(0.5f, 1); fl.anchoredPosition = new Vector2(0, 12); fl.sizeDelta = new Vector2(100, 100);
        var fim = Im(fl, S("FL", 4), White); sim.flame = fl; sim.flameImage = fim; fl.gameObject.SetActive(false);
        var finL = NewRT("FinL", rk); finL.anchorMin = finL.anchorMax = new Vector2(0.5f, 0); finL.pivot = new Vector2(1, 0); finL.anchoredPosition = new Vector2(-40, 2); finL.sizeDelta = new Vector2(52, 72); Im(finL, S("RP", 0), White);
        var finR = NewRT("FinR", rk); finR.anchorMin = finR.anchorMax = new Vector2(0.5f, 0); finR.pivot = new Vector2(0, 0); finR.anchoredPosition = new Vector2(40, 2); finR.sizeDelta = new Vector2(52, 72); Im(finR, S("RP", 2), White);
        var body = NewRT("Body", rk); body.anchorMin = body.anchorMax = new Vector2(0.5f, 0); body.pivot = new Vector2(0.5f, 0); body.anchoredPosition = Vector2.zero; body.sizeDelta = new Vector2(100, 230); Im(body, S("RP", 1), White);
        var nose = NewRT("Nose", rk); nose.anchorMin = nose.anchorMax = new Vector2(0.5f, 0); nose.pivot = new Vector2(0.5f, 0); nose.anchoredPosition = new Vector2(0, 226); nose.sizeDelta = new Vector2(106, 104); Im(nose, S("RP", 3), White);

        // ruler + marker
        Im(BL("Ruler", st, 1020, 110, 123, 420, new Vector2(0.5f, 0)), S("LP", 3), White);
        var mk = BL("Marker", st, 945, 150, 44, 9, new Vector2(0, 0.5f)); Im(mk, S("FL", 2), White); sim.marker = mk;

        sim.statusText = Tx(st, "Status", "READY ON THE PAD", 26, Orange, TextAlignmentOptions.TopLeft, 20, 14, 520, 40, true);
        sim.countdownText = Tx(st, "Countdown", "", 130, White, TextAlignmentOptions.Center, 300, 130, 500, 180, true);
        sim.altText = alt; sim.speedText = spd; sim.accText = acc; sim.timeText = tim;

        sim.flameSmall = new[] { S("FL", 3), S("FL", 4), S("FL", 5), S("FL", 6), S("FL", 7), S("FL", 8) };
        sim.flameBig = new[] { S("FL", 9), S("FL", 10), S("FL", 11), S("FL", 12), S("FL", 13), S("FL", 14) };
        return sim;
    }

    // ---------------- main build ----------------
    [MenuItem("Tools/Space Academy V2/Build Scene")]
    public static string Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        foreach (var n in new[] { "AcademyCanvas", "EventSystem", "AcademyManager" })
        { var o = GameObject.Find(n); if (o != null) Object.DestroyImmediate(o); }

        // canvas
        var cgo = new GameObject("AcademyCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = cgo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = cgo.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
        var root = (RectTransform)cgo.transform;
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var mgrGO = new GameObject("AcademyManager");
        var mgr = mgrGO.AddComponent<AcademyManager>();

        // backgrounds
        var bgs = Fill("Backgrounds", root);
        var bgGroups = new CanvasGroup[3];
        int[] bgIdx = { 4, 5, 6 };
        for (int i = 0; i < 3; i++)
        {
            var b = Fill("BG" + i, bgs); Im(b, S("CR", bgIdx[i]), White);
            bgGroups[i] = b.gameObject.AddComponent<CanvasGroup>(); bgGroups[i].alpha = i == 0 ? 1f : 0f; bgGroups[i].blocksRaycasts = false;
        }
        Im(Fill("Dim", bgs), null, new Color(0.01f, 0.03f, 0.1f, 0.55f));
        var starRT = Fill("StarOverlay", root); var sfo = starRT.gameObject.AddComponent<StarField>(); sfo.count = 90; sfo.area = new Vector2(1920, 1080);

        // mentor (created before panels so refs exist; re-ordered later)
        var mentorRoot = NewRT("MentorRoot", root); mentorRoot.anchorMin = Vector2.zero; mentorRoot.anchorMax = Vector2.one; mentorRoot.offsetMin = mentorRoot.offsetMax = Vector2.zero;
        var mentor = mentorRoot.gameObject.AddComponent<Mentor>();
        var mimg = TL("MentorImage", mentorRoot, 30, 852, 200, 228);
        var mim = Im(mimg, S("MN", 0), White); mim.preserveAspect = true;
        var bub = TL("Bubble", mentorRoot, 240, 858, 490, 205);
        Im(bub, S("MN", 7), White);
        var btxt = Tx(bub, "BubbleText", "", 23, new Color(0.05f, 0.12f, 0.28f), TextAlignmentOptions.TopLeft, 36, 24, 420, 120);
        btxt.enableAutoSizing = true; btxt.fontSizeMin = 14; btxt.fontSizeMax = 23;
        mentor.mentorImage = mim; mentor.mentorRect = mimg; mentor.bubble = bub; mentor.bubbleText = btxt;
        mentor.idleFrames = new[] { S("MN", 0), S("MN", 4), S("MN", 5), S("MN", 6) };
        mentor.talkFrames = new[] { S("MN", 1), S("MN", 8), S("MN", 9), S("MN", 10) };
        mentor.happy = S("MN", 2); mentor.sad = S("MN", 3);

        // ---- panels ----
        CanvasGroup g0, g1, g2;
        var pCh = Panel("Panel_Challenge", root, out g0);
        var pEx = Panel("Panel_Experiment", root, out g1);
        var pGr = Panel("Panel_Gravity", root, out g2);
        pCh.SetSiblingIndex(mentorRoot.GetSiblingIndex()); // panels below mentor

        // ===== Challenge =====
        var ch = pCh.gameObject.AddComponent<ChallengeController>();
        Im(TL("Card", pCh, 30, 205, 700, 640), S("HI", 1), White);
        var chips = new Button[16]; var chipImgs = new Image[16]; var chipLabels = new TMP_Text[16]; var chipLocks = new GameObject[16];
        for (int i = 0; i < 16; i++)   // 2 rows of 8 chips
        {
            var crt = TL("Chip" + i, pCh, 98 + (i % 8) * 74, 212 + (i / 8) * 50, 46, 46);
            chipImgs[i] = Im(crt, S("HD", 7), White, true);
            chips[i] = crt.gameObject.AddComponent<Button>(); chips[i].targetGraphic = chipImgs[i]; chips[i].transition = Selectable.Transition.None;
            var lk = TL("Lock", crt, 10, 10, 26, 26); Im(lk, S("GI", 2), White); chipLocks[i] = lk.gameObject;
            chipLabels[i] = Tx(crt, "Label", (i + 1).ToString(), 24, White, TextAlignmentOptions.Center, 0, 0, 46, 46, true);
        }
        ch.chips = chips; ch.chipImages = chipImgs; ch.chipLabels = chipLabels; ch.chipLocks = chipLocks;
        ch.chipOpen = S("HD", 7); ch.chipDone = S("HD", 9); ch.chipLockedSprite = S("HD", 8);
        ch.titleText = Tx(pCh, "Title", "MISSION", 28, Cyan, TextAlignmentOptions.MidlineLeft, 55, 322, 640, 40, true);
        ch.storyText = Tx(pCh, "Story", "", 23, White, TextAlignmentOptions.TopLeft, 55, 366, 620, 96);
        ch.formulaText = Tx(pCh, "Formula", "F = m × a", 26, Orange, TextAlignmentOptions.Center, 55, 464, 620, 36, true);
        ch.rowTexts = new TMP_Text[4]; ch.rows = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            var rt = Tx(pCh, "Row" + i, "", 24, White, TextAlignmentOptions.MidlineLeft, 55, 508 + 40 * i, 620, 38);
            rt.richText = true; ch.rowTexts[i] = rt; ch.rows[i] = rt.gameObject;
        }
        ch.feedbackText = Tx(pCh, "Feedback", "", 21, White, TextAlignmentOptions.TopLeft, 55, 671, 620, 62);
        TMP_Text ph;
        ch.input = Inp("Input", pCh, 55, 738, 360, 62, out ph); ch.placeholderText = ph;
        ch.submit = Btn("Submit", pCh, "BT", 0, 1, 2, 3, 430, 734, 190, 70);
        ch.hint = Btn("Hint", pCh, "BT", 12, 13, 14, 15, 55, 811, 190, 60);
        ch.reset = Btn("Reset", pCh, "BT", 16, 17, 18, 19, 255, 811, 190, 60);
        ch.sim = BuildStage(pCh);
        ch.manager = mgr; ch.mentor = mentor;

        // ===== Experiment =====
        var ex = pEx.gameObject.AddComponent<ExperimentController>();
        Im(TL("Card", pEx, 30, 205, 700, 640), S("HI", 1), White);
        Tx(pEx, "Title", "ROCKET LAB", 30, Cyan, TextAlignmentOptions.MidlineLeft, 55, 225, 640, 44, true);
        ex.massLabel = Tx(pEx, "MassLabel", "Mass", 26, White, TextAlignmentOptions.MidlineLeft, 55, 285, 620, 34, true);
        ex.massSlider = Sld("MassSlider", pEx, 55, 322, 620, 1, 40, 10);
        ex.thrustLabel = Tx(pEx, "ThrustLabel", "Thrust", 26, White, TextAlignmentOptions.MidlineLeft, 55, 380, 620, 34, true);
        ex.thrustSlider = Sld("ThrustSlider", pEx, 55, 417, 620, 1, 60, 20);
        ex.weightText = Tx(pEx, "Weight", "", 24, White, TextAlignmentOptions.MidlineLeft, 55, 480, 620, 36);
        ex.netText = Tx(pEx, "Net", "", 24, White, TextAlignmentOptions.MidlineLeft, 55, 518, 620, 36);
        ex.accText = Tx(pEx, "Acc", "", 24, Orange, TextAlignmentOptions.MidlineLeft, 55, 556, 620, 36);
        ex.twrText = Tx(pEx, "Twr", "", 24, White, TextAlignmentOptions.MidlineLeft, 55, 594, 620, 36);
        ex.verdictText = Tx(pEx, "Verdict", "", 26, White, TextAlignmentOptions.MidlineLeft, 55, 640, 620, 40, true);
        ex.feedbackText = Tx(pEx, "Feedback", "", 21, White, TextAlignmentOptions.TopLeft, 55, 690, 620, 60);
        ex.runButton = Btn("Run", pEx, "BT", 4, 5, 6, 7, 55, 755, 250, 90);
        ex.resetButton = Btn("Reset", pEx, "BT", 16, 17, 18, 19, 330, 768, 190, 72);
        ex.sim = BuildStage(pEx);
        ex.manager = mgr; ex.mentor = mentor;

        // ===== Gravity =====
        var gr = pGr.gameObject.AddComponent<GravityController>();
        Im(TL("Card", pGr, 30, 205, 700, 640), S("HI", 1), White);
        gr.planetButtons = new Button[3]; gr.planetImages = new Image[3];
        gr.planetActive = new[] { S("GU", 6), S("GU", 7), S("GU", 8) };
        gr.planetInactive = new[] { S("GU", 9), S("GU", 10), S("GU", 11) };
        for (int i = 0; i < 3; i++)
        {
            var prt = TL("Planet" + i, pGr, 55 + i * 140, 222, 125, 125);
            gr.planetImages[i] = Im(prt, gr.planetInactive[i], White, true);
            gr.planetButtons[i] = prt.gameObject.AddComponent<Button>(); gr.planetButtons[i].targetGraphic = gr.planetImages[i]; gr.planetButtons[i].transition = Selectable.Transition.None;
            prt.gameObject.AddComponent<ButtonFx>();
        }
        gr.planetTitle = Tx(pGr, "PlanetTitle", "EARTH", 28, Cyan, TextAlignmentOptions.MidlineLeft, 55, 355, 640, 40, true);
        gr.massLabel = Tx(pGr, "MassLabel", "Mass", 26, White, TextAlignmentOptions.MidlineLeft, 55, 400, 620, 34, true);
        gr.massSlider = Sld("MassSlider", pGr, 55, 438, 620, 1, 100, 10);
        gr.liftButton = Btn("Lift", pGr, "BT", 8, 9, 10, 11, 55, 485, 185, 66);
        gr.resetButton = Btn("Reset", pGr, "BT", 16, 17, 18, 19, 255, 485, 185, 66);
        gr.massRow = Tx(pGr, "MassRow", "", 26, White, TextAlignmentOptions.MidlineLeft, 55, 565, 320, 34);
        gr.gravRow = Tx(pGr, "GravRow", "", 26, White, TextAlignmentOptions.MidlineLeft, 55, 601, 320, 34);
        gr.weightRow = Tx(pGr, "WeightRow", "", 30, Orange, TextAlignmentOptions.MidlineLeft, 55, 639, 320, 40, true);
        gr.compareText = Tx(pGr, "Compare", "", 21, White, TextAlignmentOptions.TopLeft, 390, 555, 310, 130); gr.compareText.richText = true;
        gr.jumpText = Tx(pGr, "Jump", "", 23, White, TextAlignmentOptions.MidlineLeft, 55, 695, 640, 34);
        gr.liftText = Tx(pGr, "LiftText", "", 23, Cyan, TextAlignmentOptions.TopLeft, 55, 735, 640, 100);

        var gfr = TL("StageFrame", pGr, 756, 201, 1108, 773); Im(gfr, null, new Color(0.2f, 0.85f, 1f, 0.9f));
        var gst = TL("Stage", pGr, 760, 205, 1100, 765); gst.gameObject.AddComponent<RectMask2D>();
        var gsky = Fill("Sky", gst); gr.skyImage = Im(gsky, null, new Color(0.30f, 0.55f, 0.92f));
        var gStars = Fill("Stars", gst); var gcg = gStars.gameObject.AddComponent<CanvasGroup>(); gcg.alpha = 0f; gcg.blocksRaycasts = false;
        var gsf = gStars.gameObject.AddComponent<StarField>(); gsf.count = 60; gsf.area = new Vector2(1100, 765);
        gr.starsGroup = gcg;
        var surf = BL("Surface", gst, 0, 0, 1100, 300, Vector2.zero); gr.surfaceImage = Im(surf, S("GL", 13), White);
        gr.surfaceSprites = new[] { S("GL", 13), S("GL", 12), S("GL", 11) };
        Im(BL("LiftColumn", gst, 250, 205, 90, 420, new Vector2(0.5f, 0)), S("GL", 10), White);
        gr.platform = BL("Platform", gst, 250, 255, 300, 145, new Vector2(0.5f, 0)); Im(gr.platform, S("GL", 9), White);
        gr.crate = BL("Crate", gst, 250, 335, 120, 103, new Vector2(0.5f, 0)); Im(gr.crate, S("GL", 3), White);
        var lab = TL("CrateLabelBG", gr.crate, 20, 66, 80, 30); Im(lab, S("HI", 4), White);
        gr.crateLabel = Tx(lab, "CrateLabel", "10 kg", 20, White, TextAlignmentOptions.Center, 0, 0, 80, 30, true);
        gr.astronaut = BL("Astronaut", gst, 600, 215, 110, 213, new Vector2(0.5f, 0));
        var aim = Im(gr.astronaut, S("GL", 5), White, true);
        gr.astronautButton = gr.astronaut.gameObject.AddComponent<Button>(); gr.astronautButton.targetGraphic = aim; gr.astronautButton.transition = Selectable.Transition.None;
        gr.astronaut.gameObject.AddComponent<ButtonFx>();
        gr.astronautHint = Tx(gst, "AstronautHint", "TAP ME TO JUMP!", 22, Orange, TextAlignmentOptions.Center, 500, 765 - 215 - 213 - 60, 200, 30, true);
        Im(BL("Gauge", gst, 880, 620, 210, 209, new Vector2(0.5f, 0.5f)), S("GL", 8), White);
        gr.needle = BL("Needle", gst, 880, 620, 40, 92, new Vector2(0.5f, 0.12f)); Im(gr.needle, S("GL", 7), White);
        gr.gaugeText = Tx(gst, "GaugeText", "WEIGHT 0 N", 24, White, TextAlignmentOptions.Center, 760, 765 - 505, 240, 34, true);
        gr.spring = BL("Spring", gst, 880, 465, 90, 160, new Vector2(0.5f, 1)); Im(gr.spring, S("GL", 6), White);
        gr.miniCrate = BL("MiniCrate", gst, 880, 313, 60, 52, new Vector2(0.5f, 1)); Im(gr.miniCrate, S("GL", 3), White);
        gr.manager = mgr; gr.mentor = mentor;

        // ---- fx layer ----
        var fx = Fill("FxLayer", root);

        // ---- header ----
        var hdr = Fill("Header", root);
        Im(TL("Bar", hdr, 0, 0, 1920, 96), S("HI", 0), White);
        Tx(hdr, "Title", "SPACE ACADEMY", 44, Cyan, TextAlignmentOptions.MidlineLeft, 40, 8, 600, 80, true);
        var rank = Tx(hdr, "Rank", "CADET", 30, Orange, TextAlignmentOptions.MidlineRight, 640, 8, 340, 80, true);
        Im(TL("XPIcon", hdr, 1000, 17, 62, 62), S("GI", 3), White);
        var xp = Tx(hdr, "XP", "XP 0", 30, White, TextAlignmentOptions.MidlineLeft, 1070, 8, 200, 80, true);
        Im(TL("StarIcon", hdr, 1280, 17, 62, 62), S("GI", 0), White);
        var stars = Tx(hdr, "Stars", "0/48", 30, White, TextAlignmentOptions.MidlineLeft, 1350, 8, 200, 80, true);
        var back = Btn("Back", hdr, "BT", 23, 22, 21, 20, 1690, 12, 200, 76);
        back.gameObject.AddComponent<BackButton>();

        var tabs = new Button[3]; var tabImgs = new Image[3];
        int[] actI = { 0, 1, 2 }, inaI = { 5, 4, 3 };
        for (int i = 0; i < 3; i++)
        {
            var trt = TL("Tab" + i, hdr, 40 + i * 310, 100, 290, 98);
            tabImgs[i] = Im(trt, S("GU", i == 0 ? actI[0] : inaI[i]), White, true);
            tabs[i] = trt.gameObject.AddComponent<Button>(); tabs[i].targetGraphic = tabImgs[i]; tabs[i].transition = Selectable.Transition.None;
            trt.gameObject.AddComponent<ButtonFx>();
        }

        // ---- popup ----
        CanvasGroup popG;
        var pop = Panel("Popup", root, out popG);
        Im(Fill("Dim", pop), null, new Color(0, 0, 0, 0.65f), true);
        var pp = NewRT("PopupPanel", pop); pp.anchorMin = pp.anchorMax = new Vector2(0.5f, 0.5f); pp.pivot = new Vector2(0.5f, 0.5f); pp.sizeDelta = new Vector2(760, 500); pp.anchoredPosition = Vector2.zero;
        Im(pp, S("HI", 2), White, true);
        var pIcon = Im(TL("Icon", pp, 325, 22, 110, 110), S("IG", 5), White);
        var pTitle = Tx(pp, "Title", "MISSION COMPLETE!", 44, White, TextAlignmentOptions.Center, 30, 140, 700, 56, true);
        var pBody = Tx(pp, "Body", "", 26, White, TextAlignmentOptions.Top, 60, 200, 640, 80);
        var pStars = new Image[3];
        for (int i = 0; i < 3; i++) pStars[i] = Im(TL("Star" + i, pp, 225 + i * 110, 282, 90, 90), S("GI", 1), White);
        var pXP = Tx(pp, "XP", "", 34, Orange, TextAlignmentOptions.Center, 30, 372, 700, 40, true);
        var pRetry = Btn("Retry", pp, "BT", 16, 17, 18, 19, 170, 418, 190, 72);
        var pNext = Btn("Next", pp, "UE", 20, 19, 18, 17, 410, 412, 130, 84);

        // ---- certificate ----
        CanvasGroup certG;
        var cert = Panel("Certificate", root, out certG);
        Im(Fill("Dim", cert), null, new Color(0, 0, 0, 0.75f), true);
        var cp = NewRT("CertPanel", cert); cp.anchorMin = cp.anchorMax = new Vector2(0.5f, 0.5f); cp.pivot = new Vector2(0.5f, 0.5f); cp.sizeDelta = new Vector2(900, 619); cp.anchoredPosition = new Vector2(0, 30);
        Im(cp, S("MN", 11), White);
        var certText = Tx(cp, "CertText", "", 28, new Color(0.05f, 0.12f, 0.28f), TextAlignmentOptions.Center, 120, 345, 660, 50, true);
        var certClose = Btn("Close", cert, "BT", 23, 22, 21, 20, 860, 960, 200, 76);

        // ---- wire manager ----
        mgr.modePanels = new[] { pCh.gameObject, pEx.gameObject, pGr.gameObject };
        mgr.modeGroups = new[] { g0, g1, g2 };
        mgr.bgGroups = bgGroups;
        mgr.tabButtons = tabs; mgr.tabImages = tabImgs;
        mgr.tabActive = new[] { S("GU", 0), S("GU", 1), S("GU", 2) };
        mgr.tabInactive = new[] { S("GU", 5), S("GU", 4), S("GU", 3) };
        mgr.mentor = mentor; mgr.challenge = ch; mgr.experiment = ex; mgr.gravity = gr;
        mgr.xpText = xp; mgr.rankText = rank; mgr.starText = stars;
        mgr.fxLayer = fx; mgr.popupGroup = popG; mgr.popupPanel = pp;
        mgr.popupTitle = pTitle; mgr.popupBody = pBody; mgr.popupXP = pXP; mgr.popupIcon = pIcon; mgr.popupStars = pStars;
        mgr.checkSprite = S("IG", 5); mgr.crossSprite = S("IG", 6); mgr.starFull = S("GI", 0); mgr.starEmpty = S("GI", 1);
        mgr.popupNext = pNext; mgr.popupRetry = pRetry;
        mgr.certGroup = certG; mgr.certPanel = cp; mgr.certText = certText; mgr.certClose = certClose;

        // sibling order: backgrounds, stars, panels, mentor, fx, header, popup, cert
        bgs.SetAsFirstSibling();
        starRT.SetSiblingIndex(1);
        pCh.SetSiblingIndex(2); pEx.SetSiblingIndex(3); pGr.SetSiblingIndex(4);
        mentorRoot.SetSiblingIndex(5); fx.SetSiblingIndex(6); hdr.SetSiblingIndex(7); pop.SetSiblingIndex(8); cert.SetSiblingIndex(9);

        pEx.gameObject.SetActive(false); pGr.gameObject.SetActive(false);
        pop.gameObject.SetActive(false); cert.gameObject.SetActive(false);

        var cam = Camera.main;
        if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.01f, 0.02f, 0.06f); }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "built";
    }
}
#endif
