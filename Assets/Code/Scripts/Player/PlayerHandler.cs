using UnityEngine;
public class PlayerHandler : MonoBehaviour
{
    public PlayerMovement Movement { get; private set; }
    public FlashlightController Flashlight { get; private set; }

    private void Awake()
    {
        Movement = GetComponentInChildren<PlayerMovement>();
        Flashlight = GetComponentInChildren<FlashlightController>();
    }
}