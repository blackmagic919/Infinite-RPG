using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Entity/Data")]
public class EntityData : ScriptableObject
{
    public new string name;
    public bool randomUpdate;
    public float des_size;
    public float generationOffset;
    public GameObject obj_mesh;
    public List<GenericBehavior> behaviors;
}

//General layout of behavior
public abstract class GenericBehavior : ScriptableObject{
    private EntityInfo info;

    public virtual void BehaviorStart(EntityInfo info){
        this.info = info;
    }

    public virtual void BehaviorUpdate(){

    }

    public virtual bool RandomUpdate(bool correct){
        return false;
    }

    public virtual void InstantiateBehaviors(Func<object, object> Instantiate, Dictionary<Type, GenericBehavior> behaviorInfo){
        //Deep copy
    }

    //Helper classes
}



public class EntityInfo{
    public TerrainChunk currentBiome;
    public GameObject entity;
    public Bounds regionBounds;
    public RegionInfo currentRegion;
    public Dictionary<Type, GenericBehavior> behaviorInfo = new Dictionary<Type, GenericBehavior>(); 

    public string name;

    public float placeOffset;
    public Transform entityTrans{
        get{
            return entity.transform;
        }
    }
}