using UnityEngine;
using UnityEngine.UI;
using TMPro;

// One of the 4 fixed Play-sequence rows: a dropdown to pick the block type
// (unset / Wait / Boost) plus the number field(s) for that type's
// parameters. GetBlock() turns the row's current UI state into an
// IShipBlock, or null if the row is left unset/empty/invalid so SpaceShip
// can skip it when building the sequence.
public class BlockRowUI : MonoBehaviour
{
    // Index must match the dropdown's Options list order exactly:
    // 0 = "-- Empty --", 1 = "Wait", 2 = "Boost".
    public enum BlockType
    {
        None = 0,
        Wait = 1,
        Boost = 2
    }

    [Header("Dropdown (options: -- Empty --, Wait, Boost - in that order)")]
    [SerializeField] private TMP_Dropdown blockTypeDropdown;

    [Header("Wait fields")]
    [SerializeField] private GameObject waitFieldsPanel;
    [SerializeField] private TMP_InputField waitSecondsInput;

    [Header("Boost fields")]
    [SerializeField] private GameObject boostFieldsPanel;
    [SerializeField] private TMP_InputField thrustForceInput;
    [SerializeField] private TMP_InputField durationInput;

    private void Start()
    {
        if (blockTypeDropdown != null)
        {
            blockTypeDropdown.onValueChanged.AddListener(OnDropdownChanged);
            OnDropdownChanged(blockTypeDropdown.value);
        }
    }

    // Shows only the field group relevant to the selected block type.
    private void OnDropdownChanged(int index)
    {
        BlockType type = (BlockType)index;

        if (waitFieldsPanel != null) waitFieldsPanel.SetActive(type == BlockType.Wait);
        if (boostFieldsPanel != null) boostFieldsPanel.SetActive(type == BlockType.Boost);
    }

    // Returns the IShipBlock for this row's current state, or null if the
    // row is unset ("-- Empty --") or its required field(s) are blank/
    // unparseable - SpaceShip skips null rows when building the sequence.
    public IShipBlock GetBlock()
    {
        if (blockTypeDropdown == null) return null;

        BlockType type = (BlockType)blockTypeDropdown.value;

        switch (type)
        {
            case BlockType.Wait:
                if (waitSecondsInput == null) return null;
                if (!float.TryParse(waitSecondsInput.text, out float waitSeconds)) return null;
                return new WaitBlock { waitSeconds = waitSeconds };

            case BlockType.Boost:
                if (thrustForceInput == null || durationInput == null) return null;
                if (!float.TryParse(thrustForceInput.text, out float thrustForce)) return null;
                if (!float.TryParse(durationInput.text, out float duration)) return null;
                return new BoostBlock { thrustForce = thrustForce, duration = duration };

            default:
                return null;
        }
    }
}
