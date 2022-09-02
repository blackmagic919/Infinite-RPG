using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Entity/Animal/Herbivore")]
public class AnimalHerbivore : AnimalMovement.GenericMovementBehavior
{
    // Start is called before the first frame update
    EntityInfo info;
    public List<string> preyUser;
    HashSet<string> prey;

    //Relative only to eat 
    public override int TaskPriority
    {
        get
        {
            return TaskPriorityUser;
        }
    }

    public override int Priority
    {
        get
        {
            return PriorityUser;
        }
    }

    public int TaskPriorityUser;
    public int PriorityUser;


    Func<object, object> onFinishedTask;
    bool isEating = false;
    float timeEating;

    ProduceFood dest_plant;

    public override void BehaviorStart(EntityInfo info)
    {
        this.info = info;
        prey = new HashSet<string>(preyUser);
    }

    public override void BehaviorUpdate()
    {
        if (isEating)
        {
            if (timeEating > dest_plant.harvestTime)
            {
                isEating = false;
                dest_plant.produce -= (float)onFinishedTask(dest_plant.produce);
            }
            timeEating += Time.deltaTime;
        }
    }

    // Update is called once per frame
    public override bool FindPath(RegionInfo[,] regionInfo, out AnimalMovement.Destination destination)
    {
        destination = new AnimalMovement.Destination();
        destination.pos3D = Vector3.positiveInfinity;
        destination.behavior = this;
        destination.sprint = false;
        bool hasDest = false;
        foreach (RegionInfo region in regionInfo)
        {
            foreach(EntityBehavior entity in region.entities)
            {
                if (!prey.Contains(entity.info.name))
                    continue;
                if (!entity.info.behaviorInfo.ContainsKey(typeof(ProduceFood)))
                    throw new Exception("Expected Entity " + entity.info.name + " to have behavior ProduceFood");

                Vector3 idealDestination;
                Vector3 actualDestination;

                idealDestination = entity.info.entity.transform.position;
                actualDestination = AnimalMovement.getClosestPoint(idealDestination, UnityEngine.AI.NavMesh.AllAreas);
                if (Vector3.Distance(idealDestination, actualDestination) < entity.des_size)
                {
                    if (Vector3.Distance(destination.pos3D, info.entity.transform.position) > Vector3.Distance(actualDestination, info.entity.transform.position))
                    {
                        destination.pos3D = actualDestination;
                        hasDest = true;
                        dest_plant = ((ProduceFood)entity.info.behaviorInfo[typeof(ProduceFood)]);
                    }
                }
            }
        }
        return hasDest;
    }


    public override void onReachedDestination(Func<object, object> onFinishedTask)
    {
        this.onFinishedTask = onFinishedTask;
        timeEating = 0;
        isEating = true;

    }

    public override void CancelAction()
    {
        isEating = false;
        onFinishedTask = null;
    }
}
