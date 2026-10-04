using System;
using UnityEngine;

// Fuel system for the block-programmed rocket. Tracks fuel consumed by
// ACTUAL propulsion (Launch/Boost applying thrust) and exposes read-only
// state + a change event for UI display.
//
// Attached to the same GameObject as the ship's Rigidbody (the "SpaceShip"
// object, which already carries RocketPersistence / SpaceRocketManager /
// ShipBlockRunner) so it persists across the RocketLauncher -> SpaceScene
// transition by riding along on that SAME DontDestroyOnLoad object - no
// separate persistence, singleton, or scene-load logic is added here (that
// would risk creating a second/duplicate system); it just behaves exactly
// like ShipBlockRunner already does across the transition.
//
// LaunchBlock/BoostBlock look this component up via
// rb.GetComponent<RocketFuelSystem>() at the exact point propulsion is
// about to execute, so fuel is only ever consumed at those two real
// execution points - never by a second/parallel tracking system, and never
// merely because a block exists in the workspace.
public class RocketFuelSystem : MonoBehaviour
{
    [Header("Fuel capacity")]
    [SerializeField] private float maxFuel = 100f;
    [SerializeField] private float startingFuel = 100f;

    [Header("Fuel cost per action")]
    [SerializeField] private float launchFuelCost = 20f;
    [SerializeField] private float boostFuelCost = 10f;

    [Tooltip("Optional. Extra fuel drained per second while a Boost block's thrust is actively applying, on top of boostFuelCost above. Leave at 0 (default) to disable - Boost then only ever costs the flat boostFuelCost.")]
    [SerializeField] private float boostFuelPerSecond = 0f;

    [Header("Current fuel (read-only at runtime - for inspection only)")]
    [SerializeField] private float currentFuel;

    public float CurrentFuel => currentFuel;
    public float MaxFuel => maxFuel;
    public bool HasFuel => currentFuel > 0f;

    // Fired whenever currentFuel actually changes (consume or reset), with
    // the new (current, max) values, so UI can update immediately on the
    // same frame instead of waiting on a polling Update().
    public event Action<float, float> OnFuelChanged;

    private bool loggedEmpty;

    private void Awake()
    {
        currentFuel = Mathf.Clamp(startingFuel, 0f, maxFuel);
    }

    private void Start()
    {
        // Fire once on startup so UI that subscribed in its own Start()/
        // OnEnable() gets the real initial value immediately rather than
        // showing a blank/default field until the first later change.
        OnFuelChanged?.Invoke(currentFuel, maxFuel);
    }

    // Called by LaunchBlock immediately before it would apply thrust.
    // Returns true (and consumes launchFuelCost) only if there is enough
    // fuel for the full cost; returns false (consumes nothing) otherwise -
    // LaunchBlock is expected to skip applying thrust in that case.
    public bool TryConsumeLaunchFuel()
    {
        return TryConsume(launchFuelCost);
    }

    // Called by BoostBlock immediately before it would apply thrust. Same
    // contract as TryConsumeLaunchFuel().
    public bool TryConsumeBoostFuel()
    {
        return TryConsume(boostFuelCost);
    }

    // Drains boostFuelPerSecond * deltaTime for one physics step of an
    // in-progress Boost. Returns true if thrust may continue this step
    // (including whenever boostFuelPerSecond is 0 - a permanent no-op in
    // that case), false once fuel has just run out, so the caller can stop
    // applying thrust for the remainder of the block without throwing.
    public bool ConsumeBoostFuelPerSecond(float deltaTime)
    {
        if (boostFuelPerSecond <= 0f) return true;

        if (currentFuel <= 0f)
        {
            LogEmptyOnce();
            return false;
        }

        SetCurrentFuel(currentFuel - boostFuelPerSecond * deltaTime);
        return currentFuel > 0f;
    }

    // Resets fuel back to startingFuel. Call this only from the single
    // point the existing game logic already treats as "starting a new
    // mission/program attempt" (SpaceShip's Retry button, which already
    // resets ship position/rotation/velocity for that same reason) - never
    // every frame or every block execution.
    public void ResetFuel()
    {
        loggedEmpty = false;
        SetCurrentFuel(Mathf.Clamp(startingFuel, 0f, maxFuel));
    }

    private bool TryConsume(float cost)
    {
        if (cost <= 0f) return true; // no cost configured - always allowed

        if (currentFuel < cost)
        {
            LogEmptyOnce();
            return false;
        }

        SetCurrentFuel(currentFuel - cost);
        return true;
    }

    private void SetCurrentFuel(float value)
    {
        float clamped = Mathf.Clamp(value, 0f, maxFuel);
        currentFuel = clamped;

        if (currentFuel > 0f)
        {
            loggedEmpty = false;
        }

        OnFuelChanged?.Invoke(currentFuel, maxFuel);
    }

    private void LogEmptyOnce()
    {
        if (loggedEmpty) return;
        loggedEmpty = true;
        Debug.LogWarning("[RocketFuelSystem] Fuel empty - propulsion unavailable.");
    }
}
