using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    
    [SerializeField] private Light flashlight;


    [Header("Battery Settings")]
    [SerializeField] private float maxBatteryLife = 100f;
    [SerializeField] private float batteryDrainRate = 5f; // Battery drain per
    [SerializeField] private float batteryRechargeRate = 2f; // Battery recharge per second
    [SerializeField] private float rechargeDelay = 3f; // Delay before recharge starts



    
    [Header("Low Battery Feedback")]
    [SerializeField] private float lowBatteryThreshold = 20f; // Battery level at which feedback starts
    [SerializeField] private float baseIntensity = 1f; // Base intensity of the flashlight
    [SerializeField] private float flickerIntensity = 0.5f; // Int
    [SerializeField] private float flickerSpeed = 0.1f; // Speed of flickering

    public float CurrentBattery {get; private set;} // Current battery level

    public bool IsFlashlightOn {get; private set;} // Flashlight state

    public event System.Action<float> onBatteryChanged; // Event for battery level change
    public event System.Action<bool> onBatteryDepleted; // Event for flashlight toggle


    private float timeSinceOff = 0f;


    private void Awake()
    {
        CurrentBattery = maxBatteryLife;
        flashlight.intensity = baseIntensity;
        SetFlashlight(false);
    }


    private void Update()
    {
            if (Input.GetKeyDown(KeyCode.F))
    {
        ToggleFlashlight();
    }

    if (IsFlashlightOn)
    {
        DrainBattery();
        HandleFlicker();
        timeSinceOff = 0f;
    }
    else
    {
        timeSinceOff += Time.deltaTime;
        if (timeSinceOff >= rechargeDelay && CurrentBattery < maxBatteryLife)
        {
            RechargeBattery();
        }
    }
    }


    private void ToggleFlashlight()
    {
        if (!IsFlashlightOn && CurrentBattery <= 0f) return; // block turning on with no battery
            SetFlashlight(!IsFlashlightOn);
            NoiseSystem.MakeNoise(transform.position, 3f); // Make noise when toggling flashlight
    }

    private void SetFlashlight(bool state)
    {
        IsFlashlightOn = state;
        flashlight.enabled = state;

        if (state) flashlight.intensity = baseIntensity; // Reset intensity when turning on
    }

    private void DrainBattery()
    {
        
        CurrentBattery -= batteryDrainRate * Time.deltaTime;
        CurrentBattery = Mathf.Max(CurrentBattery, 0f); // Ensure battery doesn't go below 0
        onBatteryChanged?.Invoke(CurrentBattery);

        if (CurrentBattery <= 0f)
        {
            SetFlashlight(false);
            onBatteryDepleted?.Invoke(true);
        }
    }

    private void RechargeBattery()
    {
        CurrentBattery += batteryRechargeRate * Time.deltaTime;
        CurrentBattery = Mathf.Min(CurrentBattery, maxBatteryLife); // Ensure battery doesn't exceed max
        onBatteryChanged?.Invoke(CurrentBattery);
    }


    private void HandleFlicker()
    {
        if (CurrentBattery > lowBatteryThreshold)
        {
            flashlight.intensity = baseIntensity;
            return;
        }

        float severity = 1f - (CurrentBattery / lowBatteryThreshold); // Calculate severity based on battery level
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
        float flickerRange = Mathf.Lerp(baseIntensity, flickerIntensity, severity); 
        
        flashlight.intensity = Mathf.Lerp(flickerRange, baseIntensity, noise);
    }
}
