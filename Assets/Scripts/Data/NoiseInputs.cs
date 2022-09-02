using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Generation/NoiseInp")]
public class NoiseInputs : ScriptableObject
{
    public float noiseScale;
    public int octaves;
    [Range(0, 1)]
    public float persistance;
    public float lacunarity;
    public float lerpScale;
    public int localSeed = 0;
}
