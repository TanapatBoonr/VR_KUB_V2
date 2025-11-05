using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq; 

public class WoundController : MonoBehaviour
{
    // Event ที่จะถูกเรียกเมื่อบาดแผลนี้ถูกรักษา
    public event System.Action OnWoundTreated; 

    [Header("Wound Configuration")]
    [Tooltip("ชื่อ Tag ของ Item ที่ใช้รักษาบาดแผลนี้ (เช่น Gauze, Tourniquet)")]
    // ใช้สำหรับการตรวจจับแบบถือมือใกล้ (CheckForTreatment)
    public string requiredTreatmentTag; 
    
    [Tooltip("ระยะห่างสูงสุดที่ Item จะถือว่าใช้ได้ผล (ใช้เฉพาะกรณีไม่มี Fixed Socket)")]
    public float treatmentRange = 0.1f; 

    [Header("Fixed Treatment Socket (ข้อ 4.1)")]
    [Tooltip("ถ้ามี Socket ที่ต้องใช้ไอเทมเฉพาะเจาะจง ให้ลาก XRSocketInteractor มาใส่")]
    public XRSocketInteractor fixedTreatmentSocket; 
    
    [Tooltip("ชื่อ Tag ของ Item ที่ Socket นี้รับเท่านั้น (เช่น 'Tourniquet' ถ้าไม่ได้ใช้ Socket ให้เว้นว่าง)")]
    public string requiredItemForSocketTag = ""; 
    
    [Header("Visuals")]
    [Tooltip("Particle System ที่แสดงเลือดไหล/พุ่ง (ถ้ามี)")]
    public ParticleSystem bloodParticle; 

    [HideInInspector]
    public bool isTreated = false; 

    [Header("Player Interactor References (สำหรับวิธีเก่า)")]
    [Tooltip("ลาก Interactor หลักของมือ Player (L/R) มาใส่เพื่อตรวจจับการถือ Item (ถ้าใช้ Fixed Socket ไม่ต้องใช้)")]
    public XRBaseInteractor leftHandInteractor;
    public XRBaseInteractor rightHandInteractor;
    
    // อ้างอิงถึง Patient Controller เพื่อแจ้งการรักษาสำเร็จ (ใช้ร่วมกับ EmergencyPatientController)
    [Header("Patient Reference")]
    public EmergencyPatientController patientController; 


    void Start()
    {
        // 1. ตั้งค่า Particle System ตั้งแต่เริ่มต้น
        if (bloodParticle != null)
        {
            // ถ้ามี Particle System ให้เล่นไว้ก่อน
            if (!bloodParticle.isPlaying)
            {
                bloodParticle.Play();
            }
        }
        
        // 2. ถ้าใช้ Fixed Socket ให้ตั้งค่า Listener
        if (fixedTreatmentSocket != null)
        {
            fixedTreatmentSocket.selectEntered.AddListener(OnFixedSocketTreatment);
            
            // 3. ตั้งค่าการกรอง Item เฉพาะเจาะจง (ถ้ากำหนด)
            if (!string.IsNullOrEmpty(requiredItemForSocketTag))
            {
                // ตรวจสอบ Interactable Filter Component (ถ้ามี)
                // Note: ใน XR Toolkit โดยทั่วไปจะใช้ Interactable.selectFilter/InteractionGroup/Custom Validator
                // แต่สำหรับการทำงานใน Inspector สามารถทำได้ง่ายกว่าโดยการกำหนด Tag ที่ Socket เอง
            }
        }
    }

    void Update()
    {
        if (isTreated) return;
        
        // ** ตรวจสอบการรักษาแบบดั้งเดิม (ถือมือใกล้) เฉพาะเมื่อไม่มี Fixed Socket **
        if (fixedTreatmentSocket == null)
        {
            CheckForTreatment_Proximity();
        }
    }
    
    // --------------------------------------------------------------------
    // 1. วิธีการรักษาแบบ Proximity (ถือ Item มาใกล้) - สำหรับ Socket แบบที่ 1 (4.2)
    // --------------------------------------------------------------------
    private void CheckForTreatment_Proximity()
    {
        CheckHand(leftHandInteractor);
        CheckHand(rightHandInteractor);
    }
    
