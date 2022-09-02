using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterGenerator
{
    Dictionary<Vector2, WaterBody> chunkWaterDict = new Dictionary<Vector2, WaterBody>();
    int[] dX = new int[] { 0, -1, 0, 1, 1, 1, -1, -1};
    int[] dY = new int[] { -1, 0, 1, 0, 1, -1, 1, -1};

    //Helpers
    public static int inv(int s, int c) { return (s - 1) - c; }

    private float convertPos(int s1, int s2, float index)
    {
        return (int)((index / (float)s1) * s2);
    }


    public bool PlanOutWater(WaterGenInfo info)
    {
        //Get Current Info
        BiomeData.Biome biome = info.biomeList[info.requestBiome(info.chunkCoord)];

        WaterBody chunk;
        NoiseInputs[] surroundNoiseInp = info.requestSNoise(info.chunkCoord);
        TerrainData[] surroundTerrain = info.requestSMesh(info.chunkCoord);
        if (chunkWaterDict.ContainsKey(info.chunkCoord))
        {
            chunk = chunkWaterDict[info.chunkCoord];
        }
        else
        {
            WaterPoint[,] waterChunk = new WaterPoint[info.meshSettings.VertexesPerLineWater, info.meshSettings.VertexesPerLineWater];
            Vector2 sampleCenter = info.chunkCoord*info.meshSettings.meshWorldSize / info.meshSettings.uniformScale; 

            float[,] noiseChunk = Noise.GenerateNoiseMap(info.meshSettings.numVertsPerLine, info.meshSettings.numVertsPerLine, biome.GenerationNoise.localSeed, biome.GenerationNoise.noiseScale, biome.GenerationNoise.octaves, biome.GenerationNoise.persistance, biome.GenerationNoise.lacunarity, info.skipInc, sampleCenter);
            float[,][] surroundNoise = Noise.GenerateSurroundNoise(info.meshSettings.numVertsPerLine, info.meshSettings.numVertsPerLine, surroundNoiseInp, info.meshSettings.biomeBlendThreshold, info.skipInc, sampleCenter);
            chunk = new WaterBody(waterChunk, noiseChunk, surroundNoise);
            chunkWaterDict.Add(info.chunkCoord, chunk);
        }

        reCalibrateChunk(chunk);
        
        for (int x = 0; x < chunk.water.GetLength(0); x++)
        {
            for (int y = 0; y < chunk.water.GetLength(1); y++)
            {
                Vector2 maxRenderPos = new Vector2(convertPos(info.meshSettings.VertexesPerLineWater, info.meshSettings.numVertsPerLine, x), convertPos(info.meshSettings.VertexesPerLineWater, info.meshSettings.numVertsPerLine, y));
                if (chunk.water[x, y] != null)
                    continue;
                if (info.QueryHeight(maxRenderPos.x, maxRenderPos.y, info.meshSettings.numVertsPerLine, MeshGenerator.GetHeight(chunk.noise[x, y], biome.terrainData.meshHeightMultiplier, biome.terrainData.meshHeightCurve), chunk.surroundNoise[x, y], surroundTerrain) > biome.waterHeight)
                    continue;
                WaterPoint newPoint = new WaterPoint(biome.waterHeight, 0, false);
                if (!searchArea(biome.maximumWaterArea, newPoint, new Vector2(x, y), info))
                    newPoint.failed = true;
            }
        }
        chunk.alreadyGenerated = true;
        return true;
    }

    /*
        Plan out water, to make sure no water conflicts
        Queue for BFS generic search
     */
    public bool searchArea(long maxArea, WaterPoint OrigBody, Vector2 startCoord, WaterGenInfo info)
    {
        Queue<Vector4> endPoints = new Queue<Vector4>();
        endPoints.Enqueue(new Vector4(startCoord.x, startCoord.y, info.chunkCoord.x, info.chunkCoord.y));

        while (endPoints.Count > 0 && OrigBody.Area < maxArea)
        {
            Vector4 endPoint = endPoints.Dequeue();
            Vector2 chunkCoord = new Vector2(endPoint.z, endPoint.w);
            BiomeData.Biome biome = info.biomeList[info.requestBiome(chunkCoord)];
            WaterBody waterChunk = chunkWaterDict[chunkCoord];
            TerrainData[] surroundTerrain = info.requestSMesh(chunkCoord);

            WaterPoint water = waterChunk.water[(int)endPoint.x, (int)endPoint.y];
            if (water != null && !water.failed)
            {
                if (water.WaterHeight == OrigBody.WaterHeight)
                    continue;
                else
                    return false;
            }

            if (waterChunk.alreadyGenerated)
                return false;

            waterChunk.water[(int)endPoint.x, (int)endPoint.y] = OrigBody; //so edges are connected to mesh

            Vector2 maxRenderPos = new Vector2(convertPos(info.meshSettings.VertexesPerLineWater, info.meshSettings.numVertsPerLine, endPoint.x), convertPos(info.meshSettings.VertexesPerLineWater, info.meshSettings.numVertsPerLine, endPoint.y));
            if (info.QueryHeight(maxRenderPos.x, maxRenderPos.y, info.meshSettings.numVertsPerLine, MeshGenerator.GetHeight(waterChunk.noise[(int)endPoint.x, (int)endPoint.y], biome.terrainData.meshHeightMultiplier, biome.terrainData.meshHeightCurve), waterChunk.surroundNoise[(int)endPoint.x, (int)endPoint.y], surroundTerrain) > OrigBody.WaterHeight)
                continue;

            for (int i = 0; i < 4; i++)
            {
                int x = (int)endPoint.x + dX[i];
                int y = (int)endPoint.y + dY[i];
                Vector4 newPoint = new Vector4(x, y, endPoint.z, endPoint.w);

                if (x < 0)
                {
                    newPoint.z--;
                    newPoint.x = info.meshSettings.VertexesPerLineWater - 1;
                }
                if (x >= info.meshSettings.VertexesPerLineWater)
                {
                    newPoint.z++;
                    newPoint.x = 0;
                }
                if (y < 0)
                {
                    newPoint.w++;
                    newPoint.y = info.meshSettings.VertexesPerLineWater - 1;
                }
                if (y >= info.meshSettings.VertexesPerLineWater)
                {
                    newPoint.w--;
                    newPoint.y = 0;
                }
                chunkCoord = new Vector2(newPoint.z, newPoint.w);

                if (!chunkWaterDict.ContainsKey(chunkCoord))
                {
                    BiomeData.Biome newBiome = info.biomeList[info.requestBiome(chunkCoord)];
                    Vector2 sampleCenter = chunkCoord * info.meshSettings.meshWorldSize / info.meshSettings.uniformScale;

                    NoiseInputs[] surroundNoiseInp = info.requestSNoise(chunkCoord);

                    float[,] noiseChunk = Noise.GenerateNoiseMap(info.meshSettings.numVertsPerLine, info.meshSettings.numVertsPerLine, newBiome.GenerationNoise.localSeed, newBiome.GenerationNoise.noiseScale, newBiome.GenerationNoise.octaves, newBiome.GenerationNoise.persistance, newBiome.GenerationNoise.lacunarity, info.skipInc, sampleCenter);
                    float[,][] surroundNoise = Noise.GenerateSurroundNoise(info.meshSettings.numVertsPerLine, info.meshSettings.numVertsPerLine, surroundNoiseInp, info.meshSettings.biomeBlendThreshold, info.skipInc, sampleCenter);
                    chunkWaterDict.Add(chunkCoord, new WaterBody(new WaterPoint[info.meshSettings.VertexesPerLineWater, info.meshSettings.VertexesPerLineWater], noiseChunk, surroundNoise));
                }

                endPoints.Enqueue(newPoint);
            }
            OrigBody.Area++;
        }

        if (OrigBody.Area < maxArea)
            return true;

        return false;
    }

    public int reCalibrateChunk(WaterBody chunk)
    {
        int water = 0;
        for (int x = 0; x < chunk.water.GetLength(0); x++)
        {
            for (int y = 0; y < chunk.water.GetLength(1); y++)
            {
                if (chunk.water[x, y] == null)
                    continue;
                else if (chunk.water[x, y].failed)
                    chunk.water[x, y] = null;
                else
                    water++;
            }
        }
        return water;
    }


  

    //This is multithreaded; Create actual Mesh
    public WaterMeshData GetMesh(WaterGenInfo info)
    {
        WaterBody chunk;
        lock (chunkWaterDict)
        {
            chunk = chunkWaterDict[info.chunkCoord];
        }

        reCalibrateChunk(chunk);

        BiomeData.Biome biome = info.biomeList[info.requestBiome(info.chunkCoord)];
        TerrainData[] surroundTerrain = info.requestSMesh(info.chunkCoord);
        NoiseInputs[] surroundNoiseInp = info.requestSNoise(info.chunkCoord);
        int[,] waterMap = new int[info.vertexesPerLine + 1, info.vertexesPerLine + 1];
        Vector2 sampleCenter = info.chunkCoord * info.meshSettings.meshWorldSize / info.meshSettings.uniformScale;
        int fullSize = info.meshSettings.numVertsPerLine + info.skipInc;
        float[,] noiseMap = Noise.GenerateNoiseMap(fullSize, fullSize, biome.GenerationNoise.localSeed, biome.GenerationNoise.noiseScale, biome.GenerationNoise.octaves, biome.GenerationNoise.persistance, biome.GenerationNoise.lacunarity, info.skipInc, sampleCenter);
        float[,][] surroundNoise = Noise.GenerateSurroundNoise(fullSize, fullSize, surroundNoiseInp, info.meshSettings.biomeBlendThreshold, info.skipInc, sampleCenter);

        for (int x = 0; x < chunk.water.GetLength(0); x++)
        {
            for(int y = 0; y < chunk.water.GetLength(1); y++)
            {
                if (chunk.water[x, y] == null)
                    continue;                
                Vector2 detailedPos = new Vector2(convertPos(chunk.water.GetLength(1), info.vertexesPerLine, x), convertPos(chunk.water.GetLength(0), info.vertexesPerLine, y));
                
                floodArea(waterMap, noiseMap, surroundNoise, surroundTerrain, chunk.water[x, y].WaterHeight, detailedPos, biome, info);
            }
        }

        List<Vector3> verticesList = new List<Vector3>();
        List<Vector2> uvsList = new List<Vector2>();
        int[,] vertexPos = new int[info.vertexesPerLine + 1, info.vertexesPerLine + 1];
        Vector2 topLeft = new Vector2(-1, 1) * (info.meshSettings.meshWorldSize / 2);
        for (int x = 0; x < waterMap.GetLength(0); x++)
        {
            for (int y = 0; y < waterMap.GetLength(1); y++)
            {
                if (waterMap[x, y] == 0)  continue;
                float vertXPos;
                float vertYPos;
                if (x == info.vertexesPerLine + 1 || y == info.vertexesPerLine + 1)
                {
                    vertXPos = (x == info.vertexesPerLine + 1) ? info.meshSettings.meshWorldSize : ((float)x / info.vertexesPerLine) * info.meshSettings.meshWorldSize;
                    vertYPos = (y == info.vertexesPerLine + 1) ? info.meshSettings.meshWorldSize : ((float)y / info.vertexesPerLine) * info.meshSettings.meshWorldSize;
                    uvsList.Add(topLeft + new Vector2(vertXPos, -vertYPos));
                    verticesList.Add(new Vector3(topLeft.x + vertXPos, waterMap[x, y], topLeft.y - vertYPos));
                }
                else
                {
                    vertXPos = ((float)x / info.vertexesPerLine) * info.meshSettings.meshWorldSize;
                    vertYPos = ((float)y / info.vertexesPerLine) * info.meshSettings.meshWorldSize;
                    uvsList.Add(topLeft + new Vector2(vertXPos, -vertYPos));
                    verticesList.Add(new Vector3(topLeft.x + vertXPos, waterMap[x, y], topLeft.y - vertYPos));
                }
                vertexPos[x, y] = verticesList.Count-1;
            }
        }

        //build triangles
        List<int> triList = new List<int>();
        for (int x = 0; x < waterMap.GetLength(0); x++)
        {
            for (int y = 0; y < waterMap.GetLength(1); y++)
            {
                if (waterMap[x, y] == 0) continue;

                int[] triangle = new int[2];
                bool valid = true;
                for (int i = 0; i < 2; i++)
                {
                    int xd = x + dX[i];
                    int yd = y + dY[i];
                    if (xd < 0 || xd >= waterMap.GetLength(0))
                        valid = false;
                    if (yd < 0 || yd >= waterMap.GetLength(1))
                        valid = false;
                    if (valid && waterMap[xd, yd] == 0)
                        valid = false;
                    if (!valid)
                        break;
                    triangle[i] = vertexPos[xd, yd];
                }
                if (valid)
                {
                    triList.Add(triangle[1]);
                    triList.Add(triangle[0]);
                    triList.Add(vertexPos[x, y]);
                }

                triangle = new int[2];
                valid = true;
                for (int i = 2; i < 4; i++)
                {
                    int xd = x + dX[i];
                    int yd = y + dY[i];
                    if (xd < 0 || xd >= waterMap.GetLength(0))
                        valid = false;
                    if (yd < 0 || yd >= waterMap.GetLength(1))
                        valid = false;
                    if (valid && waterMap[xd, yd] == 0)
                        valid = false;
                    if (!valid)
                        break;
                    triangle[i - 2] = vertexPos[xd, yd];
                }
                if (valid)
                {
                    triList.Add(triangle[1]);
                    triList.Add(triangle[0]);
                    triList.Add(vertexPos[x, y]);
                }
            }
        }
        //convert into int list to int array
        int[] triangles = new int[triList.Count];
        Vector3[] vertices = new Vector3[verticesList.Count];
        Vector2[] uvs = new Vector2[uvsList.Count];
        for (int i = 0; i < triList.Count; i++)
            triangles[i] = triList[i];
        for (int i = 0; i < verticesList.Count; i++)
            vertices[i] = verticesList[i];
        for (int i = 0; i < uvsList.Count; i++)
            uvs[i] = uvs[i];

        return new WaterMeshData(vertices, triangles, uvs);

    }
    //Returns amount of water

    public void floodArea(int[,] map, float[,] noiseMap, float[,][]surroundNoise, TerrainData[] st, int height, Vector2 start, BiomeData.Biome biome, WaterGenInfo info)
    {
        Queue<Vector2> waterEdge = new Queue<Vector2>();
        waterEdge.Enqueue(start);

        while (waterEdge.Count != 0)
        {
            Vector2 point = waterEdge.Dequeue();

            Vector2 maxRenderPos = new Vector2(convertPos(info.vertexesPerLine+1, info.meshSettings.numVertsPerLine, point.x), convertPos(info.vertexesPerLine+1, info.meshSettings.numVertsPerLine, point.y));

            if (point.y < 0 || point.y >= map.GetLength(1))
                continue;
            if (point.x < 0 || point.x >= map.GetLength(0))
                continue;
            if (map[(int)point.x, (int)point.y] != 0) //If this was c++ I could've left it without the check :(
                continue;
            
            map[(int)point.x, (int)point.y] = height;
            
            if (info.QueryHeight(maxRenderPos.x, maxRenderPos.y, info.meshSettings.numVertsPerLine, MeshGenerator.GetHeight(noiseMap[(int)point.x, (int)point.y], biome.terrainData.meshHeightMultiplier, biome.terrainData.meshHeightCurve), surroundNoise[(int)point.x, (int)point.y], st) > height) //so edges are connected
                continue;
            
            for (int i = 0; i < 8; i++)
            {
                waterEdge.Enqueue(new Vector2(point.x + dX[i], point.y + dY[i]));
            }
        }
    }
}

