using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Entity/Animal/Hunger")]
public class AnimalEat : AnimalMovement.GenericMovementBehavior
{
    EntityInfo info;

    public List<AnimalMovement.GenericMovementBehavior> behaviors;
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

    AnimalMovement.Destination destination = new AnimalMovement.Destination();
    public int hungerMax;
    public int foodSearchHunger;
    Func<object, object> onFinishedTask;

    //Seconds 
    public float hungerDepletionRate;
    public int hungerDamageLimit;
    public float hungerDamageRate;
    float hunger;

    public override void InstantiateBehaviors(Func<object, object> Instantiate, Dictionary<Type, GenericBehavior> behaviorInfo)
    {
        List<AnimalMovement.GenericMovementBehavior> behaviorsCopy = new List<AnimalMovement.GenericMovementBehavior>();
        foreach (AnimalMovement.GenericMovementBehavior behavior in behaviors)
        {
            AnimalMovement.GenericMovementBehavior newBehavior = (AnimalMovement.GenericMovementBehavior)Instantiate(behavior);
            newBehavior.InstantiateBehaviors(Instantiate, behaviorInfo);
            behaviorsCopy.Add(newBehavior);
            behaviorInfo.Add(newBehavior.GetType(), newBehavior);
        }
        behaviors = behaviorsCopy;
    }

    public override void BehaviorStart(EntityInfo info)
    {
        this.info = info;
        hunger = hungerMax;
        foreach (AnimalMovement.GenericMovementBehavior behavior in behaviors)
        {
            behavior.BehaviorStart(info);
        }
    }

    public override void BehaviorUpdate(){
        foreach (AnimalMovement.GenericMovementBehavior behavior in behaviors)
        {
            behavior.BehaviorUpdate();
        }

        hunger -= Time.deltaTime * hungerDepletionRate;
        hunger = Mathf.Clamp(hunger, Int32.MinValue, hungerMax);
        if(hunger <= hungerDamageLimit)
        {
            try
            {
                ((HealthAging)info.behaviorInfo[typeof(HealthAging)]).health -= hungerDamageRate * Time.deltaTime;
            }
            catch
            {
                throw new Exception("Behavior AnimalEat requires behavior HealthAging; Behavior HealthAging not found on entity");
            }
        }
    }

    // Update is called once per frame
    public override bool FindPath(RegionInfo[,] regionInfo, out AnimalMovement.Destination destination){
        bool hasNewDestination = false;
        destination = new AnimalMovement.Destination();
        if (hunger > foodSearchHunger)
            return false;

        foreach (AnimalMovement.GenericMovementBehavior behavior in behaviors)
        {
            if (hasNewDestination && behavior.Priority <= destination.behavior.Priority) continue;

            AnimalMovement.Destination destinationAttempt;
            if (behavior.FindPath(regionInfo, out destinationAttempt))
            {
                destination = destinationAttempt;
                hasNewDestination = true;
            }
        }

        if(hasNewDestination)
            this.destination.behavior = destination.behavior;
        destination.waitOnFailure = failWait;
        destination.behavior = this;
        return hasNewDestination;
    }

    private object onFinishedEating(object hungerIncrease)
    {
        float prevHunger = hunger;
        hunger += (float)hungerIncrease;
        Mathf.Clamp(hunger, Int32.MinValue, hungerMax);
        onFinishedTask(true);
        return hunger - prevHunger;
    }
    public override void onReachedDestination(Func<object, object> onFinishedTask)
    {
        this.onFinishedTask = onFinishedTask;
        destination.behavior.onReachedDestination(onFinishedEating);
    }

    public override void CancelAction()
    {
        destination.behavior.CancelAction();
        onFinishedTask = null;
    }
}
