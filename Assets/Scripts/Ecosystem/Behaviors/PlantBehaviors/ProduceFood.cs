using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Entity/Plant/Produce")]
public class ProduceFood : GenericBehavior
{
    EntityInfo info;
    public int maxProduction;
    public int harvestTime;
    //Production per random tick
    public float productionRate;
    bool oneTimeConsumption = false;

    public float produce = 0;

    public override void BehaviorStart(EntityInfo info)
    {
        this.info = info;
    }

    public override void BehaviorUpdate()
    {
        produce += productionRate;
        produce = Mathf.Min(produce, (float)maxProduction);
    }

    public void Consume(int amount){
        produce -= amount;
        if(oneTimeConsumption){
            info.currentRegion.Remove(info.entity.GetComponent<EntityBehavior>());
            Destroy(info.entity);
        }
    }
}
