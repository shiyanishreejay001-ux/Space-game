using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceAcademyV2
{
    public enum Mood { Neutral, Talk, Happy, Sad, Think, Teach }

    // Robot guide: idle animation, talking animation, reactions, typewriter speech bubble.
    public class Mentor : MonoBehaviour
    {
        public Image mentorImage;
        public RectTransform mentorRect;
        public RectTransform bubble;
        public TMP_Text bubbleText;
        public Sprite[] idleFrames;
        public Sprite[] talkFrames;
        public Sprite[] thinkFrames;   // optional: used while the mentor gives a hint (falls back to talkFrames)
        public Sprite[] teachFrames;   // optional: used while the mentor explains a mistake (falls back to talkFrames)
        public Sprite happy, sad;

        Mood mood = Mood.Neutral;
        float moodUntil;
        Coroutine speech;
        Vector2 basePos;
        float animT;
        int frame;
        float frameTimer;

        void Awake() { if (mentorRect != null) basePos = mentorRect.anchoredPosition; }

        void Update()
        {
            if (mentorImage == null) return;
            float dt = Time.unscaledDeltaTime;
            animT += dt;
            if (mood != Mood.Neutral && Time.unscaledTime > moodUntil && !Speaking) mood = Mood.Neutral;

            // bob
            if (mentorRect != null)
            {
                float amp = mood == Mood.Happy ? 14f : 6f;
                float spd = mood == Mood.Happy ? 9f : 2.2f;
                mentorRect.anchoredPosition = basePos + new Vector2(0f, Mathf.Sin(animT * spd) * amp);
                // Think: slow head tilt. Teach: gentle pulse. Used when no dedicated frames are assigned.
                float tilt = mood == Mood.Think ? Mathf.Sin(animT * 1.6f) * 5f - 4f : 0f;
                float pulse = mood == Mood.Teach ? 1f + Mathf.Sin(animT * 5f) * 0.025f : 1f;
                mentorRect.localRotation = Quaternion.Lerp(mentorRect.localRotation, Quaternion.Euler(0f, 0f, tilt), 1f - Mathf.Exp(-8f * dt));
                mentorRect.localScale = Vector3.Lerp(mentorRect.localScale, Vector3.one * pulse, 1f - Mathf.Exp(-8f * dt));
            }

            frameTimer -= dt;
            if (frameTimer <= 0f)
            {
                frame++;
                switch (mood)
                {
                    case Mood.Talk:
                    case Mood.Think:
                    case Mood.Teach:
                        frameTimer = 0.16f;
                        var set = SpeakingFrames();
                        if (set != null && set.Length > 0) mentorImage.sprite = set[frame % set.Length];
                        break;
                    case Mood.Happy:
                        frameTimer = 0.2f; if (happy != null) mentorImage.sprite = happy; break;
                    case Mood.Sad:
                        frameTimer = 0.2f; if (sad != null) mentorImage.sprite = sad; break;
                    default:
                        frameTimer = 0.7f;
                        if (idleFrames != null && idleFrames.Length > 0) mentorImage.sprite = idleFrames[frame % idleFrames.Length];
                        break;
                }
            }
        }

        bool Speaking { get { return mood == Mood.Talk || mood == Mood.Think || mood == Mood.Teach; } }

        Sprite[] SpeakingFrames()
        {
            if (mood == Mood.Think && thinkFrames != null && thinkFrames.Length > 0) return thinkFrames;
            if (mood == Mood.Teach && teachFrames != null && teachFrames.Length > 0) return teachFrames;
            return talkFrames;
        }

        public void Say(string text, Mood after = Mood.Neutral, float holdAfter = 2.5f, Mood during = Mood.Talk)
        {
            if (!gameObject.activeInHierarchy || bubbleText == null) return;
            if (speech != null) StopCoroutine(speech);
            speech = StartCoroutine(SayRoutine(text, after, holdAfter, during));
        }

        IEnumerator SayRoutine(string text, Mood after, float holdAfter, Mood during)
        {
            if (bubble != null) StartCoroutine(Fx.Pop(bubble, 0.92f, 0.25f));
            mood = during;
            bubbleText.text = text;
            bubbleText.maxVisibleCharacters = 0;
            bubbleText.ForceMeshUpdate();
            int total = bubbleText.textInfo.characterCount;
            int shown = 0;
            float acc = 0f;
            while (shown < total)
            {
                acc += Time.unscaledDeltaTime * 70f;
                int add = (int)acc;
                if (add > 0) { shown = Mathf.Min(total, shown + add); acc -= add; bubbleText.maxVisibleCharacters = shown; }
                yield return null;
            }
            bubbleText.maxVisibleCharacters = 99999;
            if (after == Mood.Happy || after == Mood.Sad) { mood = after; moodUntil = Time.unscaledTime + holdAfter; }
            else mood = Mood.Neutral;
            speech = null;
        }

        public void Cheer(string text) { Say(text, Mood.Happy, 3f); }
        public void Oops(string text) { Say(text, Mood.Sad, 3f); }
        public void Think(string text) { Say(text, Mood.Neutral, 2.5f, Mood.Think); }
        public void Teach(string text, Mood after = Mood.Sad) { Say(text, after, 3f, Mood.Teach); }
    }
}
