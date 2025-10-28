using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq; 
using System.Collections; // ยังคงต้องใช้สำหรับการเรียก StartCoroutine

// กำหนดให้สคริปต์นี้ทำงานร่วมกับ XRGrabInteractable
[RequireComponent(typeof(XRGrabInteractable))]
public class InfiniteItemSpawner : MonoBehaviour
{
    private XRGrabInteractable originalGrabInteractable;
    private XRInteractionManager interactionManager;

    [Tooltip("ลาก Prefab ของไอเทมนี้มาใส่ในช่องนี้")]
    public GameObject itemPrefab; 
    
    private readonly Vector3 SPAWN_OFFSET = new Vector3(0, 0.05f, 0); 
    
    // *** NEW: ตัวแปรสำหรับเก็บ Physics Components ของ CLONE (ใช้ใน Coroutine) ***
    private Collider[] cloneColliders;
    private Rigidbody cloneRb;


    void Awake()
    {
        originalGrabInteractable = GetComponent<XRGrabInteractable>();
        interactionManager = FindObjectOfType<XRInteractionManager>();
        
        if (interactionManager == null)
        {
            Debug.LogError("XRInteractionManager ไม่พบใน Scene! การหยิบไอเทมซ้ำจะไม่ทำงาน.");
            enabled = false;
        }
    }

    void OnEnable()
    {
        if (originalGrabInteractable != null)
        {
            // 1. เชื่อมต่อ Event: เมื่อ Interactor เริ่มหยิบไอเทมต้นฉบับ
            originalGrabInteractable.selectEntered.AddListener(OnSelectEntered);
            
            // 2. *** NEW: เชื่อมต่อ Event: เมื่อ Interactor ปล่อยไอเทมต้นฉบับ ***
            // (เป็นการบังคับปล่อยในโค้ด) Event นี้จะถูกเรียกทันทีหลัง SelectExit
            originalGrabInteractable.selectExited.AddListener(OnSelectExited); 
        }
    }

    void OnDisable()
    {
        if (originalGrabInteractable != null)
        {
            originalGrabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            originalGrabInteractable.selectExited.RemoveListener(OnSelectExited);
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (interactionManager == null || itemPrefab == null) return;
        
        // 1. สร้างสำเนา (Clone)
        Vector3 spawnPosition = transform.position + SPAWN_OFFSET;
        GameObject clone = Instantiate(itemPrefab, spawnPosition, transform.rotation);
        clone.name = itemPrefab.name + " (Clone)";
        
        Destroy(clone.GetComponent<InfiniteItemSpawner>());

        
        // 2. จัดการ Physics ของ CLONE ก่อนถูกหยิบ
        cloneColliders = clone.GetComponentsInChildren<Collider>();
        cloneRb = clone.GetComponent<Rigidbody>();
        
        // ปิด Collision ชั่วคราว (ใช้ Coroutine ในการเปิดกลับมา)
        foreach (Collider col in cloneColliders)
        {
            col.enabled = false;
        }
        
        if (cloneRb != null)
        {
            cloneRb.isKinematic = true;
            cloneRb.useGravity = false; 
        }

        // 3. บังคับให้ Interactor หยิบสำเนาแทนต้นฉบับ
        IXRSelectInteractor interactor = args.interactorObject;
        XRGrabInteractable cloneGrabInteractable = clone.GetComponent<XRGrabInteractable>();
        
        if (cloneGrabInteractable != null)
        {
            // 3.1. บังคับ Interactor ให้หยุดการหยิบต้นฉบับ -> ทำให้เกิด Event selectExited (ของตัวต้นฉบับ)
            interactionManager.SelectExit(interactor, originalGrabInteractable);
            
            // 3.2. บังคับ Interactor ให้เริ่มหยิบสำเนาทันที
            interactionManager.SelectEnter(interactor, cloneGrabInteractable);
            
            Debug.Log(gameObject.name + " (ต้นฉบับ) ถูก Clone และหยิบโดย Player");
        }
        else
        {
            Debug.LogError("สำเนาไม่มี XRGrabInteractable! Prefab ผิดพลาด");
            Destroy(clone);
        }
    }
    
    // *** NEW: ฟังก์ชันที่ถูกเรียกทันทีหลังการบังคับปล่อยไอเทมต้นฉบับ (SelectExit) ***
    private void OnSelectExited(SelectExitEventArgs args)
    {
        // ใช้ Coroutine เพื่อรอ 0.1 วินาที ให้มือ Player ยึดวัตถุโคลนได้สมบูรณ์
        if (cloneColliders != null && cloneRb != null)
        {
            StartCoroutine(ReactivatePhysicsAfterDelay(cloneColliders, cloneRb, 0.1f));
        }
    }

    // Coroutine: เปิด Physics กลับมาหลังผ่านไป X วินาที
    private IEnumerator ReactivatePhysicsAfterDelay(Collider[] colliders, Rigidbody rb, float delay)
    {
        // 1. รอเวลาสั้นๆ 0.1 วินาที เพื่อให้มือ Player จับวัตถุโคลนได้มั่นคง
        yield return new WaitForSeconds(delay);
        
        // 2. *** NEW: เชื่อมต่อ Event selectExited ของตัวโคลน ***
        // ถ้า Player ปล่อยมือจากตัวโคลน ให้คืนค่า Physics ปกติ
        
        XRGrabInteractable cloneGrabInteractable = rb.GetComponent<XRGrabInteractable>();

        if (cloneGrabInteractable != null)
        {
            cloneGrabInteractable.selectExited.AddListener((selectArgs) =>
            {
                // คืนค่า Physics (จะถูกเรียกเมื่อ Player ปล่อยมือจากวัตถุโคลน)
                RestoreClonePhysics(colliders, rb);
            });
        }
    }
    
    // *** NEW: ฟังก์ชันสำหรับคืนค่า Physics ของ Clone ***
    private void RestoreClonePhysics(Collider[] colliders, Rigidbody rb)
    {
        // เปิด Collider กลับมา
        foreach (Collider col in colliders)
        {
            if (col != null)
            {
                col.enabled = true;
            }
        }
        
        // คืนค่า Rigidbody (จะทำให้มันร่วงลงมาเมื่อไม่ได้ถูกถือแล้ว)
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
        
        Debug.Log("Clone Physics Re-activated.");
        // ล้างตัวแปรที่ไม่ใช้แล้ว
        cloneColliders = null;
        cloneRb = null;
    }
}