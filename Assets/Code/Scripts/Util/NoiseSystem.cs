using UnityEngine;

public class NoiseSystem
{
    public static event System.Action<Vector3, float> OnNoiseMade;
    // position with a radius of noise


    public static void MakeNoise(Vector3 position, float radius)
    {
        OnNoiseMade?.Invoke(position, radius);
    }

    
}