public class WaterBody
{
    public WaterPoint[,] water;
    public float[,] noise;
    public float[,][] surroundNoise;

    public bool alreadyGenerated = false;

    public WaterBody(WaterPoint[,] w, float[,] n, float[,][] sN)
    {
        water = w;
        noise = n;
        surroundNoise = sN;
    } 
}
public class WaterPoint{
    public int WaterHeight;
    public int Area;
    public bool failed = false;
    public bool reCalibrated;

    public WaterPoint(int w, int l, bool r)
    {
        WaterHeight = w;
        Area = l;
        reCalibrated = r;
    }
}

public class WaterMeshData
{
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;

    public WaterMeshData(Vector3[] v, int[] t, Vector2[] u)
    {
        vertices = v;
        triangles = t;
        uvs = u;
    }
    public Mesh createMesh()
    {
        //It's all flat, so the normals are simple
        Vector3[] normals = new Vector3[vertices.Length];

        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = Vector3.up;
        }
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals = normals;
        
        mesh.RecalculateBounds();
        return mesh;
    }

}
public class WaterGenInfo{
    public Vector2 chunkCoord;
    public List<BiomeData.Biome> biomeList;
    public Func<Vector2, int> requestBiome;
    public Func<Vector2, TerrainData[]> requestSMesh;
    public Func<Vector2, NoiseInputs[]> requestSNoise;
    public MeshSettings meshSettings;
    public int skipInc;
    public int vertexesPerLine;

    public WaterGenInfo(Vector2 chunkCoord, List<BiomeData.Biome> biomeList, Func<Vector2, int> requestBiome, Func<Vector2, TerrainData[]> requestSMesh, Func<Vector2, NoiseInputs[]> requestSNoise, MeshSettings meshSettings, int skipInc)
    {
        this.chunkCoord = chunkCoord;
        this.biomeList = biomeList;
        this.requestBiome = requestBiome;
        this.requestSMesh = requestSMesh;
        this.requestSNoise = requestSNoise;
        this.meshSettings = meshSettings;
        this.skipInc = skipInc;
    }
    
    public float QueryHeight(float x, float y, float size, float baseHeight, float[] surroundNoise, TerrainData[] surroundingTerrain)
    {
        return MeshGenerator.EvaluateHeight(surroundNoise, surroundingTerrain, baseHeight, x, y, size, meshSettings.biomeBlendThreshold, meshSettings.biomeClipThreshold);
    }
}