using UnityEngine;

// What the If block's condition reads off the ship, and how it compares
// that reading against the player-entered value. Kept separate from
// BlockDefinition/ConditionalBlock so both the UI (dropdowns) and the
// runtime block (ConditionalBlock) share one source of truth for what a
// condition means and how it's evaluated.
public enum ConditionSensor
{
    Altitude,      // rb.position.y
    Speed,         // rb.linearVelocity.magnitude
    VerticalSpeed  // rb.linearVelocity.y (negative while descending)
}

public enum ConditionComparator
{
    Greater,
    Less,
    GreaterOrEqual,
    LessOrEqual,
    Equal
}

public static class ConditionTypes
{
    public static readonly ConditionSensor[] AllSensors =
    {
        ConditionSensor.Altitude,
        ConditionSensor.Speed,
        ConditionSensor.VerticalSpeed
    };

    public static readonly ConditionComparator[] AllComparators =
    {
        ConditionComparator.Greater,
        ConditionComparator.Less,
        ConditionComparator.GreaterOrEqual,
        ConditionComparator.LessOrEqual,
        ConditionComparator.Equal
    };

    public static string SensorLabel(ConditionSensor sensor)
    {
        switch (sensor)
        {
            case ConditionSensor.Altitude: return "Altitude";
            case ConditionSensor.Speed: return "Speed";
            case ConditionSensor.VerticalSpeed: return "Vertical Speed";
        }
        return sensor.ToString();
    }

    public static string ComparatorLabel(ConditionComparator comparator)
    {
        switch (comparator)
        {
            case ConditionComparator.Greater: return ">";
            case ConditionComparator.Less: return "<";
            case ConditionComparator.GreaterOrEqual: return ">=";
            case ConditionComparator.LessOrEqual: return "<=";
            case ConditionComparator.Equal: return "=";
        }
        return comparator.ToString();
    }

    public static float Measure(Rigidbody rb, ConditionSensor sensor)
    {
        switch (sensor)
        {
            case ConditionSensor.Altitude: return rb.position.y;
            case ConditionSensor.Speed: return rb.linearVelocity.magnitude;
            case ConditionSensor.VerticalSpeed: return rb.linearVelocity.y;
        }
        return 0f;
    }

    public static bool Evaluate(Rigidbody rb, ConditionSensor sensor, ConditionComparator comparator, float value)
    {
        float measured = Measure(rb, sensor);
        switch (comparator)
        {
            case ConditionComparator.Greater: return measured > value;
            case ConditionComparator.Less: return measured < value;
            case ConditionComparator.GreaterOrEqual: return measured >= value;
            case ConditionComparator.LessOrEqual: return measured <= value;
            case ConditionComparator.Equal: return Mathf.Approximately(measured, value);
        }
        return false;
    }
}
