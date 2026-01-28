using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TMP_Text crosshair;
    public UserSettings userSettings;

    void Update()
    {
        UpdateCrosshair();
    }

    void UpdateCrosshair()
    {
        bool isAiming = Input.GetKey(userSettings.aimKey);
        bool isRunning = Input.GetKey(userSettings.runKey);

        bool isMoving =
            Input.GetKey(userSettings.moveForward);

        
        if (isAiming && (!isRunning || !isMoving))
        {
            crosshair.enabled = false;
        }
        else
        {
            crosshair.enabled = true;
        }
    }
}