using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Entity/Animal/health")]
public class HealthAging : GenericBehavior
{
    public int maxHealth;
    public float naturalRegeneration; 
    public int lifeSpan;
    public float consumptionValue;
    [HideInInspector]
    public float health;
    float age;
    EntityInfo info;

    public override void BehaviorStart(EntityInfo info)
    {
        this.info = info;
        health = maxHealth;
        age = 0;
    }

    public override void BehaviorUpdate()
    {
        if(health <= 0)
        {
            info.currentRegion.Remove(info.entity.GetComponent<EntityBehavior>());
            Destroy(info.entity);
        }
        if(age >= lifeSpan)
        {
            info.currentRegion.Remove(info.entity.GetComponent<EntityBehavior>());
            Destroy(info.entity);
        }
        age += Time.deltaTime;
        health += naturalRegeneration * Time.deltaTime;
        health = Mathf.Clamp(health, Int32.MinValue, maxHealth);
    }

    public bool attacked(float damage)
    {
        health -= damage;
        if (health <= 0)
            return true;
        return false;
    }
}
