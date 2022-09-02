using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Entity/Animal/Movement")]
public class AnimalMovement : SenseEnvironment.GenericSenseBehavior
{
    Destination destination;
    //Order of list decides priority, destination task is always one priority above moving to a task 
    //That is to say, if a prey eating is below a prey running from a predator, the prey will not move towards food when eating but will run from predator if eating

    public List<GenericMovementBehavior> behaviors;
    HashSet<GenericBehavior> failedBehaviors;

    UnityEngine.AI.NavMeshAgent agent;

    public int MaxBiomeJumpDistance;

    public float sprintSpeed;
    public float walkSpeed;

    bool arrivedAtDestination = true;
    bool completingTask = false;
    bool hasDestination = false;
    bool goingOffBiome = false;

    public float stoppingDistance;
    EntityInfo info;



    public override void InstantiateBehaviors(Func<object, object> Instantiate, Dictionary<Type, GenericBehavior> behaviorInfo){
        List<GenericMovementBehavior> behaviorsCopy = new List<GenericMovementBehavior>();
        foreach(GenericMovementBehavior behavior in behaviors){
            GenericMovementBehavior newBehavior = (GenericMovementBehavior)Instantiate(behavior);
            newBehavior.InstantiateBehaviors(Instantiate, behaviorInfo);
            behaviorsCopy.Add(newBehavior);
            behaviorInfo.Add(newBehavior.GetType(), newBehavior);
        }
        behaviors = behaviorsCopy;
    }


    public override void BehaviorStart(EntityInfo info)
    {
        this.info = info;
        failedBehaviors = new HashSet<GenericBehavior>();
        foreach (GenericMovementBehavior behavior in behaviors){
            behavior.BehaviorStart(info);
        }
        
        destination = null;
        info.entity.transform.position = getClosestPoint(info.entity.transform.position, (int)Mathf.Pow(2, info.currentBiome.areaLayer));
        agent = info.entity.AddComponent<UnityEngine.AI.NavMeshAgent>();
        agent.stoppingDistance = stoppingDistance;
    }

    public static Vector3 getHitPoint(Vector2 point2D){
        RaycastHit hit;
        Ray ray = new Ray(new Vector3(point2D.x, 200, point2D.y), Vector3.down);
        Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain"));
        return hit.point;
    }

    public static Vector3 getClosestPoint(Vector3 idealPoint, System.Int32 area){
        UnityEngine.AI.NavMeshHit hit;
        if(!UnityEngine.AI.NavMesh.SamplePosition(idealPoint, out hit, Mathf.Infinity, area))
            return Vector3.zero;
        return hit.position;
    }

    public override void BehaviorUpdate(RegionInfo[,] regInfo)
    {
        Destination newDestination = new Destination();
        bool hasNewDestination = false;

        foreach (GenericMovementBehavior behavior in behaviors)
        {
            behavior.BehaviorUpdate();
        }

        foreach (GenericMovementBehavior behavior in behaviors){
            if (failedBehaviors.Contains(behavior)) continue;
            if(!arrivedAtDestination && behavior.Priority <= this.destination.behavior.Priority) continue;
            if(hasNewDestination && behavior.Priority <= newDestination.behavior.Priority) continue;
            if(completingTask && behavior.Priority <= this.destination.behavior.TaskPriority) continue;
            

            Destination destinationAttempt;
            if(behavior.FindPath(regInfo, out destinationAttempt)){
                newDestination = destinationAttempt;
                hasNewDestination = true;
            }
        }

        if(hasNewDestination){
            if (completingTask) destination.behavior.CancelAction();
            destination = newDestination;
            hasDestination = true;
            arrivedAtDestination = false;
            completingTask = false;
            goingOffBiome = !info.currentBiome.environment.environmentBound.Contains(new Vector2(destination.pos3D.x, destination.pos3D.z));
            agent.SetDestination(destination.pos3D);
            if(destination.sprint) agent.speed = sprintSpeed;
            else agent.speed = walkSpeed;
        }


        if (hasDestination && !agent.pathPending && !completingTask)
        {
            arrivedAtDestination = agent.remainingDistance <= agent.stoppingDistance;
            if (arrivedAtDestination)
            {
                bool arrivedAtTrueDestination = Vector3.Distance(destination.pos3D, info.entity.transform.position) <= agent.stoppingDistance;
                if (goingOffBiome & !arrivedAtTrueDestination)
                {
                    Vector3 offMeshPoint = Vector3.positiveInfinity;
                    bool canJump = false;
                    for (int i = 3; i <= 6; i++)
                    {
                        Vector3 newPoint = getClosestPoint(info.entity.transform.position, (int)Mathf.Pow(2, i));
                        if (Vector3.Distance(newPoint, info.entity.transform.position) > MaxBiomeJumpDistance)
                            continue;
                        if (Vector3.Distance(destination.pos3D, newPoint) < Vector3.Distance(destination.pos3D, offMeshPoint))
                        {
                            offMeshPoint = newPoint;
                            canJump = true;
                            if (i == info.currentBiome.areaLayer)
                                canJump = false;
                        }
                    }

                    if (canJump)
                    {
                        info.currentBiome.createNavMeshLink(info.entity.transform.position, offMeshPoint);
                        agent.Warp(offMeshPoint);

                        info.currentRegion.Remove(info.entity.GetComponent<EntityBehavior>());
                        info.currentBiome.environment.reCalculateRegion(new Vector2(info.entityTrans.position.x, info.entityTrans.position.z), info.entity.GetComponent<EntityBehavior>());
                        agent.SetDestination(destination.pos3D);

                        goingOffBiome = !info.currentBiome.environment.environmentBound.Contains(new Vector2(destination.pos3D.x, destination.pos3D.z));

                        arrivedAtDestination = false;
                        return;
                    }
                }
                if (arrivedAtTrueDestination)
                {
                    completingTask = true;
                    destination.behavior.onReachedDestination(onFinishedTask);
                    failedBehaviors.Clear();
                }
                else if(destination.waitOnFailure)
                    failedBehaviors.Add(destination.behavior);
                arrivedAtDestination = true;
            }
        }
    }



    private object onFinishedTask(object succeeded){
        hasDestination = false;
        completingTask = false;
        return succeeded;
    }

    public class Destination{
        public Vector3 pos3D;
        public GenericMovementBehavior behavior;
        public bool waitOnFailure = true;
        public bool sprint = false;
    }



    public abstract class GenericMovementBehavior : SenseEnvironment.GenericSenseBehavior
    {
        public abstract int TaskPriority { get; }
        public abstract int Priority { get; }

        //true if a destination is set
        public virtual bool FindPath(RegionInfo[,] info, out Destination destination){
            destination = new Destination();//some point
            return true;
        }

        public virtual void onReachedDestination(Func<object, object> onFinishedTask)
        {

        }

        public virtual void CancelAction()
        {

        }
    }

}
