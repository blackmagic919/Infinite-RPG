using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Generation/Terrain")]
public class TerrainData : ScriptableObject
{
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
}
