using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TerrainGenerator : MonoBehaviour
{
    const float viewerChunkUpdateThreshold = 25f;
    const float squareViewerChunkUpdateThreshold = viewerChunkUpdateThreshold * viewerChunkUpdateThreshold;//Quicker

    public BiomeData biomeData;

    public NoiseData noiseData;
    public MeshData meshData;
    public RandomUpdate manualUpdater;

    public LODInfo[] detailLevels;
    public Transform viewer;
    public MeshSettings meshSettings;
    public TextureAssets textures;
    public int environmentDetail;
    public int mobUpdateDistance;
    WaterGenerator waterGen;
    List<Mesh> water = new List<Mesh>();

    Texture2DArray generatedTextures;
    WaterGenInfo waterBaseInfo;

    Vector2 viewerPos;
    Vector2 viewerPosOld;
    int chunksVisible;
    float meshWorldSize;

    private GameObject TerrainChunkFolder;

    [HideInInspector]
    public Dictionary<Vector2, TerrainChunk> chunkTerDict = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> visibleChunks = new List<TerrainChunk>();

    void Start()
    {
        TerrainChunkFolder = new GameObject("Terrain Chunks");
        TerrainChunkFolder.transform.parent = transform;

        generatedTextures = TextureAssets.GenerateTextureArray(textures.texture2D);
        meshSettings.InitializeNoise();

        float maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistThresh;
        meshWorldSize = meshSettings.meshWorldSize;
        chunksVisible = Mathf.RoundToInt(maxViewDistance / meshWorldSize);
        waterGen = new WaterGenerator();
        updateVisibleChunks();
    }

    void Update()
    {
        viewerPos = new Vector2(viewer.position.x, viewer.position.z);

        if ((viewerPosOld - viewerPos).sqrMagnitude > squareViewerChunkUpdateThreshold)
        {
            viewerPosOld = viewerPos;
            updateVisibleChunks();
        }
    }

    void onChangeBiome(object obj)
    {
        EntityBehavior realObj = (EntityBehavior)obj;
        Vector3 pos3D = realObj.info.entityTrans.position;
        Vector2 chunkCoord = new Vector2(Mathf.RoundToInt(pos3D.x / meshWorldSize), Mathf.RoundToInt(pos3D.z / meshWorldSize));
        realObj.info.currentBiome = chunkTerDict[chunkCoord];
        chunkTerDict[chunkCoord].environment.reCalculateRegion(new Vector2(pos3D.x, pos3D.z), realObj);
        realObj.info.entityTrans.parent = chunkTerDict[chunkCoord].meshObj.transform;
        realObj.info.currentBiome = chunkTerDict[chunkCoord];
    }

    void getDiffBiomeRegions(object information)
    {
        Environment.callbackInfo info = (Environment.callbackInfo)information;
        Vector2 pos2D = info.parameter;

        Vector2 chunkCoord = new Vector2(Mathf.RoundToInt(pos2D.x / meshWorldSize), Mathf.RoundToInt(pos2D.y / meshWorldSize));
        Vector2 bottomLeft = (chunkCoord - Vector2.one / 2) * meshWorldSize;
        Vector2 pos = pos2D - bottomLeft;
        int RegionX = (int)(pos.x / environmentDetail);
        int RegionY = (int)(pos.y / environmentDetail);
        info.ret = chunkTerDict[chunkCoord].environment.regions[RegionX, RegionY];
    }

    void updateVisibleChunks()
    {
        HashSet<Vector2> alreadyUpdatedChunks = new HashSet<Vector2>();
        int curChunkX = Mathf.RoundToInt(viewerPos.x / meshWorldSize);
        int curChunkZ = Mathf.RoundToInt(viewerPos.y / meshWorldSize);

        for (int i = visibleChunks.Count - 1; i >= 0; i--)
        {
            alreadyUpdatedChunks.Add(visibleChunks[i].coord);
            visibleChunks[i].UpdateChunk();
        }

        //Generate as checkerboard
        for (int zOffS = -chunksVisible; zOffS <= chunksVisible; zOffS++)
        {
            for (int xOffS = -chunksVisible; xOffS <= chunksVisible; xOffS++)
            {
                Vector2 viewedChunkCoord = new Vector2(curChunkX + xOffS, curChunkZ + zOffS);
                if (alreadyUpdatedChunks.Contains(viewedChunkCoord)) continue;
                if (chunkTerDict.ContainsKey(viewedChunkCoord)) chunkTerDict[viewedChunkCoord].UpdateChunk();
                else
                {
                    int skipInc = (meshSettings.numVertsPerLine / meshSettings.VertexesPerLineWater);
                    waterBaseInfo = new WaterGenInfo(viewedChunkCoord, biomeData.biomes, getBiome, getSurroundingMeshTerrain, getSurroundingNoiseMesh, meshSettings, skipInc);
                    TerrainChunk newChunk = new TerrainChunk(viewedChunkCoord, viewer, detailLevels, environmentDetail, onChangeBiome, getDiffBiomeRegions, TerrainChunkFolder.transform, biomeData, noiseData, meshSettings, generatedTextures, manualUpdater, waterBaseInfo, waterGen);
                    chunkTerDict.Add(viewedChunkCoord, newChunk);
                    newChunk.biome = getBiome(viewedChunkCoord);
                    newChunk.onVisibilityChanged += onChunkVisibilityChanged;
                    newChunk.surroundingTerrainColor = getSurroundingMeshTexture(viewedChunkCoord);
                    newChunk.surroundingTerrainNoise = getSurroundingNoiseMesh(viewedChunkCoord);
                    newChunk.surroundingTerrainMesh = getSurroundingMeshTerrain(viewedChunkCoord);
                    newChunk.initializeMapData();
                }
            }
        }
    }

    TerrainData[] getSurroundingMeshTerrain(Vector2 coord)
    {
        TerrainData[] surroundHeights = new TerrainData[8];
        int[] yC = new int[] { -1, 0, 1, 0, -1, 1, 1, -1 };
        int[] xC = new int[] { 0, 1, 0, -1, 1, 1, -1, -1 };
        for (int i = 0; i < 8; i++)
        {
            Vector2 neighborCoord = new Vector2(coord.x + xC[i], coord.y + yC[i]);

            surroundHeights[i] = biomeData.biomes[getBiome(neighborCoord)].terrainData;

        }
        return surroundHeights;
    }

    TextureData[] getSurroundingMeshTexture(Vector2 coord)
    {
        TextureData[] surroundTexture = new TextureData[8];
        int[] yC = new int[] { -1, -1, -1, 0, 0, 1, 1, 1 };
        int[] xC = new int[] { -1, 0, 1, -1, 1, -1, 0, 1 };
        for (int i = 0; i < 8; i++)
        {
            Vector2 neighborCoord = new Vector2(coord.x + xC[i], coord.y + yC[i]);
            surroundTexture[i] = biomeData.biomes[getBiome(neighborCoord)].textureData;
        }
        return surroundTexture;
    }

    NoiseInputs[] getSurroundingNoiseMesh(Vector2 coord)
    {
        NoiseInputs[] surroundTexture = new NoiseInputs[8];
        //See MeshGenerator for coordinate map
        int[] yC = new int[] { -1, 0, 1, 0, -1, 1, 1, -1 };
        int[] xC = new int[] { 0, 1, 0, -1, 1, 1, -1, -1 };
        for (int i = 0; i < 8; i++)
        {
            Vector2 neighborCoord = new Vector2(coord.x + xC[i], coord.y + yC[i]);
            surroundTexture[i] = biomeData.biomes[getBiome(neighborCoord)].GenerationNoise;
        }
        return surroundTexture;
    }

    public int getBiome(Vector2 chunkCoord)
    {
        //Determine which biomes can be generated
        float biomeNoise = noiseData.BiomeNoise.lerpScale * NoiseQuery.RequestNoisePoint(noiseData.BiomeNoise.localSeed, noiseData.BiomeNoise.noiseScale, noiseData.BiomeNoise.octaves, noiseData.BiomeNoise.persistance, noiseData.BiomeNoise.lacunarity, chunkCoord);
        List<int> applicableBiomes = new List<int>();
        for (int i = 0; i < biomeData.biomes.Count; i++)
        {
            if (biomeData.biomes[i].noiseBiomeStartHeight <= biomeNoise && biomeNoise <= biomeData.biomes[i].noiseBiomeEndHeight)
                applicableBiomes.Add(i);
        }

        //Decide biome using the highest noise level with each biome's given settings
        int maxBiome = -1;
        float maxBiomeHeight = -1;
        for (int i = 0; i < applicableBiomes.Count; i++)
        {
            NoiseInputs candidate = biomeData.biomes[applicableBiomes[i]].BiomeGenNoise;
            float newBiomeHeight = NoiseQuery.RequestNoisePoint(candidate.localSeed, candidate.noiseScale, candidate.octaves, candidate.persistance, candidate.lacunarity, chunkCoord);

            if (newBiomeHeight > maxBiomeHeight)
            {
                maxBiomeHeight = newBiomeHeight;
                maxBiome = applicableBiomes[i];
            }

        }
        return maxBiome;
    }

    void onChunkVisibilityChanged(TerrainChunk chunk, bool isVisible)
    {
        if (isVisible)
        {
            visibleChunks.Add(chunk);
        }
        else
        {
            visibleChunks.Remove(chunk);
        }
    }
}


[System.Serializable]
public struct LODInfo
{
    public int lod;
    public float visibleDistThresh;
    public bool useForUpdates;
    public bool useForWater;
    public bool useForFakeMeshes;

    public float sqrVisibleDistThresh
    {
        get
        {
            return visibleDistThresh * visibleDistThresh;
        }
    }
}
