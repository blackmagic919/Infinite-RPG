using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Entity/Animal/Carnivore")]
public class AnimalCarnivore : AnimalMovement.GenericMovementBehavior
{
    // Start is called before the first frame update
    EntityInfo info;
    public List<string> preyUser;
    [HideInInspector]
    public HashSet<string> prey;

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

    bool reachedDest = false;
    EntityBehavior dest_animal;
    CombatInfo self_combat;

    public override void BehaviorStart(EntityInfo info)
    {
        this.info = info;
        prey = new HashSet<string>(preyUser);
        if (!info.behaviorInfo.ContainsKey(typeof(CombatInfo)))
            throw new Exception("Behavior AnimalCarnivore requires Behavior CombatInfo; CombatInfo not found on entity");
        else
            self_combat = (CombatInfo)info.behaviorInfo[typeof(CombatInfo)];
    }

    public override void BehaviorUpdate()
    {
        if (!reachedDest) return;
        if (dest_animal != null && Vector3.Distance(dest_animal.info.entity.transform.position, info.entity.transform.position) < dest_animal.des_size + self_combat.reach)
        {
            if (!dest_animal.info.behaviorInfo.ContainsKey(typeof(HealthAging))) throw new Exception("Behavior of Prey requires behavior HealthAging; HealthAging not found on prey");
            if (!dest_animal.info.behaviorInfo.ContainsKey(typeof(CombatInfo))) throw new Exception("Behavior of Prey requires behavior CombatInfo; CombatInfo not found on prey");
            HealthAging preyHealth = (HealthAging)dest_animal.info.behaviorInfo[typeof(HealthAging)];
            CombatInfo preyCombat = (CombatInfo)dest_animal.info.behaviorInfo[typeof(CombatInfo)];
            if (preyHealth.attacked(self_combat.CalculateDamage(self_combat, preyCombat)))
            {
                onFinishedTask(preyHealth.consumptionValue);
                dest_animal = null;
                reachedDest = false;
            }
        }
        else {
            reachedDest = false;
            onFinishedTask(0f);
        };
    }

    // Update is called once per frame
    public override bool FindPath(RegionInfo[,] regionInfo, out AnimalMovement.Destination destination)
    {
        destination = new AnimalMovement.Destination();
        destination.pos3D = Vector3.positiveInfinity;
        destination.behavior = this;
        destination.sprint = true;
        bool hasDest = false;
        foreach (RegionInfo region in regionInfo)
        {
            foreach (EntityBehavior entity in region.entities)
            {
                if (!prey.Contains(entity.info.name))
                    continue;

                Vector3 idealDestination;
                Vector3 actualDestination;

                idealDestination = entity.info.entity.transform.position;
                actualDestination = AnimalMovement.getClosestPoint(idealDestination, UnityEngine.AI.NavMesh.AllAreas);
                if (Vector3.Distance(idealDestination, actualDestination) < entity.des_size)
                {
                    if (Vector3.Distance(destination.pos3D, info.entity.transform.position) > Vector3.Distance(actualDestination, info.entity.transform.position))
                    {
                        destination.pos3D = actualDestination;
                        dest_animal = entity;
                        hasDest = true;
                    }
                }
            }
        }
        return hasDest;
    }


    public override void onReachedDestination(Func<object, object> onFinishedTask)
    {
        this.onFinishedTask = onFinishedTask;
        reachedDest = true;
    }

    public override void CancelAction()
    {
        reachedDest = false;
        dest_animal = null;
        onFinishedTask = null;
    }
}
