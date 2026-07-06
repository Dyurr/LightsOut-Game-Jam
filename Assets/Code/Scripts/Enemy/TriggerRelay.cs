using UnityEngine;

public class TriggerRelay : MonoBehaviour
{
    public EnemyAI owner; // assign this to the parent enemy in the Inspector

    void OnTriggerEnter(Collider other) => owner?.OnDetectionTriggerEnter(other);
    void OnTriggerStay(Collider other) => owner?.OnDetectionTriggerStay(other);
    void OnTriggerExit(Collider other) => owner?.OnDetectionTriggerExit(other);
}