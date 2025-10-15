using UnityEngine;
using TMPro;

public class ItemUIController : MonoBehaviour
{
    // ตัวแปรสำหรับอ้างอิงถึง Text บน UI
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;

    // ตัวแปรส่วนตัวสำหรับเก็บการอ้างอิงถึงกล้องหลักของผู้เล่น
    private Transform mainCameraTransform;

    // ฟังก์ชัน Awake จะถูกเรียกก่อน Start()
    void Awake()
    {
        // ค้นหากล้องหลักของโปรเจกต์และเก็บการอ้างอิงไว้
        mainCameraTransform = Camera.main.transform;
    }

    // ฟังก์ชัน Update จะถูกเรียกทุกเฟรม
    void Update()
    {
        // ตรวจสอบว่ามีการอ้างอิงถึงกล้องหรือไม่
        if (mainCameraTransform != null)
        {
            // คำนวณทิศทางจาก UI ไปหากล้อง
            Vector3 directionToCamera = mainCameraTransform.position - transform.position;

            // **การแก้ไขปัญหา 'Look rotation viewing vector is zero'**
            // **เพิ่มเงื่อนไขเพื่อป้องกันไม่ให้ Vector3.zero ถูกส่งเข้า LookRotation()**
            if (directionToCamera != Vector3.zero) 
            {
                // สร้างการหมุน (Rotation) เพื่อให้แกน Z ของ UI หันไปในทิศทางนั้น
                Quaternion lookRotation = Quaternion.LookRotation(directionToCamera);

                // ทำให้ UI หันกลับด้าน 180 องศาบนแกน Y เพื่อแก้ไขการกลับหัว
                transform.rotation = lookRotation * Quaternion.Euler(0, 180, 0);
            }
        }
    }

    // ฟังก์ชันสำหรับอัปเดตข้อความบน UI
    public void UpdateUI(string name, string description)
    {
        if (itemNameText != null)
        {
            itemNameText.text = name;
        }

        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = description;
        }
    }
}