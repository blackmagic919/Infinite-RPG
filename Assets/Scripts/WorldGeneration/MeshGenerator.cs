using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float[,][] surroundNoise, TerrainData terrainData, TerrainData[] surroundingTerrain, MeshSettings meshSettings, int levelOfDetail){

        int skipInc = (levelOfDetail == 0)? 1: levelOfDetail * 2;
        int numVertsPerLine = meshSettings.numVertsPerLine;
        
        Vector2 topLeft = new Vector2(-1,1) * meshSettings.meshWorldSize/2f;

        MeshData meshData = new MeshData(numVertsPerLine, skipInc);
    
        int[,] vertexIndicesMap = new int[numVertsPerLine,numVertsPerLine];
        int meshVertexIndex = 0;
        int outOfMeshVertInd = -1;

        for(int y = 0; y < numVertsPerLine; y++){
            for(int x = 0; x < numVertsPerLine; x++){
                bool isOutOfMeshVert = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine-1;
                bool isSkippedVert = x>2 && x < numVertsPerLine-3 && y > 2 && y < numVertsPerLine - 3 && ((x-2) % skipInc != 0 || (y-2) % skipInc != 0);
                if(isOutOfMeshVert){
                    vertexIndicesMap[x,y] = outOfMeshVertInd;
                    outOfMeshVertInd--;
                }
                else if(!isSkippedVert){
                    vertexIndicesMap[x,y] = meshVertexIndex;
                    meshVertexIndex++;
                }

            }
        }

        for(int y = 0; y < numVertsPerLine; y++){
            for(int x = 0; x < numVertsPerLine; x++){
                bool isSkippedVert = x>2 && x < numVertsPerLine-3 && y > 2 && y < numVertsPerLine - 3 && ((x-2) % skipInc != 0 || (y-2) % skipInc != 0);
                if(isSkippedVert) continue;

                //Type of Vertex
                bool isOutOfMeshVert = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine-1;
                bool isMeshEdgeVertex = (y == 1 || y == numVertsPerLine-2 || x == 1 || x == numVertsPerLine-2) && !isOutOfMeshVert;
                bool isMainVertex = (x-2)%skipInc == 0 && (y-2)%skipInc == 0 && !isOutOfMeshVert && !isMeshEdgeVertex;
                bool isEdgeConnectionVertex = (y == 2 || y == numVertsPerLine-3 || x == 2 || x == numVertsPerLine-3) && !isOutOfMeshVert && !isMeshEdgeVertex && !isMainVertex;

                //Place mesh Vertex
                int vertexI = vertexIndicesMap[x,y];
                Vector2 percent = new Vector2(x-1, y-1) / (numVertsPerLine-3);
                Vector2 vertexPos2d = topLeft　+ new Vector2(percent.x, -percent.y) * meshSettings.meshWorldSize;

                float height;
                height = EvaluateHeight(surroundNoise[x, y], surroundingTerrain, GetHeight(heightMap[x, y], terrainData.meshHeightMultiplier, terrainData.meshHeightCurve), x, y, numVertsPerLine, meshSettings.biomeBlendThreshold, meshSettings.biomeClipThreshold);


                if(isEdgeConnectionVertex){
                    bool isVertical = x == 2 || x == numVertsPerLine - 3;
                    int dstToMainVertA = ((isVertical) ? y-2 : x-2) % skipInc;
                    int dstToMainVertB = skipInc - dstToMainVertA;

                    float dstPercentFromAToB = dstToMainVertA /(float) skipInc;

                    Vector2 heightCoordsMainVertA = new Vector2((isVertical) ? x : x - dstToMainVertA, (isVertical) ? y - dstToMainVertA : y);
                    Vector2 heightCoordsMainVertB = new Vector2((isVertical) ? x : x + dstToMainVertB, (isVertical) ? y + dstToMainVertB : y);


                    float heightMainVertA = GetHeight(heightMap[(int)heightCoordsMainVertA.x, (int)heightCoordsMainVertA.y], terrainData.meshHeightMultiplier, terrainData.meshHeightCurve);
                    float heightMainVertB = GetHeight(heightMap[(int)heightCoordsMainVertB.x, (int)heightCoordsMainVertB.y], terrainData.meshHeightMultiplier, terrainData.meshHeightCurve);


                    heightMainVertA = EvaluateHeight(surroundNoise[x, y], surroundingTerrain, heightMainVertA, (int)heightCoordsMainVertA.x, (int)heightCoordsMainVertA.y, numVertsPerLine, meshSettings.biomeBlendThreshold, meshSettings.biomeClipThreshold);
                    heightMainVertB = EvaluateHeight(surroundNoise[x, y], surroundingTerrain, heightMainVertB, (int)heightCoordsMainVertB.x, (int)heightCoordsMainVertB.y, numVertsPerLine, meshSettings.biomeBlendThreshold, meshSettings.biomeClipThreshold);                
                    height = heightMainVertA * (1-dstPercentFromAToB) + heightMainVertB * dstPercentFromAToB;
                }

                meshData.addVertex(new Vector3(vertexPos2d.x, height, vertexPos2d.y), percent, vertexI);

                bool createTriangle = x < numVertsPerLine-1 && y < numVertsPerLine-1 && (!isEdgeConnectionVertex || (x!=2) && (y!=2));
                

                if(createTriangle){
                    int currentInc = (isMainVertex && x != numVertsPerLine-3 && y != numVertsPerLine-3)? skipInc : 1;

                    int a = vertexIndicesMap[x,y];
                    int b = vertexIndicesMap[x + currentInc, y];
                    int c = vertexIndicesMap[x, y + currentInc];
                    int d = vertexIndicesMap[x + currentInc, y + currentInc];
                    meshData.addTriangle(a, d, c);
                    meshData.addTriangle(d, a, b);
                } 
            }
        }
        meshData.BakeNormals();

        return meshData;
    }

    /* Make sense? it didn't to me too
     (0,0)------------------------------------------------------------>
      |   ——————————————————————————————————————————————————————————  -
      |  |8    |                    1                       |      5|    Blend
      |  |     |                                            |       |    Distance
      |  |-----+--------------------------------------------+-------| - 
      |  |     |                                            |       |
      |  |     |                                            |       |
      |  |     |                                            |       |
      |  |     |                                            |       |
      |  |     |                                            |       |
      |  |     |                                            |       |
      |  |     |                                            |       |
      |  |  4  |                Chunk xy                    |   2   |
      |  |     |                                            |       |
      |  |     |                                            |       |
      |  |     |                                            |       |
      |  |     |                                            |       |
      |  |     |                                            |       |
      |  |     |                                            |       |
      |  |     |                                            |       |
      |  |-----+--------------------------------------------+-------|
      |  |     |                                            |       |
      |  |     |                   3                        |       |
      |  |7    |                                            |      6|
      |   ——————————————————————————————————————————————————————————   
      \/
     */

    public static float EvaluateHeight(float[] surroundNoise, TerrainData[] surroundingTerrain, float baseHeight, float x, float y, float size, int threshold, float clipDist) {
        float[] refBoundX = new float[8] { x, size, x, 0, size, size, 0, 0};
        float[] refBoundY = new float[8] { 0, y, size, y, 0, size, size, 0};

        float weightedHeight = 0f;
        float totalWeights = 0f;
        for (int i = 4; i < 8; i++)
        {
            float distanceX = Mathf.Abs(refBoundX[i] - x);
            float distanceY = Mathf.Abs(refBoundY[i] - y);


            if (distanceX <= clipDist && distanceY <= clipDist)
            {
                float heightCorner = GetHeight(surroundNoise[i], surroundingTerrain[i].meshHeightMultiplier, surroundingTerrain[i].meshHeightCurve);

                float heightCC = GetHeight(surroundNoise[i-4], surroundingTerrain[i-4].meshHeightMultiplier, surroundingTerrain[i-4].meshHeightCurve);

                float heightC = GetHeight(surroundNoise[(i - 3) % 4], surroundingTerrain[(i - 3) % 4].meshHeightMultiplier, surroundingTerrain[(i - 3) % 4].meshHeightCurve);

                return (baseHeight + heightCorner + heightC + heightCC) / 4f;
            } 
        }

        for (int i = 0; i < 8; i++) {

            float distance = Vector2.Distance(new Vector2(refBoundX[i], refBoundY[i]), new Vector2(x,y));
            
            if (distance < threshold) {
                if (surroundNoise[i] == -1)
                    Debug.Log('1');
                if (distance <= clipDist)
                    return 0.5f * baseHeight + 0.5f * GetHeight(surroundNoise[i], surroundingTerrain[i].meshHeightMultiplier, surroundingTerrain[i].meshHeightCurve);
                /*  
                 When on the boundary, the height is halfway in between one mesh and another(so both meshes may go to the same value).
                 When influenced by multiple sides, take the average of their heights influenced by the distance to the edge, such that it is more heavy the closer it is to the edge


                   ------------------ =  (-------------------------average height between this side and the base height from fully base influence to half of each influence------------------------------) *  (----weight----)
                   Total weighted height ((------------------------------------------height of this side-----------------------------------------) * (-----local weight from 0 to 0.5-----) + baseHeight * (---local weight from 1 to 0.5---))  *  (weight(higher when closer to side))
                 */
                weightedHeight += (GetHeight(surroundNoise[i], surroundingTerrain[i].meshHeightMultiplier, surroundingTerrain[i].meshHeightCurve) * ((threshold-distance)/(2*threshold)) + baseHeight * ((threshold+distance) / (2*threshold))) * (threshold-distance);
                totalWeights += (threshold - distance);
            }
        }
        if (totalWeights == 0f) return baseHeight;
        return (weightedHeight / totalWeights);
    }

    public static float GetHeight(float noiseValue, float heightMultiplier, AnimationCurve heightCurve)
    {
        AnimationCurve newHeightCurve = new AnimationCurve(heightCurve.keys);
        return newHeightCurve.Evaluate(noiseValue) * heightMultiplier;
    }
}

