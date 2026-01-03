using UnityEngine;

public class UserSettings : MonoBehaviour
{
    [Header("=== KEY BINDINGS ===")]
    public KeyCode fireKey = KeyCode.Mouse0;
    public KeyCode aimKey = KeyCode.Mouse1;
    public KeyCode reloadKey = KeyCode.R;
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode switchFireModeKey = KeyCode.F;

    [Header("Movement Keys")]
    public KeyCode moveForward = KeyCode.W;
    public KeyCode moveBackward = KeyCode.S;
    public KeyCode moveLeft = KeyCode.A;
    public KeyCode moveRight = KeyCode.D;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode slideKey = KeyCode.LeftControl;

    [Header("Mouse Axes")]
    public string mouseXAxis = "Mouse X";
    public string mouseYAxis = "Mouse Y";

    [Header("Mouse Settings")]
    [Range(0f, 100f)] public float mouseSensitivity = 50f;
    [Range(0f, 1f)] public float adsSensitivityMultiplier = 0.6f;
    public bool invertMouseY = false;
}
