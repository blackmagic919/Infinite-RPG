using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TerrainChunk {
    public event System.Action<TerrainChunk, bool> onVisibilityChanged;

    public bool UpdateEntities {
        get {
            if (prevLODInd == -1) return false;
            return detailLevels[prevLODInd].useForUpdates;
        }
    }

    public Vector2 coord;

    public int areaLayer
    {
        get
        {
            if ((int)coord.x % 2 == 0 && (int)coord.y % 2 == 0)
                return 3;
            if ((int)coord.x % 2 != 0 && (int)coord.y % 2 != 0)
                return 4;
            if ((int)coord.x % 2 == 0 && (int)coord.y % 2 != 0)
                return 5;
            if ((int)coord.x % 2 != 0 && (int)coord.y % 2 == 0)
                return 6;
            return 0;
        }
    }

    public int biomeRemainPersistance = -1;
    public int biome = -1;
    public BiomeData biomeData;


    public NavMeshSurface surface;
    private List<NavMeshLinkInstance> edgeLinks = new List<NavMeshLinkInstance>();

    public bool hasBiome;

    public GameObject meshObj;
    GameObject waterMesh;
    Vector2 sampleCenter;
    Bounds bounds;

    NoiseData noiseData;
    MeshSettings meshSettings;

    MeshRenderer meshRenderer;
    MeshCollider meshCollider;
    Transform viewer;
    Texture2DArray textures;

    MeshFilter meshFilter;

    LODInfo[] detailLevels;
    LODMesh[] lodMeshes;
    LODMesh[] lodWaters;

    public TerrainData[] surroundingTerrainMesh;
    public NoiseInputs[] surroundingTerrainNoise;
    public TextureData[] surroundingTerrainColor;

    WaterGenInfo waterGenInfo;
    WaterGenerator waterGen;

    MeshFilter waterMeshFilter;
    MeshRenderer waterMeshRenderer;
    Texture thing;

    public Environment environment;

    public RandomUpdate manualUpdates;

    float[,] mapData;
    float[,] TTNMap;
    float[,][] surroundData;

    bool mapDataRecieved;
    bool surroundDataRecieved;
    bool TTNDataRecieved;
    bool plannedWater;
    int prevLODInd = -1;
    float maxViewDistance;
    float environmentDetail;


    public TerrainChunk(Vector2 coord, Transform viewer, LODInfo[] detailLevels, int environmentDetail, Action<object> biomeChangeCallback, Action<object> getBiomeCallback, Transform parent, BiomeData biomeData, NoiseData noiseData, MeshSettings meshSettings, Texture2DArray textures, RandomUpdate manualUpdates, WaterGenInfo waterGenInfo, WaterGenerator waterGen)
    {
        this.viewer = viewer;
        this.coord = coord;
        this.detailLevels = detailLevels;
        this.biomeData = biomeData;
        this.noiseData = noiseData;
        this.meshSettings = meshSettings;
        this.hasBiome = false;
        this.plannedWater = false;
        this.textures = textures;
        this.environmentDetail = environmentDetail;
        this.manualUpdates = manualUpdates;
        this.waterGenInfo = waterGenInfo;
        this.waterGen = waterGen;

        sampleCenter = coord * meshSettings.meshWorldSize / meshSettings.uniformScale;
        Vector2 position = coord * meshSettings.meshWorldSize;
        bounds = new Bounds(position, Vector2.one * meshSettings.meshWorldSize);

        //Mesh
        meshObj = new GameObject("Terrain Chunk");
        surface = meshObj.AddComponent<NavMeshSurface>();
        meshRenderer = meshObj.AddComponent<MeshRenderer>();
        meshFilter = meshObj.AddComponent<MeshFilter>();
        meshCollider = meshObj.AddComponent<MeshCollider>();
        meshObj.transform.position = new Vector3(position.x, 0, position.y);
        meshObj.transform.parent = parent;
        meshObj.gameObject.layer = LayerMask.NameToLayer("Terrain");

        //Water
        waterMesh = new GameObject("Water");
        waterMeshFilter = waterMesh.AddComponent<MeshFilter>();
        waterMeshRenderer = waterMesh.AddComponent<MeshRenderer>();
        waterMesh.transform.position = new Vector3(position.x, 0, position.y);
        waterMesh.transform.parent = meshObj.transform;

        surface.collectObjects = CollectObjects.Volume;
        surface.size = new Vector3(meshSettings.meshWorldSize - 1, 200, meshSettings.meshWorldSize - 1);
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.defaultArea = areaLayer;
        surface.overrideVoxelSize = true;
        surface.voxelSize = meshSettings.NavMeshVoxelSize;

        environment = new Environment(environmentDetail, meshSettings.meshWorldSize, coord, biomeChangeCallback, getBiomeCallback);

        setVisible(false);

        lodMeshes = new LODMesh[detailLevels.Length];
        for (int i = 0; i < detailLevels.Length; i++) {
            lodMeshes[i] = new LODMesh(detailLevels[i].lod);
            lodMeshes[i].updateCallback += updateBiomeAndChunk;
            lodMeshes[i].waterUpdateCallback += UpdateChunk;
        }
        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistThresh;
    }

    void onPlannedWater(object ret)
    {
        plannedWater = true;
        UpdateChunk();
    }
    public void createNavMeshLink(Vector3 start, Vector3 end){
        NavMeshLinkData newLink = new NavMeshLinkData();
        newLink.startPosition = start;
        newLink.endPosition = end;
        newLink.bidirectional = true;
        newLink.area = -1;
        edgeLinks.Add(NavMesh.AddLink(newLink));

    }

    public void initializeMapData(){
        NoiseInputs noiseData = biomeData.biomes[biome].GenerationNoise;
        NoiseInputs TTNData = biomeData.biomes[biome].TTNoise;

        MapGenerator.RequestData(() => Noise.GenerateNoiseMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, noiseData.localSeed, noiseData.noiseScale, noiseData.octaves, noiseData.persistance, noiseData.lacunarity, 1, sampleCenter), onMapDataRecieved);
        MapGenerator.RequestData(() => Noise.GenerateSurroundNoise(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, surroundingTerrainNoise, meshSettings.biomeBlendThreshold, 1, sampleCenter), onSurroundDataRecieved);
        MapGenerator.RequestData(() => Noise.GenerateNoiseMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, TTNData.localSeed, TTNData.noiseScale, TTNData.octaves, TTNData.persistance, TTNData.lacunarity, meshSettings.VertexesPerLineTT, sampleCenter), onTTNDataRecieved);
        Material TerrainMaterial = new Material(meshSettings.terrainShader);
        meshRenderer.material = TerrainMaterial;
        waterMeshRenderer.material = meshSettings.waterMaterial;
    }

    void ApplyTexture(BiomeData.Biome biome, Material terrainMaterial, Material WaterMaterial){
        this.thing = PureNoiseGen.Create(101, 1);
        biome.textureData.ApplyToMaterial(terrainMaterial, WaterMaterial, surroundingTerrainColor, textures, meshSettings.noiseTexture, PureNoiseGen.ConvertToTexture(this.TTNMap), coord, meshSettings.meshWorldSize);
        biome.textureData.UpdateMeshHeights(terrainMaterial, meshSettings.minHeight*meshSettings.uniformScale, meshSettings.maxHeight*meshSettings.uniformScale);
    }

    void onTTNDataRecieved(object mapData)
    {
        TTNDataRecieved = true;
        this.TTNMap = (float[,])mapData;
        ApplyTexture(biomeData.biomes[biome], meshRenderer.material, meshSettings.waterMaterial);
    }
    void onMapDataRecieved(object mapData){
        mapDataRecieved = true;
        this.mapData = (float[,])mapData;
        UpdateChunk();
    }

    void onSurroundDataRecieved(object mapData)
    {
        surroundDataRecieved = true;
        this.surroundData = (float[,][])mapData;
        UpdateChunk();
    }

    Vector2 viewerPos
    {
        get{
            return new Vector2(viewer.position.x, viewer.position.z);
        }
    }

    public void UpdateChunk()
    {
        if (!mapDataRecieved || !surroundDataRecieved || !TTNDataRecieved) return;
        float viewerEdgeDist = Mathf.Sqrt(bounds.SqrDistance(viewerPos));

        bool wasVisible = IsVisible();
        bool visible = viewerEdgeDist <= maxViewDistance;

        if (visible)
        {
            int lodIndex = 0;
            for (int i = 0; i < detailLevels.Length - 1; i++)
            {
                if (viewerEdgeDist > detailLevels[i].visibleDistThresh)
                {
                    lodIndex++;
                    continue;
                }
                break;
            }

            if (lodIndex != prevLODInd)
            {
                LODMesh lodMesh = lodMeshes[lodIndex];
                if (lodMesh.hasMesh)
                {
                    meshFilter.mesh = lodMesh.mesh;
                    meshCollider.sharedMesh = lodMesh.mesh;
                    if (hasBiome)
                        BiomeGeneration.reCalculateHeights(environment.regions);
                }
                else if (!lodMesh.hasRequestedMesh)
                {
                    lodMesh.RequestMesh(this.mapData, this.surroundData, biomeData.biomes[biome].terrainData, surroundingTerrainMesh, meshSettings);
                }
                if (lodMesh.hasWater)
                {
                    waterMeshFilter.mesh = lodMesh.waterMesh;
                }
                if (!lodMesh.hasRequestedWaterMesh && plannedWater)
                {
                    waterGenInfo.skipInc = ((lodIndex == 0) ? 1 : lodIndex * 2);
                    waterGenInfo.vertexesPerLine = meshSettings.numVertsPerLine / waterGenInfo.skipInc;
                    lodMesh.RequestWaterMesh(waterGenInfo, waterGen);
                }

                if (!plannedWater)
                {
                    if (detailLevels[lodIndex].useForWater)
                        MapGenerator.RequestIndexedData(() => waterGen.PlanOutWater(waterGenInfo), onPlannedWater, "water");
                    else
                        lodMesh.excludeWater = true;
                }

                if (lodMesh.hasMesh && (lodMesh.hasWater || lodMesh.excludeWater))
                {
                    prevLODInd = lodIndex;
                    if (hasBiome)
                        BiomeGeneration.reCalculateHeights(environment.regions);
                }
            }
        }

        if (wasVisible != visible)
        {
            setVisible(visible);
            if (onVisibilityChanged != null) onVisibilityChanged(this, visible);
        }
    }
    public void updateBiomeAndChunk(){
        UpdateChunk();

        surface.BuildNavMesh();

        if(meshCollider.sharedMesh!=null && !hasBiome){
            RegionInfo biomeItems = BiomeGeneration.GenerateBiomeItems(biomeData.biomes[biome], meshSettings.meshWorldSize, coord, meshObj.transform, environment, this);
            sortIntoEnvironment(biomeItems);
            hasBiome = true;
        }
    }

    public void sortIntoEnvironment(RegionInfo region){
        Vector2 bottomLeft = (coord - new Vector2(0.5f, 0.5f))*meshSettings.meshWorldSize; 
        
        foreach(EntityBehavior entity in region.entities){
            Vector2 pos = new Vector2(entity.info.entity.transform.position.x, entity.info.entity.transform.position.z) - bottomLeft;
            int x = (int)(pos.x/environmentDetail);
            int y = (int)(pos.y/environmentDetail);
            RegionInfo objRegion = environment.regions[y,x];
            objRegion.Add(entity);
        }
    }

    public void setVisible(bool visible){
        meshObj.SetActive(visible);
        // waterMesh.SetActive(visible);
        if(hasBiome){
            environment.CallForAllItems((RegionInfo r) => r.calculateVisibility(visible));
        }
    }

    public bool IsVisible(){
        return meshObj.activeSelf;
    }
}


