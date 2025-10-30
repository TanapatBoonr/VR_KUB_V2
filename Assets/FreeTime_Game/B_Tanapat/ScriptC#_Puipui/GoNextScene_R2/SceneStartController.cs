using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ใช้จัดการการเปิด/ปิด GameObject อื่นๆ ใน Scene เมื่อผู้เล่นเข้ามา
/// โดยอิงตามเงื่อนไขว่าผู้เล่น 'วาป' เข้ามาหรือไม่
/// </summary>
public class SceneStartController : MonoBehaviour
{
    [Header("--- Managers ที่จะถูกควบคุม ---")]
    [Tooltip("ลาก Manager GameObject (เช่น _SPAWN_MANAGER, TriageMissionManager) ที่มี Start() Logic ที่ไม่ต้องการให้ทำงานเมื่อ Teleport เข้ามา")]
    public List<GameObject> managersToDisableOnTeleport = new List<GameObject>();

    void Start()
    {
        // 1. ตรวจสอบว่าผู้เล่นถูก Teleport เข้ามาหรือไม่ โดยใช้ TeleportData
        string destinationName = TeleportData.GetDestinationPointName();
        bool isIncomingTeleport = !string.IsNullOrEmpty(destinationName);

        if (isIncomingTeleport)
        {
            // ถ้ามีการ Teleport เข้ามา (เช่น มาจาก ScoringManager/GoToNextScene)
            Debug.Log("SceneStartController: Incoming teleport detected to '" + destinationName + "'. Disabling start-up managers.");

            // 2. ปิดการทำงานของ Manager ทั้งหมดที่ถูกกำหนดไว้
            foreach (GameObject manager in managersToDisableOnTeleport)
            {
                if (manager != null)
                {
                    // การเรียก SetActive(false) จะเป็นการปิดการทำงานทั้งหมด (รวมถึง Start() ของสคริปต์นั้นๆ)
                    manager.SetActive(false);
                    Debug.Log("SceneStartController: Disabled GameObject: " + manager.name);
                }
            }
            
            // หมายเหตุ: SceneDestinationHandler.cs จะยังคงทำงานต่อเพื่อย้าย Player Rig
        }
        else
        {
            // 3. ถ้าเป็นการโหลด Scene ครั้งแรก (ไม่ได้วาป)
            Debug.Log("SceneStartController: Scene started normally. All managers remain active.");
            
            // (Manager ที่ถูกกำหนดจะยังคงทำงานตามปกติ เพราะเราไม่ได้ทำอะไรกับมัน)
        }
    }
}