public class MeshData 
{
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;

    Vector3[] outOfMeshVerts;
    int[] outOfMeshTriangles;
    Vector3[] bakedNormals;

    public void BakeNormals(){
        bakedNormals = CalculateNormals();
    }

    int triangleInd;
    int outOfMeshTriInd; 

    public MeshData(int numVertsPerLine, int skipInc){

        int numMeshEdgeVerts = (numVertsPerLine-2)*4 - 4;
        int numEdgeConnectionVerts = (skipInc-1)*(numVertsPerLine-5)/skipInc * 4;
        int numMainVertsPerLine  = (numVertsPerLine-5)/skipInc + 1;
        int numMainVerts = numMainVertsPerLine*numMainVertsPerLine;

        vertices = new Vector3[numMeshEdgeVerts + numEdgeConnectionVerts + numMainVerts];
        uvs = new Vector2[vertices.Length];

        int numMeshEdgeTriangles = 8 * (numVertsPerLine-4);
        int numMainTriangles = (numMainVertsPerLine-1) * (numMainVertsPerLine-1) * 2;

        triangles = new int[(numMeshEdgeTriangles + numMainTriangles) * 3];

        outOfMeshVerts = new Vector3[numVertsPerLine*4 - 4];
        outOfMeshTriangles = new int[24*(numVertsPerLine-2)]; 
    } 

