using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AISpawn : MonoBehaviour
{
    [Header("Spawn info")]
    public Vector3[] SpawnTriggerLocations;
    public Vector3[] SpawnLocations;
    public GameObject[] Enemies;
    [Range(1,20)] public int SpawnAmount;
    int CurrentCount;

    private void Awake()
    {
        CurrentCount = SpawnAmount;
    }

    private void Update()
    {
        for (int i = 0; i < SpawnTriggerLocations.Length; i++)
        {
            if (Vector3.Distance(SpawnTriggerLocations[i], GameObject.FindWithTag("Player").transform.position) < 4f) // detect if player is close to a trigger
            {
                SpawnAI();
                Debug.Log("Spawn Triggered");
            }

            if(i >= SpawnTriggerLocations.Length)
            {
                i = 0;
            }
        }
    }

    void SpawnAI()
    {
        while(CurrentCount > 0)
        {
            for (int i = 0; i < Enemies.Length; i++)
            {
                if (Enemies != null)
                {
                    Instantiate(Enemies[i], SpawnLocations[Random.Range(0,SpawnLocations.Length)], Quaternion.identity);
                    CurrentCount -= 1;
                }
                else
                    Debug.LogError("No Enemy selected to spawn");
            }
        }
    }
}
