using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
public class TriagePatientBlack_WithStretcher : MonoBehaviour
{
    // ======================================================================
    //  TRIAGE TAG SOCKET (ผู้บาดเจ็บสีดำ: รับบัตร Black แล้วหามทันที)
    // ======================================================================
    [Header("Triage Tag Socket (Black only)")]
    [Tooltip("GameObject ของจุด Socket (จะเปิดตั้งแต่เริ่ม)")]
    public GameObject triageTagSocketObject;

    [Tooltip("XRSocketInteractor ของจุดรับบัตร")]
    public XRSocketInteractor triageTagSocket;

    [Header("บัตรที่ถูกต้อง (ตั้งให้เป็น Black)")]
    [Tooltip("ลาก Prefab บัตรสีดำ (Black_Tag-Triage) มาใส่เพื่อตรวจแบบชื่อ prefab")]
    public GameObject validBlackTagPrefab;

    [Tooltip("หรือกำหนด Tag ของบัตร เช่น \"Black\" (กรณีอยากตรวจด้วย Tag แทน prefab)")]
    public string validBlackTagName = "Black";

    // ======================================================================
    //  TAG MOUNT: จุดติดบัตรให้ไปกับตัว/เปล
    // ======================================================================
    [Header("Triage Tag Mount (ตำแหน่งติดบัตร)")]
    [Tooltip("จุดที่ให้บัตรไปเกาะ (เช่น หน้าอก/ข้อมือ หรือ Anchor บนเปล)")]
    public Transform tagMountPoint;
    public Vector3 tagLocalOffset = Vector3.zero;
    public Vector3 tagLocalEuler  = Vector3.zero;
    public Vector3 tagLocalScale  = Vector3.one;

    // ======================================================================
    //  STRETCHER (สปอว์นเปล แล้วยก/ย้ายผู้บาดเจ็บไปยังจุดหมาย)
    // ======================================================================
    [Header("Stretcher Settings")]
    [Tooltip("Prefab ของเปลที่จะสปอว์น")]
    public GameObject stretcherPrefab;

    [Tooltip("Transform ของผู้บาดเจ็บ (root ที่ต้องย้าย/จับวางบนเปล)")]
    public Transform patientRoot;

    [Tooltip("ปลายทางที่ต้องพาคนเจ็บไป")]
    public Transform destinationPoint;

    [Header("การจัดวางเปล/ผู้บาดเจ็บ")]
    [Tooltip("ยก/กดเปลจากตำแหน่งผู้บาดเจ็บตอนสปอว์น (โลก)")]
    public float stretcherVerticalOffset = -0.05f;

    [Tooltip("หมุนเพิ่มของเปลหลังจากตาม rotation ของผู้บาดเจ็บ (องศา)")]
    public Vector3 stretcherRotationOffset = Vector3.zero;

    [Tooltip("ใช้ Anchor บนเปลเพื่อวางผู้บาดเจ็บ (ใส่ชื่อ GameObject ลูกบนเปล)")]
    public bool useStretcherAnchor = true;
    public string stretcherAnchorName = "PatientAnchor";

    [Tooltip("ออฟเซ็ตของผู้บาดเจ็บบน Anchor/เปล (local)")]
    public Vector3 patientLocalOffset = new Vector3(0f, 0.05f, 0f);

    [Tooltip("หมุนของผู้บาดเจ็บบนเปล (local)")]
    public Vector3 patientLocalEuler = Vector3.zero;

    [Tooltip("ให้ผู้บาดเจ็บเป็นลูกของเปลหรือไม่ (จะเคลื่อนตาม)")]
    public bool parentPatientToStretcher = true;

    [Header("การเคลื่อนย้ายเปล")]
    public float moveSpeed = 1.5f;
    public float arriveThreshold = 0.1f;

    // ======================================================================
    //  EVENTS (เผื่ออยากเชื่อมระบบอื่นเพิ่ม เช่นนับคะแนน/แจ้งเตือน)
    // ======================================================================
    [Header("Extra Events")]
    public UnityEvent onBlackTagAccepted;   // เมื่อรับบัตรดำถูกต้อง
    public UnityEvent onStretcherSpawned;   // เมื่อสปอว์นเปล
    public UnityEvent onDelivered;          // เมื่อส่งถึงปลายทาง

    // ======================================================================
    //  INTERNAL STATE
    // ======================================================================
    bool _triageAccepted = false;
    bool _stretcherSpawned = false;
    GameObject _stretcher;

    void Awake()
    {
        // เปิด Socket ตั้งแต่เริ่ม (ผู้บาดเจ็บสีดำไม่ต้องผ่านขั้นตอนประเมิน)
        if (triageTagSocketObject) triageTagSocketObject.SetActive(true);

        if (triageTagSocket)
            triageTagSocket.selectEntered.AddListener(OnTriageTagPlaced);
    }

    void OnDestroy()
    {
        if (triageTagSocket)
            triageTagSocket.selectEntered.RemoveListener(OnTriageTagPlaced);
    }

    void Update()
    {
        // กันอีเวนต์ตกหล่น: ถ้ามีของค้างอยู่ในซ็อกเก็ตและยังไม่ Accept ให้ตรวจซ้ำ
        if (!_triageAccepted && triageTagSocket && triageTagSocket.hasSelection)
        {
            OnTriageTagPlaced(new SelectEnterEventArgs());
        }
    }

