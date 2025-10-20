using UnityEngine;

public class SceneDestinationHandler : MonoBehaviour
{
    void Start()
    {
        string destName = TeleportData.GetDestinationPointName();

        if (!string.IsNullOrEmpty(destName))
        {
            // ... (โค้ดค้นหาจุดหมายและย้าย Player) ...
        
            // ***************************************************************
            // *** NEW: ค้นหาจุดวาปใน Scene ปัจจุบัน (ซึ่งเป็นจุดเริ่มต้นวาป) ***
            // ***************************************************************
        
            // ค้นหา GameObject ที่มีชื่อตรงกับจุดหมาย (จุดวาป)
            GameObject startingTeleporter = GameObject.Find(destName);
            if (startingTeleporter != null)
            {
                // NPC.GetComponent<GreenPatientController>() 
                SceneTeleportPoint tp = startingTeleporter.GetComponent<SceneTeleportPoint>();
                if (tp != null)
                {
                    // สั่งให้จุดวาปที่เพิ่งถูกสร้างขึ้นมาใหม่ เริ่ม Cooldown
                    tp.ResetCooldown(); 
                }
            }
            
            // 4. ล้างข้อมูลจุดหมายเพื่อป้องกันการวาปซ้ำโดยไม่ได้ตั้งใจ
            TeleportData.SetDestinationPointName("");
        }
    }
    
    // ฟังก์ชันช่วยในการค้นหา XR Rig (สมมติว่า Rig มี Tag "Player")
    private Transform GetPlayerRigTransform()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) return player.transform;

        // ถ้าสคริปต์นี้แนบอยู่กับ XR Rig ให้ใช้ตัวเอง
        if (gameObject.CompareTag("Player")) return transform;
        
        return null;
    }
}