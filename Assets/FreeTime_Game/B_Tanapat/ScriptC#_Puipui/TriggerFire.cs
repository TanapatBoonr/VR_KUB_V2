using UnityEngine;
using System.Collections; // ต้องเพิ่มไลบรารีนี้เพื่อใช้งาน Coroutine

public class TriggerFire : MonoBehaviour
{
    // ตัวแปรสำหรับอ้างอิงถึง GameObject "Fire" ที่จะเปิด/ปิด
    // ลาก GameObject Fire จาก Hierarchy มาใส่ใน Inspector
    public GameObject fireObject;

    // ตัวแปรสำหรับเวลาคูลดาวน์ (หน่วยเป็นวินาที)
    public float cooldownTime = 10f; 

    // ตัวแปรสำหรับสถานะว่าตอนนี้อยู่ในช่วงคูลดาวน์หรือไม่
    private bool isOnCooldown = false;

    // ฟังก์ชันนี้จะถูกเรียกเมื่อมี Collider อื่นเข้ามาในพื้นที่ Trigger
    private void OnTriggerEnter(Collider other)
    {
        // ตรวจสอบว่า Collider ที่เข้ามามี Tag เป็น "Player" และไม่อยู่ในช่วงคูลดาวน์
        if (other.CompareTag("Player") && !isOnCooldown)
        {
            // สลับสถานะของ GameObject Fire
            if (fireObject != null)
            {
                // ตรวจสอบสถานะปัจจุบันของ Fire แล้วสลับ
                fireObject.SetActive(!fireObject.activeSelf);
            }
            
            // เริ่มการคูลดาวน์
            StartCoroutine(StartCooldown());
        }
    }

    // Coroutine สำหรับจัดการเวลาคูลดาวน์
    private IEnumerator StartCooldown()
    {
        // ตั้งสถานะให้เป็นคูลดาวน์
        isOnCooldown = true;

        // รอตามเวลาที่กำหนดไว้ (10 วินาที)
        yield return new WaitForSeconds(cooldownTime);

        // เมื่อครบเวลาแล้ว ให้ปิดสถานะคูลดาวน์
        isOnCooldown = false;
    }
}