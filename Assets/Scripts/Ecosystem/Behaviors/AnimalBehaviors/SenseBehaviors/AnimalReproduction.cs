using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Entity/Animal/Reproduction")]
public class AnimalReproduction : AnimalMovement.GenericMovementBehavior
{
    // Start is called before the first frame update
    EntityInfo info;
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
    public bool failWait;

    public int arriveRadius;
    public int ReproductionLustLevel;
    public int pregnancyPeriod;
    public int reproductionTime;
    [HideInInspector]
    public bool isFemale;
    [HideInInspector]
    public float lust;
    [HideInInspector]
    public bool isPregnant;
    [HideInInspector]
    public bool isReproducing;

    float timePregnant;
    float timeReproducing;
    Func<object, object> onFinishedTask;
    EntityBehavior mate;


    public override void BehaviorStart(EntityInfo info)
    {
        this.info = info;
        isFemale = (UnityEngine.Random.Range(0, 2) != 1);
        isPregnant = false;
        isReproducing = false;
        lust = 0;
    }

    public override void BehaviorUpdate()
    {
        if(isReproducing)
        {
            if(timeReproducing > reproductionTime)
            {
                lust = 0;
                if (isFemale)
                {
                    isPregnant = true;
                    timePregnant = 0;
                }
                isReproducing = false;
                mate = null;
                onFinishedTask(true);
            }
            timeReproducing += Time.deltaTime;
        }
        else if (isPregnant)
        {
            if (timePregnant > pregnancyPeriod)
            {
                lust = 0;
                isPregnant = false;
                info.entity.GetComponent<EntityBehavior>().CloneEntity();
            }
            timePregnant += Time.deltaTime;
        }
        else
            lust += Time.deltaTime;
    }

    // Update is called once per frame
    public override bool FindPath(RegionInfo[,] regionInfo, out AnimalMovement.Destination destination)
    {
        destination = new AnimalMovement.Destination();

        if (lust < ReproductionLustLevel || isPregnant)
            return false;

        destination.behavior = this;
        destination.sprint = false;
        destination.waitOnFailure = failWait;
        bool hasDest = false;
        foreach (RegionInfo region in regionInfo)
        {
            foreach (EntityBehavior entity in region.entities)
            {
                if (entity.info.name != info.name)
                    continue;
                AnimalReproduction partnerInfo = (AnimalReproduction)entity.info.behaviorInfo[typeof(AnimalReproduction)];
                if (partnerInfo.isFemale == isFemale)
                    continue;
                if (partnerInfo.isPregnant)
                    continue;
                if (partnerInfo.isReproducing)
                    continue;
                if (partnerInfo.lust < partnerInfo.ReproductionLustLevel)
                    continue;

                //Reproduction must be between opposite sexes, not between those pregnant, and consensual

                Vector3 idealDestination;
                Vector3 actualDestination;

                idealDestination = entity.info.entity.transform.position;
                actualDestination = AnimalMovement.getClosestPoint(idealDestination, UnityEngine.AI.NavMesh.AllAreas);

                if (Vector3.Distance(idealDestination, actualDestination) < arriveRadius)
                {
                    if (Vector3.Distance(destination.pos3D, info.entity.transform.position) > Vector3.Distance(actualDestination, info.entity.transform.position))
                    {
                        hasDest = true;
                        destination.pos3D = actualDestination;
                        mate = entity;
                    }
                }
            }
        }
        return hasDest;
    }


    public override void onReachedDestination(Func<object, object> onFinishedTask)
    {
        if (Vector3.Distance(mate.info.entity.transform.position, info.entity.transform.position) > mate.des_size)
            onFinishedTask(false);
        else
        {
            this.onFinishedTask = onFinishedTask;
            isReproducing = true;
            timeReproducing = 0;
        }
    }

    public override void CancelAction()
    {
        isReproducing = false;
        onFinishedTask = null;
    }

}
