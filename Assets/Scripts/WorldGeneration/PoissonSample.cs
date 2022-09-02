using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoissonSample : MonoBehaviour
{
    public static List<Vector4> GeneratePoints(BiomeData.Biome biome, int Gsize, int numSamplesBeforeReject, Vector2 biomeCoord){
        int maxRadius = 0;

        int[,] grid = new int[Gsize, Gsize];
        List<Vector4> points = new List<Vector4>(); //x,y,z = pos, w = biomeItemIndex
        
        bool candidateAccepeted = true;
        while(candidateAccepeted){
            candidateAccepeted = false;
            for(int i = 0; i < numSamplesBeforeReject; i++)
            {
                Vector2 candidate = new Vector2(Mathf.RoundToInt(Random.Range(0, Gsize)), Mathf.RoundToInt(Random.Range(0, Gsize)));

                RaycastHit hit;
                Ray ray = new Ray(new Vector3((biomeCoord.x * Gsize) + (candidate.x - Gsize/2), 200, (biomeCoord.y * Gsize) + (candidate.y - Gsize/2)), Vector3.down);
                Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain"));

                int entityInd = DetermineGeneration(hit.point.y, biome.entities);
                if(entityInd == -1) continue;
                int radius = biome.entities[entityInd].radius;
                if(radius > maxRadius) maxRadius = radius;

                if(IsValid(candidate, maxRadius, radius, points, grid, (biomeCoord - Vector2.one/2)*Gsize, biome.entities)){
                    points.Add(new Vector4(hit.point.x, hit.point.y, hit.point.z, entityInd));
                    grid[(int)(candidate.x), (int)(candidate.y)] = points.Count;
                    candidateAccepeted = true;
                    break;
                }
            }
        }
        return points;
    }

    static int DetermineGeneration(float height, List<BiomeData.EntityGenerationData> candidates){
        int candidateInd = -1;
        float maxProbability = -1;
        for(int i = 0; i < candidates.Count; i++){
            BiomeData.EntityGenerationData candidate = candidates[i];
            if(height < candidate.generationMinHeight || height > candidate.generationMaxHeight) continue;
            float probability = Random.Range(0, candidate.frequency);
            if(probability > maxProbability){
                maxProbability = probability;
                candidateInd = i;
            }
        }
        return candidateInd;
    }

    static bool IsValid(Vector2 candidate, int maxRadius, int radius, List<Vector4> points, int[,] grid, Vector2 bottomLeft, List<BiomeData.EntityGenerationData> entities){
        int checkRadius = maxRadius + radius + 2;
        int searchStartX = Mathf.Max(0, (int)candidate.x - checkRadius);
        int searchEndX = Mathf.Min((int)candidate.y + checkRadius, grid.GetLength(0)-1);
        int searchStartY = Mathf.Max(0, (int)candidate.y - checkRadius);
        int searchEndY = Mathf.Min((int)candidate.y + checkRadius, grid.GetLength(1)-1);


        for(int x = searchStartX; x <= searchEndX; x++){
            for(int y = searchStartY; y <= searchEndY; y++){
                int pointInd = grid[x,y]-1;
                if(pointInd != -1){
                    float sqrDst = (candidate - (new Vector2(points[pointInd].x, points[pointInd].z)-bottomLeft)).sqrMagnitude;
                    int otherRadius = entities[(int)points[pointInd].w].radius;
                    if(sqrDst < (radius+otherRadius) * (radius+otherRadius)) return false;
                }
            }
        }
        return true;
    }
}
