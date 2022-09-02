using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Generation/Biomes")]
public class BiomeData : ScriptableObject
{
    public List<Biome> biomes;

    [System.Serializable]
    public class Biome{
        public List<EntityGenerationData> entities;
        public int rejectionSample;
        
        public float noiseBiomeStartHeight;
        public float noiseBiomeEndHeight;
        public int biomeFrequency;

        public TerrainData terrainData;
        public TextureData textureData;
        public NoiseInputs BiomeGenNoise;
        public NoiseInputs GenerationNoise;
        
        //TT -> Terrain Texture
        public NoiseInputs TTNoise;

        public int waterHeight;
        public long maximumWaterArea;
    }
    [System.Serializable]
    public class EntityGenerationData{
        public EntityData entityData;
        public int radius;
        public float generationMinHeight;
        public float generationMaxHeight;
        public int frequency;
    }

    private void Awake()
    {
        if (biomes == null) return;
        for(int i = 0; i < biomes.Count; i++)
        {
            biomes[i].BiomeGenNoise.localSeed += i;
            biomes[i].GenerationNoise.localSeed += i;
        }
    }

}