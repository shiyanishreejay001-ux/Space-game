using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The "Repeat <times>" loop block. Mirrors Scratch's Repeat block: runs
// its nested list of child blocks in order, that many times. Like
// ConditionalBlock, this is just another IShipBlock - ShipBlockRunner
// doesn't need to know it contains other blocks internally.
[System.Serializable]
public class RepeatBlock : IShipBlock
{
    public int times = 1;
    public List<IShipBlock> children = new List<IShipBlock>();

    public IEnumerator Execute(Rigidbody rb)
    {
        int count = Mathf.Max(0, times);
        for (int loop = 0; loop < count; loop++)
        {
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] == null) continue;
                yield return children[i].Execute(rb);
            }
        }
    }
}
