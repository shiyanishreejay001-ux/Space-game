using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The "If <condition> then" control block. Mirrors Scratch's If block:
// checks a single condition once when reached, and if true, runs its
// nested list of child blocks in order (each child's own Execute() runs
// exactly the way ShipBlockRunner already runs top-level blocks - this
// class is just another IShipBlock, so ShipBlockRunner needs no changes
// at all to support it). If the condition is false, the whole block is
// skipped and the sequence continues after it.
[System.Serializable]
public class ConditionalBlock : IShipBlock
{
    public ConditionSensor sensor;
    public ConditionComparator comparator;
    public float value;
    public List<IShipBlock> children = new List<IShipBlock>();

    public IEnumerator Execute(Rigidbody rb)
    {
        if (!ConditionTypes.Evaluate(rb, sensor, comparator, value))
        {
            yield break;
        }

        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] == null) continue;
            yield return children[i].Execute(rb);
        }
    }
}
