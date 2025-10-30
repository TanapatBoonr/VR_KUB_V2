using UnityEngine;

// สคริปต์นี้ควรแนบอยู่กับ GameObject ที่มี SceneDestinationHandler หรืออยู่ใน Scene
public class SpawnPointManager : MonoBehaviour
{
    [Tooltip("จุดเกิดเริ่มต้นสำหรับรอบที่ 1 (ภารกิจสำรวจโรงงาน)")]
    public Transform spawnPointA;
    [Tooltip("จุดเกิดสำหรับรอบที่ 2 (ภารกิจช่วยผู้บาดเจ็บ)")]
    public Transform spawnPointB;
    [Tooltip("Transform ของ Player Rig (XR Rig)")]
    public Transform playerRig;
    
    void Start()
    {
        // อ่านชื่อจุดหมายจากข้อมูลข้าม Scene
        string destName = TeleportData.GetDestinationPointName();

        if (playerRig == null)
        {
            Debug.LogError("Player Rig is not assigned to SpawnPointManager. Cannot set spawn position.");
            return;
        }

        Transform targetSpawn = null;
        
        // ตรวจสอบว่าผู้เล่นมาจาก Scene อื่น (มี Destination Name)
        if (!string.IsNullOrEmpty(destName))
        {
            // ถ้ามาจาก Pick_Item และต้องการไป SpawnPointB
            if (destName == "SpawnPointB" && spawnPointB != null)
            {
                targetSpawn = spawnPointB;
            }
            // สามารถเพิ่มเงื่อนไขอื่นๆ ได้ที่นี่
        }
        
        // ถ้าไม่มี Destination Name หรือไม่ตรงกับเงื่อนไขใดๆ ให้ไป SpawnPointA เสมอ (ตามที่คุณต้องการในรอบที่ 1)
        if (targetSpawn == null && spawnPointA != null)
        {
            targetSpawn = spawnPointA;
        }

        // ย้ายผู้เล่นไปยังจุดเกิดที่กำหนด
        if (targetSpawn != null)
        {
            playerRig.position = targetSpawn.position;
            playerRig.rotation = targetSpawn.rotation;
            Debug.Log($"Player spawned at: {targetSpawn.name}");
        }
        
        // ล้างข้อมูลจุดหมายหลังใช้งาน
        TeleportData.SetDestinationPointName("");

        // *** ตรวจสอบว่า CarryOverManager ควรทำงานไหม (ถ้ามาจาก Pick_Item) ***
        // เนื่องจากภารกิจที่ 2 (จุด B) คือการเลือก Plane เราอาจไม่จำเป็นต้องให้ CarryOverManager ทำงานใน TriageRoom
    }
}