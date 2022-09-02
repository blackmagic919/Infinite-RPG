using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Generation/MeshSettings")]
public class MeshSettings : ScriptableObject
{
    public const int numSupportedLOD = 5;
    public const int numSupportedChunkSizes = 9;
    public static readonly int[] supportedChunkSizes = {48, 72, 96, 120, 144, 168, 192, 216, 240};
    public Shader terrainShader;
    public Material waterMaterial;
    public float uniformScale = 5f;
    public int biomeBlendThreshold = 10;
    public float biomeClipThreshold = 2;
    public int NavMeshVoxelSize;

    public int shaderNoiseResolution;
    public int shaderNoiseSeed;

    public float minHeight;
    public float maxHeight;

    [HideInInspector]
    public Texture2D noiseTexture;

    public int VertexesPerLineWater;
    public int VertexesPerLineTT; //Terrain Texture

    [Range(0, numSupportedChunkSizes-1)]
    public int chunkSizeInd;

    //Number of vertices per line of mesh rendered at highest resolution--0. Including 2 vertices excluded from final mesh, but used to calculate normals
    public int numVertsPerLine{
        get{
            return supportedChunkSizes[chunkSizeInd]+5;
        }
    }

    public float meshWorldSize{
        get{//1 because edges is one less than verts, and removes the 2 vertices for normals
            return (numVertsPerLine-3) * uniformScale;
        }
    }

    public void InitializeNoise(){ noiseTexture = PureNoiseGen.Create(shaderNoiseResolution, shaderNoiseSeed);}
}