    public void addVertex(Vector3 vertexPos, Vector2 uv, int vertexInd){
        if(vertexInd < 0){
            outOfMeshVerts[-vertexInd-1] = vertexPos; 
        }
        else{
            vertices[vertexInd] = vertexPos;
            uvs[vertexInd] = uv;
        }
    }
    
    public void addTriangle(int a, int b, int c){
        if(a < 0 || b < 0 || c < 0){//border triangle
            outOfMeshTriangles[outOfMeshTriInd] = a;
            outOfMeshTriangles[outOfMeshTriInd+1] = b;
            outOfMeshTriangles[outOfMeshTriInd+2] = c;
            outOfMeshTriInd+=3;
            return;
        }
        triangles[triangleInd] = a;
        triangles[triangleInd+1] = b;
        triangles[triangleInd+2] = c;
        triangleInd+=3;
    }

    Vector3[] CalculateNormals(){
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        for(int normalTriInd = 0; normalTriInd < triangles.Length; normalTriInd+=3){
            int vertexIndA = triangles[normalTriInd];
            int vertexIndB = triangles[normalTriInd+1];
            int vertexIndC = triangles[normalTriInd+2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndA, vertexIndB, vertexIndC);
            vertexNormals[vertexIndA] += triangleNormal;
            vertexNormals[vertexIndB] += triangleNormal;
            vertexNormals[vertexIndC] += triangleNormal;
        }

        for(int normalTriInd = 0; normalTriInd < outOfMeshTriangles.Length; normalTriInd+=3){
            int vertexIndA = outOfMeshTriangles[normalTriInd];
            int vertexIndB = outOfMeshTriangles[normalTriInd+1];
            int vertexIndC = outOfMeshTriangles[normalTriInd+2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndA, vertexIndB, vertexIndC);
            if(vertexIndA >= 0) vertexNormals[vertexIndA] += triangleNormal;
            if(vertexIndB >= 0) vertexNormals[vertexIndB] += triangleNormal;
            if(vertexIndC >= 0) vertexNormals[vertexIndC] += triangleNormal;
        }

        for(int i = 0; i < vertexNormals.Length; i++){
            vertexNormals[i].Normalize();
        }
        return vertexNormals;
    }

    Vector3 SurfaceNormalFromIndices(int indA, int indB, int indC){
        Vector3 pointA = (indA < 0)? outOfMeshVerts[-indA - 1] : vertices[indA];
        Vector3 pointB = (indB < 0)? outOfMeshVerts[-indB - 1] : vertices[indB];
        Vector3 pointC = (indC < 0)? outOfMeshVerts[-indC - 1] : vertices[indC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;

        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public Mesh createMesh(){
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.normals = bakedNormals;
        return mesh;
    }
}

