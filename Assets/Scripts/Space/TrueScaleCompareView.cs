using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;

// Phase B6 - "True Scale Compare" overlay for SpaceScene.
//
// The in-game solar system is deliberately compressed so every planet fits on
// screen (Earth 6 units wide, Jupiter ~14, the Sun 40; Earth-Sun 50 units).
// This overlay shows the REAL sizes drawn to one common scale, next to the
// game's numbers, and is labelled as not being the game scale so students do
// not mistake one for the other.
//
// Pure display: it reads each body's scale and PlanetOrbit.orbitRadius from the
// scene when opened and never writes to anything. Real diameters / distances
// are fixed reference data below (match the PlanetInfo text already in the scene).
//
// Controls: [T] toggles it. The overlay does not change Time.timeScale.
// IMGUI placeholder: restyle with the UITheme (Phase A) when the UI kit exists.
public class TrueScaleCompareView : MonoBehaviour
{
    private struct Body
    {
        public string name;
        public double diameterKm;
        public double orbitAu;     // from the Sun; 0 = not applicable
        public Color color;

        public Body(string name, double diameterKm, double orbitAu, Color color)
        {
            this.name = name; this.diameterKm = diameterKm; this.orbitAu = orbitAu; this.color = color;
        }
    }

    private static readonly Body[] Planets =
    {
        new Body("Mercury", 4879,   0.387, new Color(0.66f, 0.63f, 0.60f)),
        new Body("Venus",   12104,  0.723, new Color(0.92f, 0.76f, 0.45f)),
        new Body("Earth",   12742,  1.000, new Color(0.22f, 0.55f, 0.95f)),
        new Body("Mars",    6779,   1.524, new Color(0.85f, 0.38f, 0.25f)),
        new Body("Jupiter", 139820, 5.203, new Color(0.85f, 0.66f, 0.50f)),
        new Body("Saturn",  116460, 9.537, new Color(0.91f, 0.81f, 0.56f)),
        new Body("Uranus",  50724,  19.19, new Color(0.55f, 0.85f, 0.90f)),
        new Body("Neptune", 49244,  30.07, new Color(0.30f, 0.45f, 0.95f)),
    };

    private const double SunDiameterKm = 1391000.0;
    private const double MoonDiameterKm = 3474.0;
    private const double MoonDistanceKm = 384400.0;
    private const double EarthDiameterKm = 12742.0;

    [SerializeField] private bool startOpen = false;

    private bool open;
    private Texture2D circle;

    // Game-side numbers, read from the scene when the overlay opens.
    private readonly double[] gameDiameterVsEarth = new double[Planets.Length];
    private readonly double[] gameOrbitAu = new double[Planets.Length];
    private double gameSunVsEarth;
    private double gameMoonVsEarth;
    private double gameMoonOrbitEarthDiameters;

    private GUIStyle label, header, small, title, warn;

    public bool IsOpen => open;

