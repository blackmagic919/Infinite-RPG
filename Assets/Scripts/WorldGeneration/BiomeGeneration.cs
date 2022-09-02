using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BiomeGeneration : MonoBehaviour
{

    public static RegionInfo GenerateBiomeItems(BiomeData.Biome biome, float Gsize, Vector2 coord, Transform parent, Environment environment, TerrainChunk parentBiome){
        List<Vector4> points;
        RegionInfo biomeItems = new RegionInfo();

        points = PoissonSample.GeneratePoints(biome, (int)Gsize, biome.rejectionSample, coord);

        foreach(Vector4 point in points){
            GameObject newObj = Instantiate(biome.entities[(int)point.w].entityData.obj_mesh) as GameObject;

            newObj.transform.parent = parent;
            newObj.transform.position = new Vector3(point.x, point.y, point.z) + Vector3.up * biome.entities[(int)point.w].entityData.generationOffset;
            EntityBehavior newBehavior = newObj.AddComponent<EntityBehavior>();
            newBehavior.Initialize(newObj, biome.entities[(int)point.w].entityData, parentBiome);
            biomeItems.entities.Add(newBehavior);
        }
        return biomeItems;
    }

    public static void reCalculateHeights(RegionInfo[,] Environment){
        for(int x = 0; x < Environment.GetLength(0); x++){
            for(int y = 0; y < Environment.GetLength(1); y++){
                RegionInfo biomeItems = Environment[x,y];
                foreach(EntityBehavior entity in biomeItems.entities){
                    RaycastHit hit;
                    Ray ray = new Ray(new Vector3(entity.info.entityTrans.position.x, 200, entity.info.entityTrans.position.z), Vector3.down);
                    entity.info.entity.SetActive(false);
                    Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain"));
                    entity.info.entityTrans.position = hit.point + Vector3.up*entity.info.placeOffset;
                    entity.info.entity.SetActive(true);
                }
            }
        }
    }
}


