using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit; // <<< บรรทัดนี้แก้ไข Error CS0246

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
        // 1. พยายามดึงสคริปต์ GreenPatientController
        GreenPatientController greenPatient = other.GetComponent<GreenPatientController>();
        if (greenPatient == null)
        {
             greenPatient = other.GetComponentInParent<GreenPatientController>();
        }
        
        // 2. พยายามดึงสคริปต์ RedPatientController
        // (ต้องแน่ใจว่า RedPatientController มีฟังก์ชัน ReceiveTriageTag(string) แล้ว ตามที่แก้ไขไปก่อนหน้า)
        RedPatientController redPatient = other.GetComponent<RedPatientController>();
        if (redPatient == null)
        {
             redPatient = other.GetComponentInParent<RedPatientController>();
        }

        // 3. ถ้าพบ Controller ที่เกี่ยวข้อง
        if (greenPatient != null) 
        {
            // พบผู้ป่วยสีเขียว: เรียกฟังก์ชันรับ Tag
            greenPatient.ReceiveTriageTag(tagColor.ToString()); 
            AttachTagToPatient(greenPatient.transform);
        }
        else if (redPatient != null)
        {
            // พบผู้ป่วยสีแดง: เรียกฟังก์ชันรับ Tag
            redPatient.ReceiveTriageTag(tagColor.ToString());
            AttachTagToPatient(redPatient.transform);
        }
        else
        {
            Debug.LogWarning("TriageTagHandler: Failed to find Green/Red Patient Controller on " + other.gameObject.name + ".");
        }
    }
    
    // ฟังก์ชันสำหรับติด Tag เข้ากับผู้ป่วย
    private void AttachTagToPatient(Transform patientRoot)
    {
        // ทำให้ Tag กลายเป็น Kinematic และไม่สามารถถูกหยิบได้อีก
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // ค้นหา Transform ชื่อ "Body" ใน Hierarchy ของ NPC เพื่อติด Tag
        Transform patientBody = patientRoot.Find("Body"); 
        if (patientBody != null)
        {
            transform.SetParent(patientBody);
        }
        else
        {
            // ถ้าไม่พบ Body ให้ติดกับ Root Transform ของ NPC
            transform.SetParent(patientRoot);
        }
        
        // ปิดการทำงานของ Collider เพื่อไม่ให้เกิด Trigger ซ้ำซ้อน
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        
        // ปิดการทำงานของ Grab Interactable เพื่อให้ผู้เล่นไม่สามารถดึง Tag ออกมาได้อีก
        if (TryGetComponent<XRGrabInteractable>(out XRGrabInteractable grab)) // ตอนนี้รู้จัก XRGrabInteractable แล้ว
        {
            grab.enabled = false;
        }
    }
}