    private void Start()
    {
        open = startOpen;
        if (open) ReadSceneValues();
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.tKey.wasPressedThisFrame) Toggle();
    }

    public void Toggle()
    {
        open = !open;
        if (open) ReadSceneValues();
    }

    private void ReadSceneValues()
    {
        GameObject earthObj = GameObject.Find("Earth");
        float earthScale = earthObj != null ? earthObj.transform.lossyScale.x : 6f;
        if (earthScale <= 0f) earthScale = 6f;

        for (int i = 0; i < Planets.Length; i++)
        {
            GameObject go = GameObject.Find(Planets[i].name);
            gameDiameterVsEarth[i] = go != null ? go.transform.lossyScale.x / earthScale : 0.0;

            PlanetOrbit orbit = go != null ? go.GetComponent<PlanetOrbit>() : null;
            gameOrbitAu[i] = orbit != null ? orbit.orbitRadius / UnitConverter.EarthOrbitRadiusUnits : 0.0;
        }

        GameObject sun = GameObject.Find("Sun");
        gameSunVsEarth = sun != null ? sun.transform.lossyScale.x / earthScale : 0.0;

        GameObject moon = GameObject.Find("Moon");
        gameMoonVsEarth = moon != null ? moon.transform.lossyScale.x / earthScale : 0.0;
        PlanetOrbit moonOrbit = moon != null ? moon.GetComponent<PlanetOrbit>() : null;
        gameMoonOrbitEarthDiameters = moonOrbit != null ? moonOrbit.orbitRadius / earthScale : 0.0;
    }

    private void EnsureAssets()
    {
        if (circle == null)
        {
            const int size = 128;
            circle = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            float r = size * 0.5f - 1f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f));
                    byte a = (byte)(Mathf.Clamp01(r - d + 0.5f) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            circle.SetPixels32(pixels);
            circle.Apply();
        }

        if (label == null)
        {
            label = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleLeft, wordWrap = false };
            label.normal.textColor = new Color(0.96f, 0.97f, 1f);

            small = new GUIStyle(label) { fontSize = 12, alignment = TextAnchor.UpperCenter };
            small.normal.textColor = new Color(0.60f, 0.65f, 0.78f);

            header = new GUIStyle(label) { fontSize = 12, fontStyle = FontStyle.Bold };
            header.normal.textColor = new Color(0.18f, 0.77f, 0.95f);

            title = new GUIStyle(label) { fontSize = 26, fontStyle = FontStyle.Bold };
            title.normal.textColor = new Color(0.18f, 0.77f, 0.95f);

            warn = new GUIStyle(label) { fontSize = 14, wordWrap = true, alignment = TextAnchor.UpperLeft };
            warn.normal.textColor = new Color(1f, 0.62f, 0.11f);
        }
    }

    private void OnGUI()
    {
        if (!open) return;
        EnsureAssets();

        // Lay out in a virtual 720-high space so it scales with resolution.
        float s = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        float vw = Screen.width / s;
        const float vh = 720f;

        const float panelW = 1060f;
        const float panelH = 650f;
        float px = (vw - panelW) * 0.5f;
        float py = (vh - panelH) * 0.5f;
        float x0 = px + 30f;

        // Panel background.
        GUI.color = new Color(0.04f, 0.06f, 0.16f, 0.96f);
        GUI.DrawTexture(new Rect(px, py, panelW, panelH), Texture2D.whiteTexture);
        GUI.color = new Color(0.18f, 0.77f, 0.95f, 1f);
        GUI.DrawTexture(new Rect(px, py, panelW, 3f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(x0, py + 12f, 700f, 36f), "TRUE SCALE COMPARE", title);
        GUI.Label(new Rect(x0, py + 50f, panelW - 60f, 40f),
            "Real sizes drawn to one scale. This is NOT the game scale: in the game, planets and orbits are " +
            "compressed so everything fits on screen.", warn);

        // ---- Circles, all to the same scale (Earth = 18 px) ----
        const float earthPx = 18f;
        float groundY = py + 300f;
        float cx = x0;
        for (int i = 0; i < Planets.Length; i++)
        {
            float d = (float)(Planets[i].diameterKm / EarthDiameterKm) * earthPx;
            float slot = Mathf.Max(d, 64f);

            GUI.color = Planets[i].color;
            GUI.DrawTexture(new Rect(cx + (slot - d) * 0.5f, groundY - d, d, d), circle);
            GUI.color = Color.white;
            GUI.Label(new Rect(cx, groundY + 4f, slot, 18f), Planets[i].name, small);

            cx += slot + 12f;
        }

        double sunEarths = SunDiameterKm / EarthDiameterKm;
        GUI.Label(new Rect(x0, groundY + 28f, panelW - 60f, 22f),
            $"The Sun is {sunEarths.ToString("F0", CultureInfo.InvariantCulture)} Earths wide - about " +
            $"{(sunEarths * earthPx).ToString("N0", CultureInfo.InvariantCulture)} px at this scale, too big to draw.", label);

        // ---- Table ----
        float ty = groundY + 62f;
        float[] colX = { 0f, 120f, 270f, 400f, 540f, 700f };
        string[] heads =
        {
            "BODY", "REAL DIAMETER", "TRUE SIZE vs EARTH", "GAME SIZE vs EARTH", "REAL DISTANCE FROM SUN", "GAME DISTANCE (AU-equiv.)"
        };
        for (int c = 0; c < heads.Length; c++)
        {
            GUI.Label(new Rect(x0 + colX[c], ty, 240f, 18f), heads[c], header);
        }
        ty += 20f;

        for (int i = 0; i < Planets.Length; i++)
        {
            DrawRow(x0, colX, ty, Planets[i].name, Planets[i].diameterKm,
                Planets[i].diameterKm / EarthDiameterKm, gameDiameterVsEarth[i],
                Planets[i].orbitAu, gameOrbitAu[i]);
            ty += 17f;
        }

        DrawRow(x0, colX, ty, "Moon", MoonDiameterKm, MoonDiameterKm / EarthDiameterKm, gameMoonVsEarth, -1.0, 0.0);
        ty += 17f;
        DrawRow(x0, colX, ty, "Sun", SunDiameterKm, sunEarths, gameSunVsEarth, -1.0, 0.0);
        ty += 24f;

        double realMoonDiameters = MoonDistanceKm / EarthDiameterKm;
        GUI.Label(new Rect(x0, ty, panelW - 60f, 20f),
            $"Earth to Moon: really {realMoonDiameters.ToString("F1", CultureInfo.InvariantCulture)} Earth-widths " +
            $"({MoonDistanceKm.ToString("N0", CultureInfo.InvariantCulture)} km); " +
            $"in the game {gameMoonOrbitEarthDiameters.ToString("F1", CultureInfo.InvariantCulture)} Earth-widths.", label);

        GUI.Label(new Rect(x0, py + panelH - 28f, panelW - 60f, 20f), "[T] close", small);
        GUI.matrix = Matrix4x4.identity;
    }

    private void DrawRow(float x0, float[] colX, float y, string name, double realKm,
                         double trueVsEarth, double gameVsEarth, double realAu, double gameAu)
    {
        var inv = CultureInfo.InvariantCulture;
        GUI.Label(new Rect(x0 + colX[0], y, 120f, 17f), name, label);
        GUI.Label(new Rect(x0 + colX[1], y, 150f, 17f), realKm.ToString("N0", inv) + " km", label);
        GUI.Label(new Rect(x0 + colX[2], y, 130f, 17f), trueVsEarth.ToString("F2", inv) + " x", label);
        GUI.Label(new Rect(x0 + colX[3], y, 140f, 17f), gameVsEarth.ToString("F2", inv) + " x", label);
        GUI.Label(new Rect(x0 + colX[4], y, 160f, 17f), realAu > 0.0 ? realAu.ToString("F2", inv) + " AU" : "-", label);
        GUI.Label(new Rect(x0 + colX[5], y, 200f, 17f), realAu > 0.0 ? gameAu.ToString("F2", inv) + " AU" : "-", label);
    }

    private void OnDestroy()
    {
        if (circle != null) Destroy(circle);
    }
}
