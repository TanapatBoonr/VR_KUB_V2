using System.Collections;
using UnityEngine;

public class PlayerTeleportController : MonoBehaviour
{
    // ตั้งค่าเวลา Cooldown ใน Inspector
    public float cooldownTime = 2.0f; 
    private bool canTeleport = true; 

    // ฟังก์ชันนี้จะถูกเรียกเมื่อผู้เล่นเข้าสู่ Trigger
    private void OnTriggerEnter(Collider other)
    {
        // ตรวจสอบว่าวัตถุที่ชนมี Tag เป็น "TeleportPoint" และสามารถวาปได้หรือไม่
        if (other.CompareTag("TeleportPoint") && canTeleport)
        {
            // ดึงสคริปต์ TeleportPoint จากวัตถุที่ชน
            TeleportPoint teleportPoint = other.GetComponent<TeleportPoint>();

            if (teleportPoint != null)
            {
                // ทำการวาปผู้เล่น (ตัวมันเอง) ไปยังจุดหมายปลายทางที่กำหนดไว้ในสคริปต์ของ Cube
                transform.position = teleportPoint.destinationPoint.position;
                Debug.Log("Teleported to " + teleportPoint.destinationPoint.name);

                // เริ่ม Cooldown
                canTeleport = false;
                StartCoroutine(TeleportCooldown());
            }
        }
    }

    // Coroutine สำหรับการนับเวลาถอยหลัง
    IEnumerator TeleportCooldown()
    {
        yield return new WaitForSeconds(cooldownTime);
        canTeleport = true;
    }
}