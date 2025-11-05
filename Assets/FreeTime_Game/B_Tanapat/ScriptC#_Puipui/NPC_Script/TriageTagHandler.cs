using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq; 

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
        
        // 2. พยายามดึงสคริปต์ EmergencyPatientController (สำหรับ Red/Yellow)
        EmergencyPatientController emergencyPatient = other.GetComponent<EmergencyPatientController>();
        if (emergencyPatient == null)
        {
             emergencyPatient = other.GetComponentInParent<EmergencyPatientController>();
        }

        // 3. ถ้าพบ Controller ที่เกี่ยวข้อง
        if (greenPatient != null) 
        {
            // *** Logic สำหรับผู้ป่วยสีเขียว ***
            // Green Patient จะรับ Tag ทันที
            greenPatient.ReceiveTriageTag(tagColor.ToString()); 
            AttachTagToPatient(greenPatient.transform);
        }
        else if (emergencyPatient != null)
        {
            // *** Logic สำหรับผู้ป่วย Red/Yellow ***
            // EmergencyPatientController จะรับ Tag เพื่อจัดการ Logic ภายใน/คะแนน
            // การติด Tag จริงๆ ควรเกิดขึ้นผ่าน Socket (OnTagAttached ใน EmergencyPatientController)
            emergencyPatient.ReceiveTriageTag(tagColor.ToString());
            AttachTagToPatient(emergencyPatient.transform); // ติด Tag เข้ากับตัว NPC
        }
        else
        {
            Debug.LogWarning("TriageTagHandler: Failed to find Green/Emergency Patient Controller on " + other.gameObject.name + ".");
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
        if (TryGetComponent<XRGrabInteractable>(out XRGrabInteractable grab)) 
        {
            grab.enabled = false;
        }
    }
}