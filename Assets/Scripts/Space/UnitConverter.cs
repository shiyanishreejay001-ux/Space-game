using System;
using System.Globalization;
using UnityEngine;

// Phase B3 - the ONE place that converts game units into real-world units.
//
// How the constants were chosen (derived from values already in the project,
// nothing in the scene was retuned):
//  - Earth is 6 units across and 12,742 km across, so
//        KmPerUnit = 12742 / 6 = ~2123.67 km per unit.
//    Every "near Earth" distance on the HUD (altitude, apoapsis, periapsis)
//    uses this single constant.
//  - EarthOrbitalGravity uses mu = 900 u^3/s^2 and Earth's real mu is
//    398,600 km^3/s^2. Requiring both to describe the same physics fixes how
//    fast game time runs against real time:
//        RealSecondsPerGameSecond = sqrt(900 * KmPerUnit^3 / 398600) = ~4650
//    Speed therefore converts as  km/s = u/s * KmPerUnit / RealSecondsPerGameSecond
//    (~0.457 km/s per u/s). Check: the game's circular speed at Earth's surface
//    is sqrt(900/3) = 17.3 u/s, which converts to 7.9 km/s - the real low-orbit
//    speed. The 25 u/s entry cap converts to ~11.4 km/s, next to real escape
//    speed (11.2 km/s). So the orbit lessons come out with real numbers.
//
// What this does NOT claim: the solar system itself is compressed (Earth to Sun
// is 50 units here, ~149.6 million km in reality). SolarKmPerUnit exists so
// planet-to-planet distances can be shown honestly and labelled as a separate
// scale. Do not use KmPerUnit for planet-to-planet distances.
//
// If EarthOrbitalGravity.gravitationalParameter or Earth's scale is ever
// re-tuned, update GameMu / EarthDiameterUnits below to match.
public static class UnitConverter
{
    // ---- Earth-local scale (HUD altitude / speed) ----
    public const double EarthDiameterKm = 12742.0;
    public const double EarthDiameterUnits = 6.0;
    public const double KmPerUnit = EarthDiameterKm / EarthDiameterUnits;
    public const float EarthRadiusUnits = (float)(EarthDiameterUnits / 2.0);

    public const double EarthMuKm3PerS2 = 398600.4418;
    // Must match EarthOrbitalGravity.gravitationalParameter (read-only reference).
    public const double GameMu = 900.0;

    public static readonly double RealSecondsPerGameSecond =
        Math.Sqrt(GameMu * KmPerUnit * KmPerUnit * KmPerUnit / EarthMuKm3PerS2);

    // ---- Solar-system scale (planet-to-planet distances only) ----
    public const double AstronomicalUnitKm = 149597870.7;
    // PlanetOrbit.orbitRadius of Earth around the Sun.
    public const double EarthOrbitRadiusUnits = 50.0;
    public const double SolarKmPerUnit = AstronomicalUnitKm / EarthOrbitRadiusUnits;

    public const double SpeedOfLightKmPerS = 299792.458;

    // ---- Conversions ----
    public static float UnitsToKm(float units)
    {
        return (float)(units * KmPerUnit);
    }

    public static float KmToUnits(float km)
    {
        return (float)(km / KmPerUnit);
    }

    /// <summary>Speed in game units/second to real km/s.</summary>
    public static float SpeedToKmPerSecond(float unitsPerSecond)
    {
        return (float)(unitsPerSecond * KmPerUnit / RealSecondsPerGameSecond);
    }

    /// <summary>
    /// Altitude above a planet's surface in km, from the distance to its CENTRE
    /// in game units. Negative when the point is inside the surface.
    /// </summary>
    public static float AltitudeKm(float distanceFromCentreUnits, float planetRadiusUnits = EarthRadiusUnits)
    {
        return (float)((distanceFromCentreUnits - planetRadiusUnits) * KmPerUnit);
    }

    /// <summary>Planet-to-planet distance in game units to km (solar scale).</summary>
    public static float SolarUnitsToKm(float units)
    {
        return (float)(units * SolarKmPerUnit);
    }

    public static float SolarUnitsToAu(float units)
    {
        return (float)(units / EarthOrbitRadiusUnits);
    }

    public static double GameSecondsToRealSeconds(double gameSeconds)
    {
        return gameSeconds * RealSecondsPerGameSecond;
    }

    public static double LightTimeSeconds(double distanceKm)
    {
        return distanceKm / SpeedOfLightKmPerS;
    }

    /// <summary>Real travel time in seconds for a distance (km) at a speed (km/s).</summary>
    public static double TravelTimeSeconds(double distanceKm, double speedKmPerS)
    {
        return speedKmPerS > 1e-6 ? distanceKm / speedKmPerS : double.PositiveInfinity;
    }

    // ---- Formatting (invariant culture so the HUD reads the same everywhere) ----
    public static string FormatKm(float km)
    {
        if (float.IsNaN(km) || float.IsInfinity(km)) return "N/A";
        float abs = Mathf.Abs(km);
        if (abs >= 1000000f) return (km / 1000000f).ToString("F2", CultureInfo.InvariantCulture) + " M km";
        return km.ToString("N0", CultureInfo.InvariantCulture) + " km";
    }

    public static string FormatSpeedKmPerSecond(float kmPerSecond)
    {
        if (float.IsNaN(kmPerSecond) || float.IsInfinity(kmPerSecond)) return "N/A";
        return kmPerSecond.ToString("F2", CultureInfo.InvariantCulture) + " km/s";
    }

    public static string FormatDuration(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds)) return "N/A";
        if (seconds < 60.0) return seconds.ToString("F1", CultureInfo.InvariantCulture) + " s";
        if (seconds < 3600.0) return (seconds / 60.0).ToString("F1", CultureInfo.InvariantCulture) + " min";
        if (seconds < 86400.0) return (seconds / 3600.0).ToString("F1", CultureInfo.InvariantCulture) + " h";
        return (seconds / 86400.0).ToString("F1", CultureInfo.InvariantCulture) + " days";
    }
}
