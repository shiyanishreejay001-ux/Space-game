using UnityEngine;

// Phase C1 - educational data for ONE body (Sun, a planet, or the Moon).
//
// One asset per body lives in Assets/Resources/PlanetData/. Teachers or
// designers can edit the numbers and the fun fact in the Inspector without
// touching code. PlanetDatabase loads them; if the folder is empty it falls
// back to the built-in defaults in PlanetDefaults, so the learning UI still
// works.
//
// All numbers here are REAL values. The game's own scale is compressed, so
// nothing in this file is derived from scene sizes.
//
// Text is kept to plain ASCII on purpose (m/s^2, "deg C") so it renders with
// the Orbitron SDF font without missing-glyph boxes.
[CreateAssetMenu(fileName = "PlanetData", menuName = "Space Game/Planet Data", order = 1)]
public class PlanetData : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Planet";
    [Tooltip("Name of the GameObject in SpaceScene this data belongs to (e.g. Earth, Moon, Sun).")]
    public string sceneObjectName = "Planet";
    public string bodyType = "Terrestrial planet";
    [Tooltip("Sort order in lists (Sun first, then outward).")]
    public int order;
    public Color accentColor = Color.white;

    [Header("Size and gravity")]
    public double diameterKm = 12742.0;
    [Tooltip("Surface gravity in m/s^2 (Earth = 9.81).")]
    public float surfaceGravity = 9.81f;

    [Header("Time")]
    public string dayLength = "24 hours";
    public string yearLength = "365 days";

    [Header("Orbit and distance (real values)")]
    [Tooltip("Average distance from the Sun in astronomical units. 0 = not applicable.")]
    public double distanceFromSunAu;
    [Tooltip("Distance from Earth in km. 0 = not applicable (Earth itself).")]
    public double distanceFromEarthKm;
    [Tooltip("What the Earth distance means: 'closest approach' or 'average'.")]
    public string distanceFromEarthNote = "closest approach";

    [Header("Extras")]
    [Tooltip("Text, because counts keep changing as new moons are found (e.g. '95+').")]
    public string moons = "0";
    public string averageTemperature = "15 deg C";
    [TextArea(2, 5)]
    public string funFact = "";

    // ---- Derived helpers (all real-world, none use the game scale) ----
    public double DiameterVsEarth => diameterKm / UnitConverter.EarthDiameterKm;
    public float GravityVsEarth => surfaceGravity / 9.81f;
    public bool HasEarthDistance => distanceFromEarthKm > 0.0;
}
