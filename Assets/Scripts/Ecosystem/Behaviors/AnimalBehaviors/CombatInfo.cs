using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Entity/Animal/combat.info")]
public class CombatInfo : GenericBehavior
{
    public float defense;
    [Range(0,1)]
    public float dodgeChance;

    public float damage;
    public float attackWait; //Time(sec) between attacks
    public float reach;
    [Range(0,1)]
    public float criticalHitChance;
    public float criticalHitMultiplier;

    float timePassed = 0;
    public float CalculateDamage(CombatInfo attacker, CombatInfo defender)
    {
        if(timePassed > attackWait)
        {
            timePassed = 0;
            return (Random.Range(0, 100) < 100* defender.dodgeChance) ? attacker.damage * ((Random.Range(0, 100) < 100*criticalHitChance) ? criticalHitMultiplier : 1) - defender.defense : 0;
        }
        return 0;
    }

    public override void BehaviorStart(EntityInfo info)
    {
        return;
    }

    public override void BehaviorUpdate()
    {
        timePassed += Time.deltaTime;
        return;
    }
}
