using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceAcademyV2
{
    // Mode state machine (Challenge / Experiment / Gravity), header, result popup, certificate.
    public class AcademyManager : MonoBehaviour
    {
        [Header("Modes")]
        public GameObject[] modePanels;
        public CanvasGroup[] modeGroups;
        public CanvasGroup[] bgGroups;
        public Button[] tabButtons;
        public Image[] tabImages;
        public Sprite[] tabActive, tabInactive;

        [Header("Controllers")]
        public Mentor mentor;
        public ChallengeController challenge;
        public ExperimentController experiment;
        public GravityController gravity;

        [Header("Header")]
        public TMP_Text xpText, rankText, starText;

        [Header("Result popup")]
        public RectTransform fxLayer;
        public CanvasGroup popupGroup;
        public RectTransform popupPanel;
        public TMP_Text popupTitle, popupBody, popupXP;
        public Image popupIcon;
        public Image[] popupStars;
        public Sprite checkSprite, crossSprite, starFull, starEmpty;
        public Button popupNext, popupRetry;

        [Header("Review mistakes")]
        public Button popupReview;
        public CanvasGroup reviewGroup;
        public RectTransform reviewPanel;
        public TMP_Text reviewBody;
        public Button reviewClose;

        [Header("Certificate")]
        public CanvasGroup certGroup;
        public RectTransform certPanel;
        public TMP_Text certText;
        public Button certClose;

        int mode = -1;
        Action nextAction, retryAction;
        string reviewText;

        public int Mode { get { return mode; } }

        void Awake()
        {
            Time.timeScale = 1f;
            Progress.Changed += RefreshHeader;
        }

        void OnDestroy() { Progress.Changed -= RefreshHeader; Sfx.StopEngine(); }

        void Start()
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int idx = i;
                tabButtons[i].onClick.AddListener(() => { if (idx != mode) { Sfx.Play(Snd.Tab); SetMode(idx); } });
            }
            if (popupNext != null) popupNext.onClick.AddListener(() => { var a = nextAction; HidePopup(); if (a != null) a(); });
            if (popupRetry != null) popupRetry.onClick.AddListener(() => { var a = retryAction; HidePopup(); if (a != null) a(); });
            if (certClose != null) certClose.onClick.AddListener(HideCertificate);
            if (popupReview != null) popupReview.onClick.AddListener(ShowReview);
            if (reviewClose != null) reviewClose.onClick.AddListener(HideReview);
            HideInstant(popupGroup); HideInstant(certGroup); HideInstant(reviewGroup);
            RefreshHeader();
            SetMode(0, true);
        }

        static void HideInstant(CanvasGroup g)
        {
            if (g == null) return;
            g.alpha = 0f; g.interactable = false; g.blocksRaycasts = false;
            g.gameObject.SetActive(false);
        }

        public void RefreshHeader()
        {
            if (xpText != null) xpText.text = "XP " + Progress.XP;
            if (rankText != null) rankText.text = Progress.Rank;
            if (starText != null) starText.text = Progress.TotalStars + "/" + (Progress.Levels * 3);
        }

        public void SetMode(int m, bool instant = false)
        {
            if (m == mode) return;
            int prev = mode;
            mode = m;
            for (int i = 0; i < modePanels.Length; i++)
            {
                bool on = i == m;
                if (on) { modePanels[i].SetActive(true); }
                if (tabImages != null && i < tabImages.Length)
                    tabImages[i].sprite = on ? tabActive[i] : tabInactive[i];
                if (bgGroups != null && i < bgGroups.Length)
                {
                    if (instant) bgGroups[i].alpha = on ? 1f : 0f;
                    else StartCoroutine(Fx.Fade(bgGroups[i], on ? 1f : 0f, 0.5f));
                }
                if (modeGroups != null && i < modeGroups.Length)
                {
                    if (on)
                    {
                        modeGroups[i].interactable = true; modeGroups[i].blocksRaycasts = true;
                        if (instant) modeGroups[i].alpha = 1f; else { modeGroups[i].alpha = 0f; StartCoroutine(Fx.Fade(modeGroups[i], 1f, 0.35f)); }
                    }
                }
            }
            if (prev >= 0 && prev != m) StartCoroutine(HideOld(prev));
            if (m == 0 && challenge != null) challenge.Enter();
            if (m == 1 && experiment != null) experiment.Enter();
            if (m == 2 && gravity != null) gravity.Enter();
        }

        IEnumerator HideOld(int old)
        {
            if (modeGroups != null && old < modeGroups.Length)
            {
                modeGroups[old].interactable = false; modeGroups[old].blocksRaycasts = false;
                yield return Fx.Fade(modeGroups[old], 0f, 0.25f);
            }
            if (old != mode) modePanels[old].SetActive(false);
        }

        // ------------------------------------------------------------------
        public void ShowResult(bool correct, int stars, int xp, string title, string body, Action onNext, Action onRetry, bool showNext, string review = null)
        {
            nextAction = onNext; retryAction = onRetry;
            reviewText = review;
            if (popupReview != null) popupReview.gameObject.SetActive(!string.IsNullOrEmpty(review));
            popupTitle.text = title;
            popupTitle.color = correct ? new Color(0.3f, 0.95f, 0.5f) : new Color(1f, 0.35f, 0.35f);
            popupBody.text = body;
            popupXP.text = xp > 0 ? "+" + xp + " XP" : "";
            popupIcon.sprite = correct ? checkSprite : crossSprite;
            for (int i = 0; i < popupStars.Length; i++)
            {
                popupStars[i].gameObject.SetActive(correct);
                popupStars[i].sprite = i < stars ? starFull : starEmpty;
                popupStars[i].transform.localScale = Vector3.zero;
            }
            popupNext.gameObject.SetActive(showNext);
            popupGroup.gameObject.SetActive(true);
            popupGroup.interactable = true; popupGroup.blocksRaycasts = true;
            StartCoroutine(Fx.Fade(popupGroup, 1f, 0.2f));
            StartCoroutine(Fx.Pop(popupPanel, 0.75f, 0.4f));
            Sfx.Play(correct ? Snd.Fanfare : Snd.Wrong);
            if (correct) StartCoroutine(StarsIn(stars));
        }

        IEnumerator StarsIn(int stars)
        {
            yield return new WaitForSecondsRealtime(0.35f);
            for (int i = 0; i < stars && i < popupStars.Length; i++)
            {
                Sfx.Play(Snd.Tab);
                StartCoroutine(Fx.Pop(popupStars[i].transform, 0.1f, 0.4f));
                yield return new WaitForSecondsRealtime(0.28f);
            }
            for (int i = stars; i < popupStars.Length; i++) popupStars[i].transform.localScale = Vector3.one;
            if (fxLayer != null && stars > 0) StartCoroutine(Fx.Burst(fxLayer, new Vector2(960f, -470f), 46, true));
        }

        public void HidePopup()
        {
            if (popupGroup == null || !popupGroup.gameObject.activeSelf) return;
            popupGroup.interactable = false; popupGroup.blocksRaycasts = false;
            StartCoroutine(HidePopupRoutine());
        }

        IEnumerator HidePopupRoutine()
        {
            HideInstant(reviewGroup);
            yield return Fx.Fade(popupGroup, 0f, 0.15f);
            popupGroup.gameObject.SetActive(false);
        }

        public void ShowReview()
        {
            if (reviewGroup == null || string.IsNullOrEmpty(reviewText)) return;
            reviewBody.text = reviewText;
            reviewGroup.gameObject.SetActive(true);
            reviewGroup.interactable = true; reviewGroup.blocksRaycasts = true;
            reviewGroup.alpha = 0f;
            StartCoroutine(Fx.Fade(reviewGroup, 1f, 0.2f));
            if (reviewPanel != null) StartCoroutine(Fx.Pop(reviewPanel, 0.85f, 0.3f));
            Sfx.Play(Snd.Reveal);
        }

        public void HideReview()
        {
            if (reviewGroup == null || !reviewGroup.gameObject.activeSelf) return;
            reviewGroup.interactable = false; reviewGroup.blocksRaycasts = false;
            StartCoroutine(HideReviewRoutine());
        }

        IEnumerator HideReviewRoutine()
        {
            yield return Fx.Fade(reviewGroup, 0f, 0.15f);
            reviewGroup.gameObject.SetActive(false);
        }

        public void ShowCertificate()
        {
            if (certGroup == null) return;
            certText.text = Progress.Rank + "  ·  " + Progress.XP + " XP  ·  " + Progress.TotalStars + " STARS";
            certGroup.gameObject.SetActive(true);
            certGroup.interactable = true; certGroup.blocksRaycasts = true;
            StartCoroutine(Fx.Fade(certGroup, 1f, 0.3f));
            StartCoroutine(Fx.Pop(certPanel, 0.6f, 0.5f));
            Sfx.Play(Snd.Fanfare);
            if (fxLayer != null) StartCoroutine(Fx.Burst(fxLayer, new Vector2(960f, -540f), 90, true));
            if (mentor != null) mentor.Cheer("Incredible! You completed every mission, Commander!");
        }

        public void HideCertificate()
        {
            if (certGroup == null) return;
            certGroup.interactable = false; certGroup.blocksRaycasts = false;
            StartCoroutine(HideCert());
        }

        IEnumerator HideCert()
        {
            yield return Fx.Fade(certGroup, 0f, 0.2f);
            certGroup.gameObject.SetActive(false);
        }

        public void Burst(Vector2 topLeftPos, int count)
        {
            if (fxLayer != null) StartCoroutine(Fx.Burst(fxLayer, topLeftPos, count, false));
        }
    }
}