class LODMesh{
    public Mesh mesh;
    public Mesh waterMesh;
    public bool hasRequestedMesh;
    public bool hasRequestedWaterMesh;
    public bool hasMesh = false;
    public bool hasWater = false;
    public bool excludeWater = false;
    public event System.Action updateCallback;
    public event System.Action waterUpdateCallback;
    int lod;


    public LODMesh(int lod){
        this.lod = lod;
    }

    void onMeshDataRecieved(object meshData){
        hasMesh = true;
        mesh = ((MeshData)meshData).createMesh();
        updateCallback();
    }

    void onMeshWaterRecieved(object newMesh)
    {
        hasWater = true;
        waterMesh = ((WaterMeshData)newMesh).createMesh();
        waterUpdateCallback();
    }


    public void RequestMesh(float[,] mapData, float[,][] surroundNoise, TerrainData meshData, TerrainData[] surroundMesh, MeshSettings meshSettings){
        hasRequestedMesh = true;
        MapGenerator.RequestData(()=> MeshGenerator.GenerateTerrainMesh(mapData, surroundNoise, meshData, surroundMesh, meshSettings, lod), onMeshDataRecieved);
    }

    public void RequestWaterMesh(WaterGenInfo genInfo, WaterGenerator generator)
    {
        hasRequestedWaterMesh = true;
        MapGenerator.RequestData(() => generator.GetMesh(genInfo), onMeshWaterRecieved);
    }

    Vector3[,] UnfoldVertices(Vector3[] FoldedVertices, int length){
        Vector3[,] UnfoldedVertices = new Vector3[length, (FoldedVertices.Length / length)];
        for(int y = 0; y < (FoldedVertices.Length / length); y++){
            for(int x = 0; x < length; x++){
                UnfoldedVertices[x, y] = FoldedVertices[y*length+x];
            }
        }
        return UnfoldedVertices;
    }

}