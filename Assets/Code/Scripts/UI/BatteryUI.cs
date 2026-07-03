using UnityEngine;
using UnityEngine.UI;
public class BatteryUI : MonoBehaviour
{
 
    [SerializeField] private RectTransform fillRect;
    [SerializeField] private FlashlightController flashlightController;

    private float maxWidth;

    private void Awake()
    {
        maxWidth = fillRect.sizeDelta.x;
    }

    private void OnEnable()
    {
        flashlightController.onBatteryChanged += UpdateFill;
    }

    private void OnDisable()
    {
        flashlightController.onBatteryChanged -= UpdateFill;
    }

    private void Start()
    {
        UpdateFill(flashlightController.CurrentBattery);
    }

    private void UpdateFill(float currentBattery)
    {
        float percent = currentBattery / 100f;
        fillRect.sizeDelta = new Vector2(maxWidth * percent, fillRect.sizeDelta.y);
    }
}
