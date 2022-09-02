using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(menuName = "Generation/Texture")]
public class TextureData : ScriptableObject
{
    const int textureSize = 512;
    const TextureFormat textureFormat = TextureFormat.RGB565;

    public Layer[] layers;

    float savedMinHeight;
    float savedMaxHeight;
    public int blendDistance;

    public void ApplyToMaterial(Material material, Material water, TextureData[] surroundingTextures, Texture2DArray textures, Texture2D waterNoise, Texture2D TTNoise, Vector2 coord, float meshWorldSize)
    {

        material.SetInt("layerCount", layers.Length);
        material.SetInt("blendDistance", blendDistance);
        material.SetColorArray("baseColors", layers.Select(x => x.tint).ToArray());
        material.SetFloatArray("baseStartHeights", layers.Select(x => x.startHeight).ToArray());
        material.SetFloatArray("baseBlends", layers.Select(x => x.blendStrength).ToArray());
        material.SetFloatArray("baseColorStrength", layers.Select(x => x.tintStrength).ToArray());
        material.SetFloatArray("baseTextureScales", layers.Select(x => x.textureScale).ToArray());
        material.SetFloatArray("baseTextureIndexes", layers.Select(x => (float)x.textureInd).ToArray());
        material.SetFloatArray("noiseStartHeights", layers.Select(x => x.startNoise).ToArray());
        material.SetFloat("boundUp", (coord.y + 0.5f) * meshWorldSize);
        material.SetFloat("boundDown", (coord.y - 0.5f) * meshWorldSize);
        material.SetFloat("boundRight", (coord.x + 0.5f) * meshWorldSize);
        material.SetFloat("boundLeft", (coord.x - 0.5f) * meshWorldSize);
        material.SetFloat("TTVertexC", TTNoise.width);

        enterNeighbors(material, surroundingTextures);

        material.SetTexture("baseTextures", textures);

        material.SetTexture("TTNoise", TTNoise);
        water.SetTexture("_NoiseTex", waterNoise);
        UpdateMeshHeights(material, savedMinHeight, savedMaxHeight);
    }
    
    public void enterNeighbors(Material material, TextureData[] surroundingTextures){
        //Get Total Layers
        int totalLayers = 0;
        for(int i = 0; i < 8; i++){
            totalLayers += surroundingTextures[i].layers.Length;
        }

        float[] neighborLayerCount = new float[totalLayers];
        Color[] neighborBaseColors = new Color[totalLayers];
        float[] neighborBaseStartHeights = new float[totalLayers];
        float[] neighborBaseBlends= new float[totalLayers];
        float[] neighborBaseColorStrength = new float[totalLayers];
        float[] neighborBaseTextureScales = new float[totalLayers];
        float[] texturesArray = new float[totalLayers];

        int curInd = 0;
        for(int i = 0; i < surroundingTextures.Length; i++){
            Layer[] layers = surroundingTextures[i].layers;
            addItemsToColorList(neighborBaseColors, layers.Select(x => x.tint).ToArray(), curInd); 
            addItemsToFloatList(neighborBaseStartHeights, layers.Select(x => x.startHeight).ToArray(), curInd);
            addItemsToFloatList(neighborBaseBlends, layers.Select(x => x.blendStrength).ToArray(), curInd);
            addItemsToFloatList(neighborBaseColorStrength, layers.Select(x => x.tintStrength).ToArray(), curInd);
            addItemsToFloatList(neighborBaseTextureScales, layers.Select(x => x.textureScale).ToArray(), curInd);
            addItemsToFloatList(texturesArray, layers.Select(x => (float)x.textureInd).ToArray(), curInd);
            for(int c = 0; c < layers.Length; c++)
                neighborLayerCount[c+curInd] = i;
            curInd += layers.Length;
        }
        material.SetInt("neighborLayerCount", curInd);

        material.SetFloatArray("neighborIndToLayer", neighborLayerCount);
        material.SetColorArray("neighborBaseColors", neighborBaseColors);
        material.SetFloatArray("neighborBaseStartHeights", neighborBaseStartHeights);
        material.SetFloatArray("neighborBaseBlends", neighborBaseBlends);
        material.SetFloatArray("neighborBaseColorStrength", neighborBaseColorStrength);
        material.SetFloatArray("neighborBaseTextureScales", neighborBaseTextureScales);
        material.SetFloatArray("neighborBaseTextureIndexes", texturesArray);
    }

    public void addItemsToFloatList(float[] to, float[] from, int ind){
        for(int i = 0; i < from.Length; i++){
            to[ind+i] = from[i];
        }
    }

    public void addItemsToColorList(Color[] to, Color[] from, int ind){
        for(int i = 0; i < from.Length; i++){
            to[ind+i] = from[i];
        }
    }

    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight){
        savedMinHeight = minHeight;
        savedMaxHeight = maxHeight;
        material.SetFloat("minHeight", minHeight);
        material.SetFloat("maxHeight", maxHeight);
    }

    [System.Serializable]
    public class Layer{
        public int textureInd;
        public Color tint;
        [Range(0,1)]
        public float tintStrength;
        [Range(0,1)]
        public float startHeight;
        [Range(0, 1)]
        public float startNoise;
        [Range(0,1)]
        public float blendStrength;
        public float textureScale;
        //VN -> Virtual Noise -> TerTextureNoise
        [Range(0, 1)]
        public float VNStartHeight;
    }
}