    private void CheckHand(XRBaseInteractor interactor)
    {
        if (interactor == null) return;
        
        // ตรวจสอบว่ากำลังถือ Item
        if (interactor.selectTarget != null) 
        {
            GameObject heldItem = interactor.selectTarget.transform.gameObject;
            
            // 2. ตรวจสอบ Tag ของ Item ที่ใช้ (ใช้ requiredTreatmentTag)
            if (heldItem.CompareTag(requiredTreatmentTag))
            {
                // 3. ตรวจสอบระยะห่าง
                float distance = Vector3.Distance(transform.position, heldItem.transform.position);
                
                if (distance <= treatmentRange)
                {
                    // 4. การรักษาสำเร็จ!
                    TreatWound(heldItem, interactor);
                }
            }
        }
    }
    
    // --------------------------------------------------------------------
    // 2. วิธีการรักษาแบบ Fixed Socket (ใส่ Item ลงใน Socket) - สำหรับ Socket แบบที่ 2 (4.1)
    // --------------------------------------------------------------------
    private void OnFixedSocketTreatment(SelectEnterEventArgs args)
    {
        if (isTreated) 
        {
            // ถ้าถูกรักษาไปแล้ว ให้ปล่อย Item ออกจาก Socket ทันที
            fixedTreatmentSocket.interactionManager.SelectExit(fixedTreatmentSocket, args.interactableObject);
            return;
        }

        GameObject insertedItem = args.interactableObject.transform.gameObject;
        bool isCorrectItem = true;
        
        // ตรวจสอบ Item เฉพาะเจาะจง
        if (!string.IsNullOrEmpty(requiredItemForSocketTag) && !insertedItem.CompareTag(requiredItemForSocketTag))
        {
            // ถ้ามี Tag กำหนด แต่ Tag ไม่ตรง
            isCorrectItem = false;
            Debug.LogWarning(gameObject.name + $": Fixed Socket required Tag: {requiredItemForSocketTag}. Item used: {insertedItem.tag}.");
            
            // ปล่อย Item ที่ผิดออกมา
            fixedTreatmentSocket.interactionManager.SelectExit(fixedTreatmentSocket, args.interactableObject);
        }
        
        if (isCorrectItem)
        {
            // ใช้ IXRSelectInteractor จาก Socket เอง
            TreatWound(insertedItem, fixedTreatmentSocket); 
        }
    }

    // --------------------------------------------------------------------
    // 3. ฟังก์ชันหลักในการรักษา
    // --------------------------------------------------------------------
    private void TreatWound(GameObject itemUsed, IXRSelectInteractor interactor)
    {
        isTreated = true;
        
        Debug.Log("Wound on " + gameObject.name + " treated with " + itemUsed.name);

        // 1. หยุด Particle Effect (ถ้ามี)
        if (bloodParticle != null)
        {
            bloodParticle.Stop();
        }

        // 2. แจ้ง Event ว่ารักษาบาดแผลเสร็จแล้ว
        OnWoundTreated?.Invoke();
        
        // 3. แจ้ง Patient Controller
        if (patientController != null)
        {
            patientController.OnWoundTreated();
        }

        // ** ถ้าเป็นการรักษาแบบ Proximity ให้สั่งปล่อย Item **
        if (fixedTreatmentSocket == null)
        {
            // สั่งให้ Interactor ปล่อย Item ที่ใช้แล้ว
            if (interactor is XRBaseInteractor baseInteractor) 
            {
                if (itemUsed.TryGetComponent<IXRSelectInteractable>(out var interactableUsed))
                {
                    baseInteractor.interactionManager.SelectExit(baseInteractor, interactableUsed);
                }
            }
        }
        // ** ถ้าเป็นการรักษาแบบ Fixed Socket ไม่ต้องสั่งปล่อย เพราะ Socket ถืออยู่ **
    }
}