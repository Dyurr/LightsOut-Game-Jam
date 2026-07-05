using UnityEngine;
using TMPro;
public class PlayerInteract : MonoBehaviour
{
    
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private TextMeshProUGUI interactPromptText;

    private Item itemTarget;


    private void Start()
    {
        interactPromptText.gameObject.SetActive(false); // Initially hide the prompt
    }

private void Update()
    {
        Item previousTarget = itemTarget;
        CheckForInteractable();

        if (itemTarget != previousTarget)
        {
            previousTarget?.CancelHolding();
            itemTarget?.SetProgressBar(HUDController.Instance.InteractProgressBar);
        }

        if (itemTarget != null)
        {
            if (Input.GetKeyDown(KeyCode.E))
                itemTarget.StartHolding();

            if (Input.GetKey(KeyCode.E))
                itemTarget.TickHolding(Time.deltaTime);
            else
                itemTarget.CancelHolding();
        }

        interactPromptText.text = itemTarget != null ? itemTarget.InteractionPrompt : "";
        interactPromptText.gameObject.SetActive(itemTarget != null);
    }



    // Checking for interactable objects in front of the player
    private void CheckForInteractable()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, interactableLayer))
            itemTarget = hit.collider.GetComponent<Item>();
        else
            itemTarget = null;
    }

}