    // ======================================================================
    //  TRIAGE TAG
    // ======================================================================
    void OnTriageTagPlaced(SelectEnterEventArgs _)
    {
        if (_triageAccepted || triageTagSocket == null || !triageTagSocket.hasSelection) return;

        var sel = triageTagSocket.interactablesSelected.FirstOrDefault();
        var tr  = (sel as Component)?.transform;
        if (tr == null) return;

        if (!IsCorrectBlackTag(tr))
        {
            EjectWrong(triageTagSocket);
            return;
        }

        // ปลดออกจากซ็อกเก็ตก่อน แล้วแนบบัตรให้ติดกับตัว/เปล
        if (triageTagSocket.interactionManager != null && sel != null)
            triageTagSocket.interactionManager.SelectExit(triageTagSocket, sel);

        AttachTagToMount(tr);

        _triageAccepted = true;
        onBlackTagAccepted?.Invoke();

        // ล็อกซ็อกเก็ตไม่ให้ถอดบัตร
        triageTagSocket.enabled = false;
        if (triageTagSocketObject) triageTagSocketObject.SetActive(false);

        // ต่อด้วยการหามขึ้นเปลและเคลื่อนไปยังปลายทาง
        if (!_stretcherSpawned) StartCoroutine(Co_SpawnStretcherAndMove());
    }

    bool IsCorrectBlackTag(Transform tagTr)
    {
        if (tagTr == null) return false;

        // 1) ตรวจด้วย Tag ชื่อ "Black" (หรือที่กำหนด)
        if (!string.IsNullOrEmpty(validBlackTagName))
        {
            try { if (tagTr.CompareTag(validBlackTagName)) return true; }
            catch { if (tagTr.tag == validBlackTagName) return true; }
        }

        // 2) ตรวจด้วยชื่อ Prefab
        if (validBlackTagPrefab != null && StripClone(tagTr.name) == validBlackTagPrefab.name)
            return true;

        return false;
    }

    void EjectWrong(XRSocketInteractor socket)
    {
        if (socket && socket.hasSelection && socket.interactionManager != null)
        {
            var sel = socket.interactablesSelected.FirstOrDefault();
            if (sel != null) socket.interactionManager.SelectExit(socket, sel);
        }
    }

    // ======================================================================
    //  ATTACH TAG TO MOUNT (ให้บัตรติดไปกับคนเจ็บ/เปล)
    // ======================================================================
    void AttachTagToMount(Transform tagTr)
    {
        if (tagTr == null) return;

        // ปิดการโต้ตอบ/ฟิสิกส์ของบัตร เพื่อให้ติดนิ่ง
        var grab = tagTr.GetComponent<XRGrabInteractable>();
        var rb   = tagTr.GetComponent<Rigidbody>();
        if (grab) grab.enabled = false;
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // เลือกพาเรนต์สำหรับติดบัตร
        Transform parent = tagMountPoint != null ? tagMountPoint : (patientRoot != null ? patientRoot : transform);
        tagTr.SetParent(parent, worldPositionStays: false);

        tagTr.localPosition = tagLocalOffset;
        tagTr.localRotation = Quaternion.Euler(tagLocalEuler);
        tagTr.localScale    = tagLocalScale;
    }

    // ======================================================================
    //  STRETCHER FLOW
    // ======================================================================
    IEnumerator Co_SpawnStretcherAndMove()
    {
        _stretcherSpawned = true;

        if (stretcherPrefab == null || patientRoot == null)
        {
            Debug.LogError($"{name}: Missing stretcherPrefab or patientRoot.");
            yield break;
        }

        // 1) สร้างเปลใต้ตัวผู้บาดเจ็บ
        Quaternion baseRot = patientRoot.rotation * Quaternion.Euler(stretcherRotationOffset);
        Vector3 spawnPos   = patientRoot.position + new Vector3(0f, stretcherVerticalOffset, 0f);
        _stretcher = Instantiate(stretcherPrefab, spawnPos, baseRot);
        onStretcherSpawned?.Invoke();

        yield return new WaitForSeconds(0.05f); // รอให้ประกอบตัว

        // 2) หา Anchor บนเปล
        Transform anchor = _stretcher.transform;
        if (useStretcherAnchor)
        {
            var found = _stretcher.transform.Find(stretcherAnchorName);
            if (found) anchor = found;
        }

        // 3) วางผู้บาดเจ็บบนเปล + ออฟเซ็ต
        Vector3 worldTarget =
            anchor.position +
            anchor.right   * patientLocalOffset.x +
            anchor.up      * patientLocalOffset.y +
            anchor.forward * patientLocalOffset.z;

        patientRoot.position = worldTarget;
        patientRoot.rotation = anchor.rotation * Quaternion.Euler(patientLocalEuler);

        if (parentPatientToStretcher) patientRoot.SetParent(_stretcher.transform, true);

        // 4) เคลื่อนเปลไปยังจุดหมาย
        if (destinationPoint != null)
        {
            while (Vector3.Distance(_stretcher.transform.position, destinationPoint.position) > arriveThreshold)
            {
                Vector3 dir = (destinationPoint.position - _stretcher.transform.position).normalized;
                _stretcher.transform.position += dir * moveSpeed * Time.deltaTime;
                yield return null;
            }
        }

        onDelivered?.Invoke();
    }

    // ======================================================================
    //  HELPERS
    // ======================================================================
    static string StripClone(string s)
    {
        const string c = "(Clone)";
        return (!string.IsNullOrEmpty(s) && s.EndsWith(c)) ? s.Substring(0, s.Length - c.Length) : s;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (destinationPoint)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(destinationPoint.position, 0.15f);
        }

        if (tagMountPoint)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(tagMountPoint.position, Vector3.one * 0.05f);
        }
    }
#endif
}
