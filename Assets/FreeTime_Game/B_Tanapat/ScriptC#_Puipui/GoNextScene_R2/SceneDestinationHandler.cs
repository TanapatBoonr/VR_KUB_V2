using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SceneDestinationHandler : MonoBehaviour
{
    // ตรวจสอบเมื่อ Scene โหลดเสร็จ
    void Start()
    {
        // 1. ดึงชื่อจุดหมายปลายทางที่ถูกบันทึกไว้
        string destName = TeleportData.GetDestinationPointName();
        
        if (string.IsNullOrEmpty(destName))
        {
            // ถ้าไม่มีชื่อจุดหมายถูกบันทึก แปลว่าไม่ได้มาจาก Teleport/ScoringManager
            Debug.Log("TeleportData: No specific destination set. Player stays at default spawn point.");
            return;
        }

        // 2. ค้นหา Player Rig
        Transform playerRig = GetPlayerRigTransform();
        
        if (playerRig == null)
        {
             // ******* จุดนี้คือจุดที่มักมีปัญหา - เราต้องตรวจสอบให้แน่ใจว่า Tag "Player" ถูกตั้งค่าแล้ว *******
             Debug.LogError("Player Rig (with Tag 'Player' or name 'XR Rig') not found in the scene! Cannot teleport.");
             // ล้างข้อมูลแม้ว่าการวาปจะล้มเหลว
             TeleportData.SetDestinationPointName(""); 
             return;
        }

        // 3. ค้นหา GameObject จุดหมายปลายทางด้วยชื่อ
        GameObject destinationObject = GameObject.Find(destName);
        
        if (destinationObject != null)
        {
            Transform targetPoint = destinationObject.transform;

            // 4. ย้าย Player Rig ไปยังตำแหน่งและทิศทางของจุดหมาย
            playerRig.position = targetPoint.position;
            playerRig.rotation = targetPoint.rotation;
            
            Debug.Log("Player Teleported to Destination: " + destName + " at " + targetPoint.position);
        }
        else
        {
            Debug.LogError("Destination Point '" + destName + "' not found in the scene! Player remains at default spawn.");
        }
    
        // 5. ล้างข้อมูลจุดหมายเพื่อป้องกันการวาปซ้ำเมื่อโหลด Scene อื่นที่ไม่เกี่ยวข้อง
        TeleportData.SetDestinationPointName("");
    }
    
    /// <summary>
    /// ค้นหา Transform ของ Player Rig หลักใน Scene
    /// </summary>
    /// <returns>Transform ของ Player Rig หรือ null ถ้าหาไม่พบ</returns>
    private Transform GetPlayerRigTransform()
    {
        // ค้นหา XR Rig ด้วย Tag "Player" (แนะนำให้ใช้ Tag นี้)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) return player.transform;
        
        // สำรอง: ลองค้นหาจากชื่อ GameObject หลักที่ใช้ทั่วไป
        GameObject rig = GameObject.Find("XR Rig");
        if (rig != null) return rig.transform;

        return null;
    }
}
