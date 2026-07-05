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
    [SerializeField] private float baseIntensity = 150f; // Base intensity of the flashlight
    [SerializeField] private float flickerIntensity = 0.5f; // Int 
    [SerializeField] private float flickerSpeed = 0.1f; // Speed of flickering

    [Header("Flashlight Wobble")]
    [SerializeField] private Transform flashlightPivot;
    [SerializeField] private float wobbleAmount = 2.5f;
    [SerializeField] private float wobbleSmoothness = 8f;
    [SerializeField] private float returnSmoothness = 6f;

    private Vector3 lastRotation;
    private Vector3 currentWobble;

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
        lastRotation = transform.eulerAngles;
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
        HandleWobble();
    }

    private void HandleWobble()
{
    if (flashlightPivot == null) return;

    Vector3 currentRotation = transform.eulerAngles;

    float yawDelta = Mathf.DeltaAngle(lastRotation.y, currentRotation.y);
    float pitchDelta = Mathf.DeltaAngle(lastRotation.x, currentRotation.x);

    Vector3 targetWobble = new Vector3(
        -pitchDelta * wobbleAmount,
        yawDelta * wobbleAmount,
        0f
    );

    targetWobble.x = Mathf.Clamp(targetWobble.x, -wobbleAmount, wobbleAmount);
    targetWobble.y = Mathf.Clamp(targetWobble.y, -wobbleAmount, wobbleAmount);

    currentWobble = Vector3.Lerp(
        currentWobble,
        targetWobble,
        Time.deltaTime * wobbleSmoothness
    );

    currentWobble = Vector3.Lerp(
        currentWobble,
        Vector3.zero,
        Time.deltaTime * returnSmoothness
    );

    flashlightPivot.localRotation = Quaternion.Euler(currentWobble);

    lastRotation = currentRotation;
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

    float severity = 1f - (CurrentBattery / lowBatteryThreshold);

    // Random flicker between low and high
    float randomFlicker = Random.Range(0f, 1f);

    // Makes dark dips more common
    randomFlicker = Mathf.Pow(randomFlicker, 2.5f);

    float minIntensity = Mathf.Lerp(baseIntensity, flickerIntensity, severity);
    float maxIntensity = baseIntensity;

    flashlight.intensity = Mathf.Lerp(minIntensity, maxIntensity, randomFlicker);
}
}
