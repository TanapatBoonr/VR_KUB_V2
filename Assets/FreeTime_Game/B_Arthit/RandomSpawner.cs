using UnityEngine;
using System.Collections.Generic;
using System.Linq; 

public class RandomSpawner : MonoBehaviour
{
    [Header("Prefab คนทั้งหมด")]
    public GameObject[] personPrefabs; 

    [Header("ตำแหน่ง Spawn Point ทั้งหมด")]
    public Transform[] spawnPoints; 
    
    // ลบ Start() ออก เพื่อให้ ScenarioManager ควบคุมการ Spawn

    // เปลี่ยนชื่อฟังก์ชันให้เหมาะสมกับการถูกเรียกภายนอก
    public void StartSpawning() 
    {
        SpawnPeople();
    }

    private void SpawnPeople()
    {
        if (personPrefabs.Length < spawnPoints.Length)
        {
            Debug.LogWarning("จำนวน Prefab คนน้อยกว่าจำนวนจุด Spawn! (Plane: " + gameObject.transform.parent.name + ")");
            return;
        }

        // ... (Logic การสุ่มและ Instantiate เหมือนเดิม) ...
        
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
            
            // Spawn เป็น Child ของ Spawn Point เพื่อความเป็นระเบียบ
            Instantiate(personPrefabs[chosenPersonIndex], spawnPoints[i].position, spawnPoints[i].rotation, spawnPoints[i]);
        }
    }
}