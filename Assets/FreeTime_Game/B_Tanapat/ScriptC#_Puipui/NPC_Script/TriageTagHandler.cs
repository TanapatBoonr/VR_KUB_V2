using UnityEngine;

// ***************************************************************
// TriageColor ถูกดึงมาจาก TriageEnums.cs
// ***************************************************************

public class TriageTagHandler : MonoBehaviour
{
    // กำหนดสีของบัตร Triage นี้ใน Inspector
    public TriageColor tagColor;

    // เมื่อป้าย Tag ชน (Collide) กับ NPC ที่มี Collider แบบ Is Trigger
    void OnTriggerEnter(Collider other)
    {
        // *** 1. ตรวจสอบว่ามีการชนเกิดขึ้นหรือไม่ ***
        Debug.Log("--- Tag Collision Detected with: " + other.gameObject.name + " ---"); 

        // 2. พยายามดึงสคริปต์ PatientController จาก GameObject ที่ชน
        
        // ลองดึงจาก Collider ที่ชนโดยตรง
        GreenPatientController patient = other.GetComponent<GreenPatientController>();

        // ถ้าไม่เจอ ให้ลองดึงจาก Parent GameObject (เผื่อสคริปต์อยู่บน Root ของ NPC)
        if (patient == null)
        {
             patient = other.GetComponentInParent<GreenPatientController>();
        }
        
        if (patient != null) 
        {
            // *** 3. DEBUG: พบสคริปต์ NPC ***
            Debug.Log("Successfully found GreenPatientController on " + patient.gameObject.name + "!");

            // 4. สั่งให้ NPC รับ Tag สีนี้
            // ส่งค่า Enum TriageColor ในรูปแบบ string ไป
            patient.ReceiveTriageTag(tagColor.ToString()); 

            // 5. ติดบัตรนี้เข้ากับ NPC
            
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            // ค้นหา Transform ชื่อ "Body" ใน Hierarchy ของ NPC
            Transform patientBody = patient.transform.Find("Body"); 
            if (patientBody != null)
            {
                transform.SetParent(patientBody);
            }
            else
            {
                // ถ้าไม่พบ Body ให้ติดกับ Root Transform ของ NPC
                transform.SetParent(patient.transform);
            }
            
            // ปิดการทำงานของ Collider เพื่อไม่ให้เกิด Trigger ซ้ำซ้อน
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
        else
        {
            // *** 6. DEBUG: พบ Collider แต่ไม่พบสคริปต์ ***
            Debug.LogWarning("TriageTagHandler failed to find GreenPatientController on " + other.gameObject.name + ". Check script placement.");
        }
    }
}