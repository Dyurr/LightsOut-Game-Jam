using UnityEngine;
using UnityEngine.UI;

public class Item : MonoBehaviour
{
    
    [SerializeField] protected int cashValue = 50;
    [SerializeField] protected string itemName = "Default Item";
    [SerializeField] protected float holdDuration;

    protected float holdTimer = 0f;
    protected bool isHolding = false;
    protected Slider progressBar;

    public virtual string InteractionPrompt => $"Hold E to pick up {itemName}";

    public void SetProgressBar(Slider slider)
    {
        progressBar = slider;
        progressBar.gameObject.SetActive(false);

    }

    public void StartHolding()
    {
        isHolding = true;
        holdTimer = 0f;
        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(true);
            progressBar.value = 0f;
        }
    }

    public void CancelHolding()
    {
        isHolding = false;
        holdTimer = 0f;
        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(false);
            progressBar.value = 0f;
        }
    }

    public void TickHolding(float deltaTime)
    {
        if (isHolding)
        {
            holdTimer += deltaTime;
            if (progressBar != null)
            {
                progressBar.value = holdTimer / holdDuration;
            }

            if (holdTimer >= holdDuration)
            {
                Interact();
                CancelHolding();
            }
        }
    }

    public virtual void Interact()
    {
        PlayerWallet.Instance.AddCash(cashValue);
        NoiseSystem.MakeNoise(transform.position, 2f); // Make noise when picking up item
        Destroy(gameObject);
    }
}
