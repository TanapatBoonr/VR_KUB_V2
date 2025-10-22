using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq; // ต้องเพิ่ม

public class WoundController : MonoBehaviour
{
    public event System.Action OnWoundTreated; 

    [Header("Wound Configuration")]
    [Tooltip("ชื่อ Tag ของ Item ที่ใช้รักษาบาดแผลนี้ เช่น Gauze, Tourniquet")]
    public string requiredTreatmentTag; 
    
    [Tooltip("ระยะห่างสูงสุดที่ Item จะถือว่าใช้ได้ผล")]
    public float treatmentRange = 0.1f; 

    [HideInInspector]
    public bool isTreated = false; 

    // *** ลบ: private Rigidbody heldItemRigidbody = null; (ไม่จำเป็นแล้ว) ***

    [Header("Player Interactor References")]
    [Tooltip("ลาก Interactor หลักของมือ Player (L/R) มาใส่เพื่อตรวจจับการถือ Item")]
    public XRBaseInteractor leftHandInteractor;
    public XRBaseInteractor rightHandInteractor;


    void Update()
    {
        if (isTreated) return;
        CheckForTreatment();
    }
    
    private void CheckForTreatment()
    {
        // 1. ตรวจสอบมือทั้งสองข้างว่ากำลังถือ Item ที่ถูกต้องหรือไม่
        CheckHand(leftHandInteractor);
        CheckHand(rightHandInteractor);
    }
    
    private void CheckHand(XRBaseInteractor interactor)
    {
        if (interactor == null) return;
        
        // ใช้ GetOldestInteractableSelected() หรือ interactablesSelected
        // เนื่องจากเวอร์ชันของคุณอาจเป็นเวอร์ชันเก่า เราจะใช้ Logic ที่ปลอดภัย
        
        // *** แก้ไข: ใช้ interactablesSelected เพื่อหา Interactable Object ที่ถูกถือ ***
        IXRSelectInteractable heldInteractable = interactor.interactablesSelected.FirstOrDefault();

        if (heldInteractable != null)
        {
            GameObject heldItem = heldInteractable.transform.gameObject;

            // 2. ตรวจสอบ Tag
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

    private void TreatWound(GameObject itemUsed, IXRSelectInteractor interactor)
    {
        isTreated = true;
        
        Debug.Log("Wound on " + gameObject.name + " treated with " + itemUsed.name);

        // *** KEY FIX: แก้ปัญหา CS1061 โดยการ Cast ไปเป็น XRBaseInteractor ***
        if (interactor is XRBaseInteractor baseInteractor) // ตรวจสอบและ Cast
        {
            // 1. รับ IXRSelectInteractable Component จาก Item ที่ถูกใช้
            if (itemUsed.TryGetComponent<IXRSelectInteractable>(out var interactableUsed))
            {
                // 2. ใช้ interactionManager ที่อยู่ใน Base Interactor สั่ง SelectExit
                baseInteractor.interactionManager.SelectExit(baseInteractor, interactableUsed);
            }
        }
        else
        {
            Debug.LogWarning("Cannot auto-release item. Interactor is not XRBaseInteractor.");
        }

        // Optional: ทำลาย Item ที่ใช้แล้ว
        // Destroy(itemUsed); 
        
        OnWoundTreated?.Invoke();
        
        enabled = false;
    }
}