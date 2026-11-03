using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceAcademyV2
{
    // Experiment mode: choose mass and thrust, see the physics live, then run the experiment.
    public class ExperimentController : MonoBehaviour
    {
        public AcademyManager manager;
        public Mentor mentor;
        public RocketSim sim;

        public Slider massSlider, thrustSlider;       // mass = value * 50 kg ; thrust = value * 1000 N
        public TMP_Text massLabel, thrustLabel;
        public TMP_Text weightText, netText, accText, twrText, verdictText;
        public TMP_Text feedbackText;
        public Button runButton, resetButton;

        bool wired, busy;
        float bestAlt;

        float Mass { get { return massSlider.value * 50f; } }
        float Thrust { get { return thrustSlider.value * 1000f; } }

        void Wire()
        {
            if (wired) return;
            wired = true;
            massSlider.onValueChanged.AddListener(v => { Sfx.Play(Snd.Tick, 0.5f); Refresh(); });
            thrustSlider.onValueChanged.AddListener(v => { Sfx.Play(Snd.Tick, 0.5f); Refresh(); });
            runButton.onClick.AddListener(Run);
            resetButton.onClick.AddListener(ResetAll);
        }

        public void Enter()
        {
            Wire();
            ResetAll();
            mentor.Say("Welcome to the Rocket Lab! Slide the MASS and THRUST controls, then press RUN EXPERIMENT. Can your rocket beat gravity?");
        }

        void ResetAll()
        {
            busy = false;
            sim.ResetRocket();
            massSlider.interactable = true; thrustSlider.interactable = true; runButton.interactable = true;
            feedbackText.text = bestAlt > 0f ? "Best altitude so far: " + bestAlt.ToString("0") + " m" : "";
            feedbackText.color = new Color(0.7f, 0.8f, 1f);
            Refresh();
        }

        void Refresh()
        {
            float m = Mass, F = Thrust, g = ChallengeData.G;
            float W = m * g;
            float net = F - W;
            float a = net / m;
            massLabel.text = "Mass: " + m.ToString("0", CultureInfo.InvariantCulture) + " kg";
            thrustLabel.text = "Thrust: " + F.ToString("0", CultureInfo.InvariantCulture) + " N";
            weightText.text = "Weight   W = m × g = " + W.ToString("0") + " N";
            netText.text = "Net force  F − W = " + net.ToString("0") + " N";
            accText.text = "Acceleration  a = " + (net > 0 ? a : 0f).ToString("0.00") + " m/s²";
            twrText.text = "Thrust ÷ Weight = " + (F / W).ToString("0.00");
            if (net <= 0f) { verdictText.text = "Too heavy. Thrust must beat the weight!"; verdictText.color = new Color(1f, 0.4f, 0.4f); }
            else if (a < 5f) { verdictText.text = "Lifts off, but slowly."; verdictText.color = new Color(1f, 0.85f, 0.35f); }
            else { verdictText.text = "Strong liftoff!"; verdictText.color = new Color(0.3f, 0.95f, 0.5f); }
        }

        void Run()
        {
            if (busy) return;
            busy = true;
            massSlider.interactable = false; thrustSlider.interactable = false; runButton.interactable = false;
            float m = Mass, F = Thrust;
            feedbackText.color = new Color(0.7f, 0.8f, 1f);
            feedbackText.text = "Experiment running…";
            mentor.Say("Prediction: a = (F − m×g) ÷ m. Let's see if the rocket agrees!");
            sim.Launch(m, F, true, 10f, (alt, vel, noLift) =>
            {
                busy = false;
                massSlider.interactable = true; thrustSlider.interactable = true; runButton.interactable = true;
                if (noLift)
                {
                    feedbackText.color = new Color(1f, 0.4f, 0.4f);
                    feedbackText.text = "No liftoff: weight " + (m * ChallengeData.G).ToString("0") + " N is bigger than thrust " + F.ToString("0") + " N.";
                    mentor.Oops("The rocket is too heavy for its engine. Add thrust or remove mass!");
                    Sfx.Play(Snd.Wrong);
                }
                else
                {
                    if (alt > bestAlt) bestAlt = alt;
                    feedbackText.color = new Color(0.3f, 0.95f, 0.5f);
                    feedbackText.text = "Reached " + alt.ToString("0") + " m at " + vel.ToString("0.0") + " m/s.  (Best: " + bestAlt.ToString("0") + " m)";
                    mentor.Cheer("Nice flight! Higher thrust or lower mass means bigger acceleration.");
                    Sfx.Play(Snd.Correct);
                    manager.Burst(new Vector2(1310f, -600f), 22);
                    if (Progress.AddXPOnce("firstlift", 20)) feedbackText.text += "  +20 XP first liftoff!";
                }
            });
        }
    }
}
