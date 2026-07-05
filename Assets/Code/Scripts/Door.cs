using UnityEngine;

public class Door : Item
{
   [SerializeField] private string sceneToLoad = "DylanTest"; // Default scene name

    public override string InteractionPrompt => $"Hold E to exit";

    public override void Interact()
    {
         // Load the specified scene when the door is interacted with
         UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoad);
    }
}
