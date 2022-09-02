using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Entity/Animal/Sense")]
public class SenseEnvironment : GenericBehavior
{
    public int ViewDistance;
    public List<GenericSenseBehavior> behaviors;

    EntityInfo info;



    public override void InstantiateBehaviors(Func<object, object> Instantiate, Dictionary<Type, GenericBehavior> behaviorInfo){
        List<GenericSenseBehavior> behaviorsCopy = new List<GenericSenseBehavior>();
        foreach(GenericSenseBehavior behavior in behaviors){
            GenericSenseBehavior newBehavior = (GenericSenseBehavior)Instantiate(behavior);
            newBehavior.InstantiateBehaviors(Instantiate, behaviorInfo);
            behaviorsCopy.Add(newBehavior);
            behaviorInfo.Add(newBehavior.GetType(), newBehavior);
        }
        behaviors = behaviorsCopy;
    }

    public override void BehaviorStart(EntityInfo info)
    {
        this.info = info;
        
        foreach(GenericSenseBehavior SenseBehavior in behaviors){
            SenseBehavior.BehaviorStart(info);
        }
    }

    public override void BehaviorUpdate()
    {
        RegionInfo[,] regions;

        regions = info.currentBiome.environment.RegionsInfo(new Vector2(info.entityTrans.position.x, info.entityTrans.position.z), ViewDistance);

        foreach(GenericSenseBehavior SenseBehavior in behaviors){
            SenseBehavior.BehaviorUpdate(regions);
        }
    }

    public abstract class GenericSenseBehavior: GenericBehavior{

        //called once every periodic update
        public virtual void BehaviorUpdate(RegionInfo[,] info)
        {
            
        }
    }
}
