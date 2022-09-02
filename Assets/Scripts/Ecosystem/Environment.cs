using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Environment
{
    public RegionInfo[,] regions; 
    public Bounds environmentBound;

    Action<object> biomeChangeCallback;
    Action<object> getBiomeCallback;
    Vector2 coord;
    float worldSize;
    float regionsSize;

    public Environment(float regionsSize, float worldSize, Vector2 coord, Action<object> biomeChangeCallback, Action<object> getBiomeCallback){
        this.coord = coord;
        this.worldSize = worldSize;
        this.regionsSize = regionsSize;
        this.biomeChangeCallback = biomeChangeCallback;
        this.getBiomeCallback = getBiomeCallback;
        

        int numberOfChunks = (int)Mathf.Ceil((worldSize/regionsSize));

        regions = new RegionInfo[numberOfChunks, numberOfChunks];
        environmentBound = new Bounds(coord*worldSize, Vector2.one*worldSize);

        for(int yOff = 0; yOff < numberOfChunks; yOff++){
            for(int xOff = 0; xOff < numberOfChunks; xOff++){
                regions[yOff, xOff] = new RegionInfo();
                regions[yOff, xOff].setBounds(((coord - Vector2.one/2) * worldSize) + (new Vector2(xOff, yOff) + Vector2.one/2)*regionsSize, regionsSize);
            }
        }
    }


    public void CallForAllItems(Action<RegionInfo> calledFunction){
        for(int x = 0; x < regions.GetLength(0); x++){
            for(int y = 0; y < regions.GetLength(1); y++){
                calledFunction(regions[x,y]);
            }
        }
    }

    public class callbackInfo{
        public Vector2 parameter;
        public RegionInfo ret;
        public callbackInfo(Vector2 parameter, RegionInfo ret){
            this.parameter = parameter;
            this.ret = ret;
        }
    }

    //Returns concatenated Regions Info for the nearby regions
    public RegionInfo[,] RegionsInfo(Vector2 entity_pos, float viewDistance){
        Vector2 bottomLeft = (coord - Vector2.one/2)*worldSize;
        Vector2 pos = entity_pos - bottomLeft;
        int RegionX = (int)(pos.x/regionsSize);
        int RegionY = (int)(pos.y/regionsSize);

        int regionViewDist = Mathf.RoundToInt(viewDistance/regionsSize);

        RegionInfo[,] regionsInView = new RegionInfo[regionViewDist*2,regionViewDist*2];
        for(int y = (int)(RegionY - regionViewDist); y < (int)(RegionY+regionViewDist); y++){
            for(int x = (int)(RegionX - regionViewDist); x < (int)(RegionX+regionViewDist); x++){
                RegionInfo region = new RegionInfo();
                if(y < 0 || y >= regions.GetLength(0) || x < 0 || x >= regions.GetLength(1)){
                    callbackInfo cInf = new callbackInfo(new Vector2(x, y)*regionsSize + bottomLeft, region);
                    getBiomeCallback(cInf);
                    region = cInf.ret;
                }
                else region = regions[x,y];

                regionsInView[y-(RegionY - regionViewDist), x-(RegionX - regionViewDist)] = region;
            }
        }
        return regionsInView;
    }

    public void reCalculateRegion(Vector2 entity_pos, EntityBehavior obj){
        if(!environmentBound.Contains(entity_pos)){
            biomeChangeCallback(obj);
            return;
        }
        Vector2 bottomLeft = (coord - Vector2.one/2)*worldSize;
        Vector2 pos = entity_pos - bottomLeft;
        int x = (int)(pos.x/regionsSize);
        int y = (int)(pos.y/regionsSize);
        regions[y, x].Add(obj);
    }

    
}

public class RegionInfo
{
    public HashSet<EntityBehavior> entities;
    public Bounds regionBounds;

    public RegionInfo(){
        this.entities = new HashSet<EntityBehavior>();
    }

    public void setBounds(Vector2 coord, float worldSize){
        regionBounds = new Bounds(coord, Vector2.one*worldSize);
    }

    public void Add(EntityBehavior entity){
        this.entities.Add(entity);
        entity.info.currentRegion = this;
        entity.info.regionBounds = regionBounds;
    }

    public void Remove(EntityBehavior entity){
        this.entities.Remove(entity);
    }

    public object calculateVisibility(bool visible){
        foreach(EntityBehavior entity in entities){
            entity.SetActive(visible);
        }
        return null;
    }

    public object Update(){
        EntityBehavior[] entityList = new EntityBehavior[entities.Count];

        int i = 0;
        foreach(EntityBehavior entity in entities){
            entityList[i] = entity;
            i++;
        }
        foreach(EntityBehavior entity in entityList){
            entity.Update();
        }
        return null;
    }

    
    public void Death(EntityBehavior obj){
        entities.Remove(obj);
        UnityEngine.Object.Destroy(obj.info.entity);
    }

}

