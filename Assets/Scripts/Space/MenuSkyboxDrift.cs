using UnityEngine;

/// <summary>
/// Adds an extremely subtle rotation to the scene's skybox so the main
/// menu backdrop feels alive rather than a frozen image. Instances the
/// skybox material on Awake so this NEVER modifies the shared material
/// asset (and therefore never affects SpaceScene or any other scene that
/// references the same skybox material).
/// </summary>
public class MenuSkyboxDrift : MonoBehaviour
{
    [Tooltip("Degrees per second added to the skybox _Rotation property. Kept tiny for subtlety.")]
    public float degreesPerSecond = 0.15f;

    private Material _instancedSkybox;
    private float _rotation;

    private void Awake()
    {
        if (RenderSettings.skybox == null || !RenderSettings.skybox.HasProperty("_Rotation"))
        {
            enabled = false;
            return;
        }

        // Clone so we never write back to the shared skybox asset on disk.
        _instancedSkybox = new Material(RenderSettings.skybox);
        _rotation = _instancedSkybox.GetFloat("_Rotation");
        RenderSettings.skybox = _instancedSkybox;
    }

    private void Update()
    {
        _rotation = Mathf.Repeat(_rotation + degreesPerSecond * Time.deltaTime, 360f);
        _instancedSkybox.SetFloat("_Rotation", _rotation);
    }

    private void OnDestroy()
    {
        if (_instancedSkybox != null)
        {
            Destroy(_instancedSkybox);
        }
    }
}
