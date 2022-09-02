using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Entity/Animal/Flee")]
public class AnimalFlee : AnimalMovement.GenericMovementBehavior
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

    public int ArriveRadius;
    public int fleeDistance;
    public int destinationAttempts;
    public float AverageRunDist;
    public float RunDistSpread;
    public float TimeRest;

    Func<object, object> onFinishedTask;
    bool isResting = false;
    float restedTime;

    public override void BehaviorStart(EntityInfo info)
    {
        this.info = info;
    }

    public override void BehaviorUpdate()
    {
        if (isResting)
        {
            if (restedTime > TimeRest)
            {

                isResting = false;
                onFinishedTask(true);
            }
            restedTime += Time.deltaTime;
        }
    }

    // Update is called once per frame
    public override bool FindPath(RegionInfo[,] regionInfo, out AnimalMovement.Destination destination)
    {
        destination = new AnimalMovement.Destination();

        float principalDirection;
        float principalDistance;

        Vector3 idealDestination;
        Vector3 actualDestination;

        destination.behavior = this;
        destination.sprint = true;
        destination.waitOnFailure = failWait;

        Vector3 closestPredator = Vector3.positiveInfinity;
        foreach (RegionInfo region in regionInfo)
        {
            foreach (EntityBehavior entity in region.entities)
            {
                if (!entity.info.behaviorInfo.ContainsKey(typeof(AnimalCarnivore)))
                    continue;
                if (!((AnimalCarnivore)entity.info.behaviorInfo[typeof(AnimalCarnivore)]).prey.Contains(info.name))
                    continue;

                if (Vector3.Distance(entity.info.entity.transform.position, info.entity.transform.position) < Vector3.Distance(closestPredator, info.entity.transform.position))
                    closestPredator = entity.info.entity.transform.position;
            }
        }
        if (Vector3.Distance(closestPredator, info.entity.transform.position) > fleeDistance)
            return false;
        float PredatorAngle = Vector2.Angle(new Vector2(closestPredator.x, closestPredator.z), new Vector2(info.entity.transform.position.x, info.entity.transform.position.z));
        for (int i = 0; i < destinationAttempts; i++)
        {
            principalDirection = UnityEngine.Random.Range(PredatorAngle + 90, PredatorAngle+270);

            principalDistance = UnityEngine.Random.Range(AverageRunDist - RunDistSpread, AverageRunDist + RunDistSpread);

            Vector2 pos2D = new Vector2(info.entityTrans.position.x, info.entityTrans.position.z) + principalDistance * new Vector2(Mathf.Cos(principalDirection), Mathf.Sin(principalDirection));
            idealDestination = AnimalMovement.getHitPoint(pos2D);
            actualDestination = AnimalMovement.getClosestPoint(idealDestination, UnityEngine.AI.NavMesh.AllAreas);
            if (Vector3.Distance(idealDestination, actualDestination) < ArriveRadius)
            {
                destination.pos3D = actualDestination;
                return true;
            }
        }
        return false;
    }

    public override void onReachedDestination(Func<object, object> onFinishedTask)
    {
        this.onFinishedTask = onFinishedTask;
        restedTime = 0;
        isResting = true;
    }
    public override void CancelAction()
    {
        isResting = false;
        onFinishedTask = null;
    }

}
