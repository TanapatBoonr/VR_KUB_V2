using UnityEngine;
using System.Collections.Generic;
using System.Linq; 

public class ScenarioManager : MonoBehaviour
{
    public static ScenarioManager Instance { get; private set; }

    [Header("Player Teleport Target")]
    [Tooltip("ลาก XR Rig (XR Origin) มาใส่")]
    public Transform playerRig; 

    [Header("Scenario Settings")]
    [Tooltip("ลาก GameObject หลักของแต่ละพื้นที่ (PlaneForSpawn_A, B, C...) มาใส่")]
    public List<GameObject> allScenarioPlanes = new List<GameObject>();

    // ตัวแปรสำหรับเช็คว่ามีการเลือก Scenario หลักไปแล้วหรือไม่
    private bool scenarioHasBeenSelected = false; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 1. ซ่อนพื้นที่เล่นทั้งหมดเมื่อเริ่ม Scene (เพื่อให้ Player ต้องเลือกก่อน)
        foreach (var plane in allScenarioPlanes)
        {
            if (plane != null)
            {
                plane.SetActive(false); 
            }
        }
    }

    // ----------------------------------------------------------------------
    // NEW: 1. ฟังก์ชันเช็คสถานะการเลือก (ถูกเรียกจาก ScenarioButton)
    // ----------------------------------------------------------------------
    // ใช้ตรวจสอบว่าถ้าเลือกไปแล้ว ห้าม Preview
    public bool IsScenarioSelected()
    {
        return scenarioHasBeenSelected;
    }

    // ----------------------------------------------------------------------
    // NEW: 2. ฟังก์ชันสำหรับควบคุมการพรีวิว (ถูกเรียกจาก ScenarioButton)
    // ----------------------------------------------------------------------
    public void PreviewScenario(string planeName, bool isEnteringHover)
    {
        // ถ้าเลือกไปแล้ว ห้าม Preview
        if (scenarioHasBeenSelected) return;

        // ค้นหา Plane ตามชื่อ
        GameObject planeToPreview = allScenarioPlanes.FirstOrDefault(p => p.name == planeName);
        
        if (planeToPreview != null)
        {
            // เปิด/ปิด GameObject หลักเพื่อ Preview (แสดงเฉพาะตอน Hover)
            planeToPreview.SetActive(isEnteringHover);
        }
    }


    // ----------------------------------------------------------------------
    // 3. ฟังก์ชันหลัก: SelectScenario (ถูกเรียกเมื่อกดปุ่ม)
    // ----------------------------------------------------------------------
    public void SelectScenario(string planeName)
    {
        if (scenarioHasBeenSelected) return;
        
        // ตั้งค่าสถานะการเลือก
        scenarioHasBeenSelected = true; 

        GameObject selectedPlane = null;
        
        // 1. ควบคุมการแสดงผล (ซ่อนที่ไม่ถูกเลือก, แสดงที่ถูกเลือก)
        foreach (var plane in allScenarioPlanes)
        {
            if (plane != null)
            {
                bool isSelected = (plane.name == planeName);
                
                // เปิด Plane ที่เลือก และปิด Plane ที่เหลือ
                plane.SetActive(isSelected); 

                if (isSelected)
                {
                    selectedPlane = plane;
                    Debug.Log("Selected Scenario: " + planeName);
                }
            }
        }

        // 2. ดำเนินการหลังการเลือก (Spawn NPC และ Teleport)
        if (selectedPlane != null)
        {
            // 2.1 สั่งให้ Random Spawner ทำงาน
            RandomSpawner spawner = selectedPlane.GetComponentInChildren<RandomSpawner>();
            if (spawner != null)
            {
                spawner.StartSpawning();
                Debug.Log("Spawning NPCs for " + planeName);
            }
            else
            {
                 Debug.LogError("RandomSpawner component not found in the selected plane's children!");
            }
            
            // 2.2 Teleport Player
            if (playerRig != null)
            {
                Transform teleportTarget = selectedPlane.transform.Find("TeleportTarget");
                
                if (teleportTarget != null)
                {
                    playerRig.position = teleportTarget.position;
                    playerRig.rotation = teleportTarget.rotation; 
                    Debug.Log("Player Teleported to " + planeName + " start point.");
                }
                else
                {
                    Debug.LogError("TeleportTarget child not found under " + planeName + ". Player will not move.");
                }
            }
            else
            {
                 Debug.LogError("Player Rig is not assigned in the Inspector!");
            }
        }
    }
}