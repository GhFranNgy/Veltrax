using UnityEngine;

public class UserSettings : MonoBehaviour
{
    [Header("=== KEY BINDINGS ===")]
    public KeyCode fireKey = KeyCode.Mouse0;       // Left Mouse Button
    public KeyCode aimKey = KeyCode.Mouse1;        // Right Mouse Button
    public KeyCode reloadKey = KeyCode.R;
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode switchFireModeKey = KeyCode.F;
}