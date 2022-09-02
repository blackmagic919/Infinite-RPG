using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomUpdate : MonoBehaviour
{
    List<EntityBehavior> entities;
    [Range(0, 1)]
    public float updatePercentage;
    // Start is called before the first frame update
    void Start()
    {
        entities = new List<EntityBehavior>();
    }

    public void Add(EntityBehavior entity)
    {
        entities.Add(entity);
    }

    // Update is called once per frame
    void Update()
    {
        if (entities.Count == 0)
            return;

        int updateNumber = (int)(entities.Count * updatePercentage);
        if (Random.Range(0, 100) < updatePercentage*100)
            updateNumber++;
        for(int i = 0; i < updateNumber; i++)
        {
            entities[(int)Random.Range(0, entities.Count)].manualUpdate();
        }
    }
}
