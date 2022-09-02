using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Entity/Animal/Wander")]
public class AnimalWander : AnimalMovement.GenericMovementBehavior
{
    // Start is called before the first frame update
    EntityInfo info;
    public override int TaskPriority{
        get{
            return TaskPriorityUser;
        }
    }

    public override int Priority{
        get{
            return PriorityUser;
        }
    }


    public int TaskPriorityUser;
    public int PriorityUser;
    public bool failWait;

    public int destinationAttempts;

    public float TimeRest;
    public int ArriveRadius;
    public float AverageWanderDist;
    public float WanderDistSpread;

    Func<object, object> onFinishedTask;
    bool isResting = false;
    float restedTime = 0;

    public override void BehaviorStart(EntityInfo info){
        this.info = info;
    }

    public override void BehaviorUpdate(){
        if(isResting){
            if(restedTime > TimeRest){
                isResting = false;
                onFinishedTask(true);
            }
            restedTime += Time.deltaTime;
        }
    }

    // Update is called once per frame
    public override bool FindPath(RegionInfo[,] regionInfo, out AnimalMovement.Destination destination){
        destination = new AnimalMovement.Destination();
        
        float principalDirection;
        float principalDistance;

        Vector3 idealDestination;
        Vector3 actualDestination;

        destination.behavior = this;
        destination.sprint = false;
        destination.waitOnFailure = failWait;
        for (int i = 0; i < destinationAttempts; i++){
            principalDirection = UnityEngine.Random.Range(-180, 180);
            principalDistance = UnityEngine.Random.Range(AverageWanderDist - WanderDistSpread, AverageWanderDist + WanderDistSpread);

            Vector2 pos2D = new Vector2(info.entityTrans.position.x, info.entityTrans.position.z) + principalDistance * new Vector2(Mathf.Cos(principalDirection), Mathf.Sin(principalDirection));
            idealDestination = AnimalMovement.getHitPoint(pos2D);
            actualDestination = AnimalMovement.getClosestPoint(idealDestination, UnityEngine.AI.NavMesh.AllAreas);
            if(Vector3.Distance(idealDestination, actualDestination) < ArriveRadius){
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
