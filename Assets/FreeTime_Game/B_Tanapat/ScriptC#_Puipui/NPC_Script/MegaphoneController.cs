using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;                               // ใช้ XRNode
using UnityEngine.XR.Interaction.Toolkit;

public class MegaphoneController : MonoBehaviour
{
    public static event System.Action<bool> OnMegaphoneStateChanged;

    [Header("Audio")]
    public AudioClip commandClip;

    [Header("Interactors (ใส่อันที่มีจริง)")]
    public XRBaseInteractor leftDirectInteractor;
    public XRBaseInteractor leftRayInteractor;
    public XRBaseInteractor rightDirectInteractor;
    public XRBaseInteractor rightRayInteractor;

    [Header("(ทางเลือก) Root ของมือ")]
    public Transform leftHandRoot;
    public Transform rightHandRoot;

    [Header("Input (per hand)")]
    public InputActionProperty leftActivateAction;   // XRI LeftHand Interaction / Activate
    public InputActionProperty rightActivateAction;  // XRI RightHand Interaction / Activate

    [Header("Debug")]
    public bool debugLogs = true;

    private AudioSource _audio;
    private XRGrabInteractable _grab;
    private XRBaseInteractor _holder;

    private enum Hand { None, Left, Right }

    void Awake()
    {
        _audio = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        _grab  = GetComponent<XRGrabInteractable>();
    }

    void OnEnable()
    {
        if (leftActivateAction.action != null)
        {
            leftActivateAction.action.performed += OnLeftPerformed;
            leftActivateAction.action.canceled  += OnLeftCanceled;
            leftActivateAction.action.Enable();
        }
        if (rightActivateAction.action != null)
        {
            rightActivateAction.action.performed += OnRightPerformed;
            rightActivateAction.action.canceled  += OnRightCanceled;
            rightActivateAction.action.Enable();
        }

        if (_grab != null)
        {
            _grab.selectEntered.AddListener(OnGrabbed);
            _grab.selectExited.AddListener(OnReleased);
        }
    }

    void OnDisable()
    {
        if (leftActivateAction.action != null)
        {
            leftActivateAction.action.performed -= OnLeftPerformed;
            leftActivateAction.action.canceled  -= OnLeftCanceled;
            leftActivateAction.action.Disable();
        }
        if (rightActivateAction.action != null)
        {
            rightActivateAction.action.performed -= OnRightPerformed;
            rightActivateAction.action.canceled  -= OnRightCanceled;
            rightActivateAction.action.Disable();
        }

        if (_grab != null)
        {
            _grab.selectEntered.RemoveListener(OnGrabbed);
            _grab.selectExited.RemoveListener(OnReleased);
        }
    }

    // ───────── Grab / Release ─────────
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        _holder = args.interactorObject as XRBaseInteractor;

        // ถ้าเป็น Socket → ไม่นับว่า "ถือด้วยมือ"
        if (_holder != null && _holder.GetComponent<XRSocketInteractor>() != null)
            _holder = null;

        if (debugLogs)
            Debug.Log($"[Mega] Grabbed by: {_holder?.name ?? "null"}");
    }

    private void OnReleased(SelectExitEventArgs _)
    {
        _holder = null;
        if (_audio.isPlaying) StopSound();
        if (debugLogs) Debug.Log("[Mega] Released.");
    }

    // ───────── Input: Left ─────────
    private void OnLeftPerformed(InputAction.CallbackContext _)
    {
        var handNow = ResolveHand(_holder);
        if (debugLogs) Debug.Log($"[Mega] Left trigger → holder={_holder?.name ?? "null"}, resolved={handNow}");

        if (handNow == Hand.Left && IsHeldByHand())
            StartSound();
        else if (debugLogs)
            Debug.Log("[Mega] Left trigger pressed but not held by LEFT hand.");
    }

    private void OnLeftCanceled(InputAction.CallbackContext _)
    {
        if (_audio.isPlaying && ResolveHand(_holder) == Hand.Left)
            StopSound();
    }

    // ───────── Input: Right ─────────
    private void OnRightPerformed(InputAction.CallbackContext _)
    {
        var handNow = ResolveHand(_holder);
        if (debugLogs) Debug.Log($"[Mega] Right trigger → holder={_holder?.name ?? "null"}, resolved={handNow}");

        if (handNow == Hand.Right && IsHeldByHand())
            StartSound();
        else if (debugLogs)
            Debug.Log("[Mega] Right trigger pressed but not held by RIGHT hand.");
    }

    private void OnRightCanceled(InputAction.CallbackContext _)
    {
        if (_audio.isPlaying && ResolveHand(_holder) == Hand.Right)
            StopSound();
    }

    // ───────── Helpers ─────────
    private bool IsHeldByHand()
    {
        return _holder != null && _holder.GetComponent<XRSocketInteractor>() == null;
    }

    /// <summary>
    /// ระบุว่าถือด้วยมือซ้าย/ขวา:
    /// 1) เทียบกับ Interactor ที่กรอกไว้ (Direct/Ray)
    /// 2) ถัดมา: หา XRController แล้วอ่าน controllerNode (LeftHand/RightHand) — รองรับ XRI 2.6.5
    /// 3) ถัดมา: เทียบกับ Root ของมือ (ถ้ากรอก)
    /// 4) สุดท้าย: เดาจากชื่อ
    /// </summary>
    private Hand ResolveHand(XRBaseInteractor inter)
    {
        if (inter == null) return Hand.None;

        // 1) เทียบตรงกับ reference ที่กรอกไว้
        if (inter == leftDirectInteractor  || inter == leftRayInteractor)  return Hand.Left;
        if (inter == rightDirectInteractor || inter == rightRayInteractor) return Hand.Right;

        // 2) XRController.controllerNode (ใช้งานได้ใน XRI 2.6.5)
        var xrCtrl = inter.GetComponentInParent<XRController>();
        if (xrCtrl != null)
        {
            if (xrCtrl.controllerNode == XRNode.LeftHand)  return Hand.Left;
            if (xrCtrl.controllerNode == XRNode.RightHand) return Hand.Right;
        }

        // 3) เช็กลูกหลานของ Root มือ
        if (leftHandRoot  != null && inter.transform.IsChildOf(leftHandRoot))  return Hand.Left;
        if (rightHandRoot != null && inter.transform.IsChildOf(rightHandRoot)) return Hand.Right;

        // 4) Fallback จากชื่อ
        string n = inter.name.ToLower();
        if (n.Contains("left"))  return Hand.Left;
        if (n.Contains("right")) return Hand.Right;

        return Hand.None;
    }

    // ───────── Audio ─────────
    private void StartSound()
    {
        if (commandClip == null) { Debug.LogWarning("[Mega] Missing CommandClip."); return; }
        _audio.clip = commandClip;
        _audio.loop = true;
        _audio.Play();
        OnMegaphoneStateChanged?.Invoke(true);
        if (debugLogs) Debug.Log("[Mega] START sound.");
    }

    private void StopSound()
    {
        _audio.loop = false;
        _audio.Stop();
        OnMegaphoneStateChanged?.Invoke(false);
        if (debugLogs) Debug.Log("[Mega] STOP sound.");
    }
}
