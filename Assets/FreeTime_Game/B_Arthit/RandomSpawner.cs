using UnityEngine;
using System.Collections.Generic;

public class RandomSpawner : MonoBehaviour
{
    [Header("Prefab คนทั้งหมด")]
    public GameObject[] personPrefabs; 

    [Header("ตำแหน่ง Spawn Point ทั้งหมด")]
    public Transform[] spawnPoints; 

    private void Start()
    {
        SpawnPeople();
    }

    void SpawnPeople()
    {
        if (personPrefabs.Length < spawnPoints.Length)
        {
            Debug.LogWarning("จำนวน Prefab คนน้อยกว่าจำนวนจุด Spawn!");
            return;
        }

        
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < personPrefabs.Length; i++)
        {
            availableIndices.Add(i);
        }

        
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            int randomIndex = Random.Range(0, availableIndices.Count);
            int chosenPersonIndex = availableIndices[randomIndex];
            availableIndices.RemoveAt(randomIndex);

            Instantiate(personPrefabs[chosenPersonIndex], spawnPoints[i].position, spawnPoints[i].rotation);
        }
    }
}