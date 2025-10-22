using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem; 
using System.Collections; 
using System.Linq; // ต้องเพิ่ม namespace นี้สำหรับการใช้งาน Linq

public class MegaphoneController : MonoBehaviour
{
    public static event System.Action<bool> OnMegaphoneStateChanged;

    private AudioSource audioSource;
    private XRGrabInteractable grabInteractable;

    public AudioClip commandClip; 

    public InputActionProperty activateAction; // ลาก Input Action (Trigger/Activate) มาใส่

    private bool isBeingUsed = false; 

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    void OnEnable()
    {
        if (activateAction.action != null)
        {
            activateAction.action.performed += OnActivatePerformed;
            activateAction.action.canceled += OnActivateCanceled; 
            activateAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (activateAction.action != null)
        {
            activateAction.action.performed -= OnActivatePerformed;
            activateAction.action.canceled -= OnActivateCanceled;
            activateAction.action.Disable();
        }
    }

    // *** ฟังก์ชันใหม่: ตรวจสอบว่าถูกถือด้วย "มือ" จริงๆ ไม่ใช่ "กระเป๋า" ***
    private bool IsHeldByHand()
    {
        if (!grabInteractable.isSelected)
        {
            return false; // ไม่ได้ถูกเลือกเลย
        }

        // ตรวจสอบ Interactor ที่กำลังเลือก (ถือ) โทรโข่งอยู่
        IXRSelectInteractor interactor = grabInteractable.interactorsSelecting.FirstOrDefault();

        if (interactor == null)
        {
            return false;
        }

        // ถ้า Interactor นั้นเป็น XRDirectInteractor (คือมือที่จับโดยตรง) 
        // หรือ XRRayInteractor ที่ใช้จับวัตถุโดยตรง ให้ถือว่า "ถูกถืออยู่ในมือ"
        // แต่ถ้าเป็น XRSocketInteractor ให้ถือว่า "ถูกเก็บ"
        
        // เราใช้การตรวจสอบว่า Interactor ที่กำลังเลือก "ไม่ใช่" Socket
        // สมมติว่า Interactor ที่เป็นมือ ไม่ได้มี Component XRSocketInteractor
        // และ Interactor ที่เป็นกระเป๋ามี Component XRSocketInteractor
        
        // วิธีที่ง่ายและใช้ได้ผลกับ XR Interaction Toolkit คือ:
        // ตรวจสอบว่า Interactor นั้น "ไม่ใช่" Socket Interactor
        return interactor.transform.GetComponent<XRSocketInteractor>() == null;
    }

    // ---------------------------------------------------------------------
    // 4. โทรโข่งเริ่มเล่นเสียง (เมื่อกดปุ่ม Trigger/Activate ขณะถือด้วยมือ)
    // ---------------------------------------------------------------------
    private void OnActivatePerformed(InputAction.CallbackContext context)
    {
        // *** เงื่อนไขการทำงานที่แก้ไข: ต้องถูกถือด้วยมือจริงๆ และยังไม่เริ่มใช้งาน ***
        if (IsHeldByHand() && !isBeingUsed)
        {
            StartMegaphoneSequence();
        }
    }

    // ---------------------------------------------------------------------
    // เมื่อเลิกกดปุ่ม Trigger/Activate
    // ---------------------------------------------------------------------
    private void OnActivateCanceled(InputAction.CallbackContext context)
    {
        // ถ้ากำลังใช้งานอยู่ (เล่นเสียงอยู่) ให้หยุด
        if (isBeingUsed)
        {
            StopMegaphoneSequence();
        }
    }
    
    // ... StartMegaphoneSequence() และ StopMegaphoneSequence() เหมือนเดิม ...
    public void StartMegaphoneSequence()
    {
        isBeingUsed = true;
        
        if (audioSource != null && commandClip != null)
        {
            audioSource.clip = commandClip;
            audioSource.loop = true; 
            audioSource.Play();
        }

        if (OnMegaphoneStateChanged != null)
        {
            OnMegaphoneStateChanged.Invoke(true);
        }
    }

    public void StopMegaphoneSequence()
    {
        isBeingUsed = false;
        
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }

        if (OnMegaphoneStateChanged != null)
        {
            OnMegaphoneStateChanged.Invoke(false);
        }
    }
}