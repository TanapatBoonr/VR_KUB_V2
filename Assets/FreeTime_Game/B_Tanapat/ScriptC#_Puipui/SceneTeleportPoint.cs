using UnityEngine;
using UnityEngine.SceneManagement; 
using System.Collections; 

public class SceneTeleportPoint : MonoBehaviour
{
    [Header("Destination Settings")]
    [Tooltip("ชื่อของ Scene ที่ต้องการเปลี่ยนไป")]
    public string destinationSceneName;

    [Tooltip("ชื่อของ GameObject ที่เป็นจุดหมายปลายทางใน Scene ปลายทาง (เช่น 'SpawnPointA')")]
    public string destinationPointName;
    
    [Tooltip("เวลาหน่วง (วินาที) ก่อนเริ่มโหลด Scene ใหม่")]
    public float delayBeforeLoad = 0.5f;

    [Header("Cooldown Settings")]
    [Tooltip("เวลาคูลดาวน์ (วินาที) หลังจากวาปเสร็จสิ้น")]
    public float teleportCooldown = 2f; 

    private bool isCoolingDown = false; // สถานะคูลดาวน์

    private void OnTriggerEnter(Collider other)
    {
        // 1. ตรวจสอบ Player และ Cooldown
        if (other.CompareTag("Player") && !isCoolingDown)
        {
            if (string.IsNullOrEmpty(destinationSceneName))
            {
                Debug.LogError("Destination Scene Name is not set on " + gameObject.name);
                return;
            }
            
            // 2. เริ่มต้น Cooldown ทันที
            isCoolingDown = true;
            
            // 3. เริ่ม Coroutine เพื่อหน่วงเวลาและเปลี่ยน Scene
            StartCoroutine(LoadNewSceneWithDelay(other.transform));
        }
    }

    IEnumerator LoadNewSceneWithDelay(Transform playerTransform)
    {
        // 1. หน่วงเวลาก่อนโหลด
        yield return new WaitForSeconds(delayBeforeLoad);

        // 2. บันทึกข้อมูลจุดหมายก่อนโหลด Scene
        TeleportData.SetDestinationPointName(destinationPointName);

        // 3. โหลด Scene ใหม่
        SceneManager.LoadScene(destinationSceneName);
        
        // ***************************************************************
        // หมายเหตุ: เนื่องจาก Scene นี้จะถูกทำลายเมื่อโหลด Scene ใหม่ 
        // Cooldown จะถูกรีเซ็ตใน Scene ปลายทางแทน (ดู SceneDestinationHandler.cs)
        // ***************************************************************
    }

    // ฟังก์ชันสำหรับเรียกใช้เมื่อต้องการรีเซ็ต cooldown
    // (ควรเรียกจาก SceneDestinationHandler ใน Scene ปลายทาง)
    public void ResetCooldown()
    {
        // ใช้ Invoke เพื่อรีเซ็ตสถานะ cooldown หลังจากผ่านไป 2 วินาที (teleportCooldown)
        // Note: Invoke จะถูกยกเลิกเมื่อ Scene ถูกทำลาย/โหลดใหม่
        Invoke("DoResetCooldown", teleportCooldown);
    }
    
    private void DoResetCooldown()
    {
        isCoolingDown = false;
        Debug.Log(gameObject.name + " Cooldown finished. Ready to teleport again.");
    }
}