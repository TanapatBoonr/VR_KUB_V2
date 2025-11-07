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
        // 1) เปิดเลือดไว้ก่อน
        if (bloodParticle != null && !bloodParticle.isPlaying)
            bloodParticle.Play();
        
        // 2) Fixed Socket: ผูก listener
        if (fixedTreatmentSocket != null)
            fixedTreatmentSocket.selectEntered.AddListener(OnFixedSocketTreatment);
    }

    void Update()
    {
        if (isTreated) return;
        
        // ตรวจแบบถือมาใกล้ เฉพาะเมื่อไม่ใช้ Fixed Socket
        if (fixedTreatmentSocket == null)
            CheckForTreatment_Proximity();
    }
    
    // ---------- วิธี Proximity ----------
    private void CheckForTreatment_Proximity()
    {
        CheckHand(leftHandInteractor);
        CheckHand(rightHandInteractor);
    }

    private void CheckHand(XRBaseInteractor interactor)
    {
        if (interactor == null || !interactor.hasSelection) return;

        // ใช้ตัวแรกที่ถูกเลือกอยู่ (แทน selectTarget)
        var held = interactor.interactablesSelected.FirstOrDefault();
        var heldGO = (held as Component)?.gameObject;
        if (heldGO == null) return;

        if (heldGO.CompareTag(requiredTreatmentTag))
        {
            float distance = Vector3.Distance(transform.position, heldGO.transform.position);
            if (distance <= treatmentRange)
                TreatWound(heldGO, interactor);
        }
    }
    
    // ---------- วิธี Fixed Socket ----------
    private void OnFixedSocketTreatment(SelectEnterEventArgs args)
    {
        if (isTreated)
        {
            // ถ้ารักษาแล้ว คายของออก
            fixedTreatmentSocket.interactionManager.SelectExit(fixedTreatmentSocket, args.interactableObject);
            return;
        }

        GameObject insertedItem = args.interactableObject.transform.gameObject;
        bool isCorrectItem = true;
        
        if (!string.IsNullOrEmpty(requiredItemForSocketTag) && !insertedItem.CompareTag(requiredItemForSocketTag))
        {
            isCorrectItem = false;
            Debug.LogWarning(gameObject.name + $": Fixed Socket required Tag: {requiredItemForSocketTag}. Item used: {insertedItem.tag}.");
            fixedTreatmentSocket.interactionManager.SelectExit(fixedTreatmentSocket, args.interactableObject);
        }
        
        if (isCorrectItem)
            TreatWound(insertedItem, fixedTreatmentSocket);
    }

    // ---------- หลัก ----------
    private void TreatWound(GameObject itemUsed, IXRSelectInteractor interactor)
    {
        isTreated = true;
        Debug.Log("Wound on " + gameObject.name + " treated with " + itemUsed.name);

        if (bloodParticle != null) bloodParticle.Stop();

        OnWoundTreated?.Invoke();

        if (patientController != null)
            patientController.OnWoundTreated();

        // ถ้าเป็น Proximity (ไม่มี socket) ให้สั่งปล่อยของออกจากมือ
        if (fixedTreatmentSocket == null && interactor is XRBaseInteractor baseInteractor)
        {
            if (itemUsed.TryGetComponent<IXRSelectInteractable>(out var interactableUsed))
                baseInteractor.interactionManager.SelectExit(baseInteractor, interactableUsed);
        }
    }
}
