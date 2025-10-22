using UnityEngine;

public class SceneDestinationHandler : MonoBehaviour
{
    void Start()
    {
        string destName = TeleportData.GetDestinationPointName();
        Transform playerRig = GetPlayerRigTransform();

        if (!string.IsNullOrEmpty(destName) && playerRig != null)
        {
            // ... (โค้ดค้นหาจุดหมายและย้าย Player) ...

            // ... (โค้ดค้นหา StartingTeleporter และ ResetCooldown) ...
        
            // ***************************************************************
            // *** NEW: วางกระเป๋าและสิ่งของลงบนเข็มขัด Player ใน Scene ใหม่ ***
            // ***************************************************************
            if (CarryOverManager.Instance != null)
            {
                CarryOverManager.Instance.PlaceCarriedItemsInNewScene(playerRig);
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