using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class EntityBehavior : MonoBehaviour
{
    List<GenericBehavior> behaviors = new List<GenericBehavior>();
    public EntityInfo info = new EntityInfo();
    public float des_size;
    bool randomUpdate;
    EntityData createData;
    GameObject blackCopy;

    public void CloneEntity()
    {
        GameObject newObj = Instantiate(blackCopy) as GameObject;
        newObj.transform.parent = info.entity.transform.parent;
        newObj.transform.position = info.entity.transform.position;
        EntityBehavior newBehavior = newObj.AddComponent<EntityBehavior>();
        newBehavior.Initialize(newObj, createData, info.currentBiome);
        info.currentBiome.environment.reCalculateRegion(new Vector2(info.entityTrans.position.x, info.entityTrans.position.z), newBehavior);
    }
    public void InstantiateBehaviors(List<GenericBehavior> behaviors, Func<object, object> Instantiate){
        foreach(GenericBehavior behavior in behaviors){
            GenericBehavior newBehavior = (GenericBehavior)Instantiate(behavior);
            newBehavior.InstantiateBehaviors(Instantiate, info.behaviorInfo);
            this.behaviors.Add(newBehavior);
            info.behaviorInfo.Add(newBehavior.GetType(), newBehavior);
        }
    }


    public void Initialize(GameObject entity, EntityData entityData, TerrainChunk parentBiome){
        info.entity = entity;
        info.currentBiome = parentBiome;
        info.placeOffset = entityData.generationOffset;
        info.name = entityData.name;
        this.des_size = entityData.des_size;
        randomUpdate = entityData.randomUpdate;
        blackCopy = entityData.obj_mesh;
        createData = entityData;


        InstantiateBehaviors(entityData.behaviors, (object thing) => {
            return Instantiate((GenericBehavior)thing) as ScriptableObject;
        });

        if (randomUpdate)
            info.currentBiome.manualUpdates.Add(this);

        foreach (GenericBehavior behavior in behaviors){
            behavior.BehaviorStart(info);
        }
    }

    public void SetActive(bool active){
        info.entity.SetActive(active);
    }

    public bool RandomUpdate(bool calledCorrectly){
        foreach(GenericBehavior behavior in behaviors){
            behavior.RandomUpdate(true);
        }
        return true;
    }

    public void Update()
    {
        if(info.currentRegion == null) return;
        if(!info.currentBiome.UpdateEntities) return;
        if(!info.regionBounds.Contains(new Vector2(info.entityTrans.position.x, info.entityTrans.position.z))){
            info.currentRegion.Remove(this);
            info.currentBiome.environment.reCalculateRegion(new Vector2(info.entityTrans.position.x, info.entityTrans.position.z), this);
        }

        if (randomUpdate)
            return;

        foreach(GenericBehavior behavior in behaviors){
            behavior.BehaviorUpdate();
        }
    }

    public void manualUpdate()
    {
        foreach (GenericBehavior behavior in behaviors)
        {
            behavior.BehaviorUpdate();
        }
    }
}
