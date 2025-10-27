using UnityEngine;

public class BillboardText : MonoBehaviour
{
    [Tooltip("ลาก Transform ของ Player's Camera (หรือ XR Rig) มาใส่")]
    public Transform targetCamera;

    void Start()
    {
        // 1. ตรวจสอบว่าได้กำหนด Player Camera แล้ว
        if (targetCamera == null)
        {
            // พยายามหา Main Camera ของ Player Rig โดยอัตโนมัติ
            if (Camera.main != null)
            {
                targetCamera = Camera.main.transform;
            }
            else
            {
                Debug.LogError("Target Camera (Player) ไม่ได้ถูกกำหนดใน Inspector และไม่พบ Main Camera!");
                enabled = false; // ปิดสคริปต์เพื่อป้องกัน Error ใน Update
                return;
            }
        }
    }

    void Update()
    {
        // คำนวณทิศทางจากป้ายชื่อไปยัง Player
        Vector3 directionToTarget = targetCamera.position - transform.position;
        
        // *** FIX: 1. สร้าง Quaternion.LookRotation ***
        // LookRotation() จะสร้างการหมุนที่ทำให้แกน Z-positive (ด้านหน้า) ชี้ไปที่เป้าหมาย
        Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);

        // *** FIX: 2. ปรับใช้ Y-Axis Lock และลบ Roll/Pitch ออก ***
        // เราต้องการให้ป้ายหมุนตาม Player ในแนวราบ (แกน Y) เท่านั้น
        // (โดยทั่วไป Canvas จะหันหน้าไปทาง แกน Z)
        
        // ดึงมุม Yaw (การหมุนรอบแกน Y) ออกมา
        float yAngle = lookRotation.eulerAngles.y;
        
        // สร้างการหมุนใหม่โดยใช้มุม Y เท่านั้น เพื่อให้ป้ายตั้งตรง (ไม่มี Pitch/Roll)
        Quaternion flatRotation = Quaternion.Euler(0, yAngle, 0);

        // *** FIX: 3. หันป้ายกลับด้าน 180 องศาเพื่อชดเชย LookRotation ***
        // โดยปกติ Canvas จะถูกสร้างโดยหันไปทาง Z-positive แต่ถ้า LookRotation ชี้ไปที่ Player
        // มันอาจจะทำให้ UI หัน "หลัง" เข้าหา Player
        // การหมุน 180 องศาบนแกน Y ทำให้ด้านหน้าของ UI (Z-positive) หันเข้าหา Player
        transform.rotation = flatRotation * Quaternion.Euler(0, 180, 0);
    }
}