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
        CheckForInteractable();
        if (Input.GetKeyDown(KeyCode.E) && itemTarget != null)
        {
            InteractWithItem();
        }
    }



    // Checking for interactable objects in front of the player
    private void CheckForInteractable()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, interactableLayer))
        {
            itemTarget = hit.collider.GetComponent<Item>();
        }
        else
        {
            itemTarget = null;
        }
        interactPromptText.gameObject.SetActive(itemTarget != null);
    }
    
    private void InteractWithItem()
    {
        if (itemTarget != null)
        {
            itemTarget.Interact();
            itemTarget = null; // Reset the target after interaction
            interactPromptText.gameObject.SetActive(false);
        }
    }

}
