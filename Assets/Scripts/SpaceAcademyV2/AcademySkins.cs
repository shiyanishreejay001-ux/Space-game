using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceAcademyV2
{
    // One rocket skin: the four sprites the rocket is built from, its own flame set and the XP needed to unlock it.
    [Serializable]
    public class RocketSkin
    {
        public string title;
        public int unlockXP;
        public Sprite body, nose, finL, finR;
        public Rect bodyOpaque, noseOpaque;   // visible pixels inside each sprite rect (width 0 = use the whole rect)
        public float bodyDx, noseDx;          // manual horizontal nudge in rocket px (fixes sprites whose pivot shifts them)
        public Sprite[] flameSmall, flameBig;
    }

    // Rocket skins: picker in the Experiment tab, rank badge in the header, unlock popup when a rank is reached.
    // Saves: SAV2_skin (selected), SAV2_skinU_<i> (unlocked), SAV2_rankseen (highest rank already celebrated).
    public class SkinManager : MonoBehaviour
    {
        public AcademyManager manager;
        public RocketSim[] sims;              // every rocket in the scene (Challenge + Experiment)
        public RocketSkin[] skins;

        [Header("Layout (same proportions as the original rocket: body ~102 x 235, nose ~80 x 79)")]
        public float bodyHeight = 234.6f;     // height of the whole body sprite rect of the reference skin
        public float refBodyRectH = 410f;     // pixel height of that reference rect (every skin shares one scale)
        public Vector2 bodyPos = new Vector2(0.4f, -4.9f);
        public float noseScale = 1.08f;       // relative to the body scale
        public float noseOverlap = 14f;       // how far the nose sinks into the body top
        public float finScale = 1f;
        public float finInset = 40f;          // fin edge distance from the rocket centre
        public float finY = -2f;
        public float tileScale = 0.26f;       // thumbnail size in the picker
        public float popScale = 0.5f;         // preview size in the unlock popup

        [Header("Picker (Experiment tab)")]
        public Button[] tileButtons;
        public Image[] tileFrames;
        public RectTransform[] tileBody, tileNose, tileFinL, tileFinR;
        public TMP_Text[] tileNames, tileInfo;
        public GameObject[] tileLocks;
        public Color frameNormal = new Color(0.20f, 0.55f, 0.85f, 1f);
        public Color frameSelected = new Color(1f, 0.78f, 0.15f, 1f);
        public Color frameLocked = new Color(0.30f, 0.34f, 0.42f, 1f);

        [Header("Header rank badge")]
        public Image rankBadge;
        public Sprite[] rankBadges;           // CADET, PILOT, NAVIGATOR, COMMANDER

        [Header("Unlock popup")]
        public CanvasGroup unlockGroup;
        public RectTransform unlockPanel;
        public Image unlockBadge;
        public TMP_Text unlockTitle, unlockBody;
        public GameObject popThumbRoot;
        public RectTransform popBody, popNose, popFinL, popFinR;
        public Button unlockEquip, unlockClose;

        public int Selected { get; private set; }

        bool wired, showing;
        int pendingRank = -1, pendingSkin = -1, equipTarget = -1;

        void Awake() { Progress.Changed += OnProgress; }
        void OnDestroy() { Progress.Changed -= OnProgress; }

        void Start()
        {
            if (!PlayerPrefs.HasKey("SAV2_rankseen"))
            {
                // first run (or a save from before skins existed): unlock what the XP already earned, silently
                PlayerPrefs.SetInt("SAV2_rankseen", Progress.RankIndex);
                for (int i = 0; i < skins.Length; i++) if (skins[i].unlockXP <= Progress.XP) PlayerPrefs.SetInt("SAV2_skinU_" + i, 1);
                PlayerPrefs.Save();
            }
            Selected = PlayerPrefs.GetInt("SAV2_skin", 0);
            if (Selected < 0 || Selected >= skins.Length || !IsUnlocked(Selected)) Selected = 0;
            WireButtons();
            HideInstant();
            ApplySkin(Selected);
            RefreshPicker();
            RefreshBadge();
        }

        // ---------------------------------------------------------------- unlocks
        public bool IsUnlocked(int i)
        {
            if (i < 0 || i >= skins.Length) return false;
            return skins[i].unlockXP <= 0 || PlayerPrefs.GetInt("SAV2_skinU_" + i, 0) == 1;
        }

        void OnProgress()
        {
            RefreshBadge();
            int r = Progress.RankIndex;
            int seen = PlayerPrefs.GetInt("SAV2_rankseen", 0);
            if (r > seen)
            {
                int newest = -1;
                for (int i = 0; i < skins.Length; i++)
                {
                    if (skins[i].unlockXP <= Progress.XP && PlayerPrefs.GetInt("SAV2_skinU_" + i, 0) != 1)
                    {
                        PlayerPrefs.SetInt("SAV2_skinU_" + i, 1);
                        newest = i;
                    }
                }
                PlayerPrefs.SetInt("SAV2_rankseen", r);
                PlayerPrefs.Save();
                pendingRank = r; pendingSkin = newest;
            }
            else if (r < seen) PlayerPrefs.SetInt("SAV2_rankseen", r);   // progress was reset
            if (!IsUnlocked(Selected)) { Selected = 0; PlayerPrefs.SetInt("SAV2_skin", 0); ApplySkin(0); }
            RefreshPicker();
        }

        void Update()
        {
            if (pendingRank >= 0 && !showing && !Blocked()) ShowUnlock();
        }

        // wait until the mission-result popup / certificate has been closed
        bool Blocked()
        {
            if (manager == null) return false;
            if (manager.popupGroup != null && manager.popupGroup.gameObject.activeSelf) return true;
            if (manager.certGroup != null && manager.certGroup.gameObject.activeSelf) return true;
            return false;
        }

        // ---------------------------------------------------------------- applying a skin
        static void SetPiece(RectTransform rt, Sprite sp, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            if (rt == null || sp == null) return;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var im = rt.GetComponent<Image>();
            if (im != null) { im.sprite = sp; im.preserveAspect = false; }
        }

        // Lays the four pieces out like the original rocket. k = overall scale (1 = the real rocket).
        // Every skin uses the same pixel scale and the nose is centred on the visible body, so different padding in the art can't shift it.
        public void Layout(RectTransform body, RectTransform nose, RectTransform finL, RectTransform finR, RocketSkin sk, float k)
        {
            if (sk == null || sk.body == null) return;
            float s = bodyHeight / refBodyRectH * k;
            Vector2 bp = bodyPos * k;
            Rect bo = sk.bodyOpaque.width > 0f ? sk.bodyOpaque : new Rect(0f, 0f, sk.body.rect.width, sk.body.rect.height);
            Vector2 bsz = sk.body.rect.size * s;
            float bodyCx = (bo.x + bo.width * 0.5f - sk.body.rect.width * 0.5f) * s;      // visible centre relative to the rect centre
            SetPiece(body, sk.body, new Vector2(0.5f, 0f), new Vector2(bp.x - bodyCx + sk.bodyDx * k, bp.y), bsz);
            float top = bp.y + (bo.y + bo.height) * s;                                    // top of the visible body
            if (sk.nose != null)
            {
                Rect no = sk.noseOpaque.width > 0f ? sk.noseOpaque : new Rect(0f, 0f, sk.nose.rect.width, sk.nose.rect.height);
                float ns = s * noseScale;
                float noseCx = (no.x + no.width * 0.5f - sk.nose.rect.width * 0.5f) * ns;
                SetPiece(nose, sk.nose, new Vector2(0.5f, 0f), new Vector2(bp.x - noseCx + sk.noseDx * k, top - noseOverlap * k - no.y * ns), sk.nose.rect.size * ns);
            }
            SetPiece(finL, sk.finL, new Vector2(1f, 0f), new Vector2(-finInset * k, finY * k), sk.finL.rect.size * s * finScale);
            SetPiece(finR, sk.finR, new Vector2(0f, 0f), new Vector2(finInset * k, finY * k), sk.finR.rect.size * s * finScale);
        }

        public void ApplySkin(int i)
        {
            if (skins == null || i < 0 || i >= skins.Length) return;
            var sk = skins[i];
            if (sims == null) return;
            foreach (var sim in sims)
            {
                if (sim == null || sim.rocket == null) continue;
                var r = sim.rocket;
                Layout(r.Find("Body") as RectTransform, r.Find("Nose") as RectTransform, r.Find("FinL") as RectTransform, r.Find("FinR") as RectTransform, sk, 1f);
                if (sk.flameSmall != null && sk.flameSmall.Length > 0) sim.flameSmall = sk.flameSmall;
                if (sk.flameBig != null && sk.flameBig.Length > 0) sim.flameBig = sk.flameBig;
            }
        }

        public void Select(int i)
        {
            if (!IsUnlocked(i))
            {
                Sfx.Play(Snd.Wrong, 0.6f);
                if (tileButtons != null && i < tileButtons.Length) StartCoroutine(Fx.Shake((RectTransform)tileButtons[i].transform, 0.35f, 8f));
                if (manager != null && manager.mentor != null) manager.mentor.Oops("That rocket is locked. Earn " + skins[i].unlockXP + " XP to unlock it!");
                return;
            }
            Selected = i;
            PlayerPrefs.SetInt("SAV2_skin", i);
            PlayerPrefs.Save();
            ApplySkin(i);
            RefreshPicker();
            Sfx.Play(Snd.Tab);
            if (manager != null && manager.mentor != null) manager.mentor.Cheer(skins[i].title + " rocket equipped. Looking good, cadet!");
        }

        // ---------------------------------------------------------------- picker + badge
        void WireButtons()
        {
            if (wired) return;
            wired = true;
            if (tileButtons != null)
                for (int i = 0; i < tileButtons.Length; i++)
                {
                    int idx = i;
                    tileButtons[i].onClick.AddListener(() => Select(idx));
                }
            if (unlockEquip != null) unlockEquip.onClick.AddListener(() => { int t = equipTarget; HideUnlock(); if (t >= 0) Select(t); });
            if (unlockClose != null) unlockClose.onClick.AddListener(HideUnlock);
        }

        public void RefreshPicker()
        {
            if (tileButtons == null || skins == null) return;
            for (int i = 0; i < tileButtons.Length && i < skins.Length; i++)
            {
                bool open = IsUnlocked(i), sel = i == Selected;
                Layout(tileBody[i], tileNose[i], tileFinL[i], tileFinR[i], skins[i], tileScale);
                if (tileFrames[i] != null) tileFrames[i].color = sel ? frameSelected : (open ? frameNormal : frameLocked);
                if (tileLocks[i] != null) tileLocks[i].SetActive(!open);
                if (tileNames[i] != null) tileNames[i].text = skins[i].title;
                if (tileInfo[i] != null)
                {
                    tileInfo[i].text = !open ? "UNLOCK AT " + skins[i].unlockXP + " XP" : (sel ? "EQUIPPED" : "TAP TO EQUIP");
                    tileInfo[i].color = sel ? new Color(1f, 0.85f, 0.35f) : (open ? new Color(0.6f, 0.9f, 1f) : new Color(0.75f, 0.78f, 0.85f));
                }
            }
        }

        void RefreshBadge()
        {
            if (rankBadge == null || rankBadges == null) return;
            int r = Mathf.Clamp(Progress.RankIndex, 0, rankBadges.Length - 1);
            if (rankBadges[r] != null) rankBadge.sprite = rankBadges[r];
        }

        // ---------------------------------------------------------------- unlock popup
        void HideInstant()
        {
            if (unlockGroup == null) return;
            unlockGroup.alpha = 0f; unlockGroup.interactable = false; unlockGroup.blocksRaycasts = false;
            unlockGroup.gameObject.SetActive(false);
            showing = false;
        }

        void ShowUnlock()
        {
            if (unlockGroup == null) { pendingRank = -1; return; }
            int r = Mathf.Clamp(pendingRank, 0, Progress.RankNames.Length - 1), ns = pendingSkin;
            pendingRank = -1; pendingSkin = -1;
            showing = true; equipTarget = ns;
            bool has = ns >= 0 && ns < skins.Length;
            if (unlockBadge != null && rankBadges != null && r < rankBadges.Length) unlockBadge.sprite = rankBadges[r];
            if (unlockTitle != null) unlockTitle.text = "RANK UP!  ·  " + Progress.RankNames[r];
            if (unlockBody != null) unlockBody.text = has ? "New rocket unlocked: <color=#FF9F1C>" + skins[ns].title + "</color>" : "Keep up the great work, cadet!";
            if (popThumbRoot != null) popThumbRoot.SetActive(has);
            if (has) Layout(popBody, popNose, popFinL, popFinR, skins[ns], popScale);
            if (unlockEquip != null) unlockEquip.gameObject.SetActive(has);
            unlockGroup.gameObject.SetActive(true);
            unlockGroup.interactable = true; unlockGroup.blocksRaycasts = true;
            unlockGroup.alpha = 0f;
            StartCoroutine(Fx.Fade(unlockGroup, 1f, 0.25f));
            if (unlockPanel != null) StartCoroutine(Fx.Pop(unlockPanel, 0.75f, 0.4f));
            Sfx.Play(Snd.Fanfare);
            if (manager != null && manager.mentor != null) manager.mentor.Cheer("You reached " + Progress.RankNames[r] + "! " + (has ? "A new rocket skin is waiting for you." : ""));
        }

        void HideUnlock()
        {
            if (unlockGroup == null || !showing) return;
            unlockGroup.interactable = false; unlockGroup.blocksRaycasts = false;
            StartCoroutine(HideRoutine());
        }

        IEnumerator HideRoutine()
        {
            yield return Fx.Fade(unlockGroup, 0f, 0.18f);
            unlockGroup.gameObject.SetActive(false);
            showing = false;
        }
    }
}
