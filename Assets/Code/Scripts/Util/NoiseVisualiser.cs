using UnityEngine;
using System.Collections.Generic;
public class NoiseVisualiser : MonoBehaviour
{
 private struct NoiseEvent
    {
        public Vector3 position;
        public float radius;
        public float timeCreated;
    }

    [SerializeField] private float displayDuration = 1f;
    private List<NoiseEvent> activeNoises = new List<NoiseEvent>();

    private void OnEnable()
    {
        NoiseSystem.OnNoiseMade += RegisterNoise;
    }

    private void OnDisable()
    {
        NoiseSystem.OnNoiseMade -= RegisterNoise;
    }

    private void RegisterNoise(Vector3 position, float radius)
    {
        activeNoises.Add(new NoiseEvent { position = position, radius = radius, timeCreated = Time.time });
    }

    private void Update()
    {
        activeNoises.RemoveAll(n => Time.time - n.timeCreated > displayDuration);
    }

    private void OnDrawGizmos()
    {
        foreach (var noise in activeNoises)
        {
            float age = Time.time - noise.timeCreated;
            float alpha = 1f - (age / displayDuration);
            Gizmos.color = new Color(1f, 0f, 0f, alpha);
            Gizmos.DrawWireSphere(noise.position, noise.radius);
        }
    }
}
