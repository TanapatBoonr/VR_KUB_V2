using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
public class TriagePatientRY_WithStretcher : MonoBehaviour
{
    // ======================================================================
    //  CONFIG: สีตั้งต้น + เส้นตายเปลี่ยนสถานะ
    // ======================================================================
    public enum StartClass { Red, Yellow }
    public StartClass startClass = StartClass.Red;

    [Tooltip("แดงจะเสื่อมเป็น ดำ หลังเวลานี้ (นาที)")]
    public float redDeadlineMinutes = 2f;

    [Tooltip("เหลืองจะเสื่อมเป็น แดง หลังเวลานี้ (นาที)")]
    public float yellowDeadlineMinutes = 3f;

    public enum CurrentClass { Red, Yellow, Black }
    [SerializeField] private CurrentClass currentClass;

    // ======================================================================
    //  ASSESSMENT (เข้าใกล้ → ปุ่ม Assess → แสดง 'I Can't Walk')
    // ======================================================================
    [Header("Assessment / Proximity")]
    public Transform playerCamera;
    public float showAssessRadius = 2.2f;

    public Button uiAssessButton;           // ปุ่มกด "Assess" (World-Space UI)
    public GameObject uiCantWalkGraphic;    // ข้อความ/กราฟิก "I Can't Walk"
    public float cantWalkShowSeconds = 1.5f;

    // ======================================================================
    //  ห้ามเลือด (เลือดพุ่ง): Tourniquet → Top Gauze → เลือดหยุด
    // ======================================================================
    [Header("Arterial Bleed (Optional)")]
    public bool hasArterialBleed = false;
    public GameObject bleedParticle;

    public XRSocketInteractor tourniquetSocket;      // ขั้นที่ 1
    public XRSocketInteractor gauzeSocket;           // ขั้นที่ 2

    [Tooltip("ตรวจด้วย Tag ของไอเท็ม (วิธีที่ 1)")]
    public string tourniquetItemTag = "Tourniquet";
    public string topGauzeItemTag = "TopGauze";

    [Tooltip("หรือเจาะจง Prefab (วิธีที่ 2)")]
    public GameObject tourniquetPrefab;
    public GameObject topGauzePrefab;

    // ======================================================================
    //  TRIAGE TAG SOCKET (จะแสดงเมื่อผ่านเงื่อนไขครบ)
    // ======================================================================
    [Header("Triage Tag Socket")]
    public GameObject triageTagSocketObject;
    public XRSocketInteractor triageTagSocket;

    [Header("บัตรที่ถูกต้อง (Prefab/Tag)")]
    [Tooltip("บัตรที่ถูกต้อง 'ก่อน' เดดไลน์ (ลาก Prefab ของบัตร)")]
    public GameObject validTagPrefab_BeforeDeadline;    // แดง→Red / เหลือง→Yellow

    [Tooltip("บัตรที่ถูกต้อง 'หลัง' เดดไลน์ (ลาก Prefab ของบัตร)")]
    public GameObject validTagPrefab_AfterDeadline;     // แดง→Black / เหลือง→Red

    [Tooltip("สำรอง: ตรวจด้วยชื่อ Tag ของ GameObject บัตร")]
    public string validTagName_BeforeDeadline;          // "Red" หรือ "Yellow"
    public string validTagName_AfterDeadline;           // "Black" หรือ "Red"

    // ======================================================================
    //  STRETCHER (สปอว์นเปล แล้วยก/ย้ายผู้บาดเจ็บไปยังจุดหมาย)
    // ======================================================================
    [Header("Stretcher Settings")]
    public GameObject stretcherPrefab;
    public Transform patientRoot;
    public Transform destinationPoint;

    [Header("การจัดวางเปล/ผู้บาดเจ็บ")]
    public float stretcherVerticalOffset = -0.05f;
    public Vector3 stretcherRotationOffset = Vector3.zero;
    public bool useStretcherAnchor = true;
    public string stretcherAnchorName = "PatientAnchor";
    public Vector3 patientLocalOffset = new Vector3(0f, 0.05f, 0f);
    public Vector3 patientLocalEuler = Vector3.zero;
    public bool parentPatientToStretcher = true;

    [Header("การเคลื่อนย้ายเปล")]
    public float moveSpeed = 1.5f;
    public float arriveThreshold = 0.1f;

    // ======================================================================
    //  EVENTS (เผื่ออยากเชื่อมระบบอื่นเพิ่ม)
    // ======================================================================
    [Header("Extra Events")]
    public UnityEvent onAssessStarted;
    public UnityEvent onBleedFullyTreated;
    public UnityEvent onTriageAccepted;
    public UnityEvent onStretcherSpawned;
    public UnityEvent onDelivered;

    // ======================================================================
    //  INTERNAL STATE
    // ======================================================================
    float _spawnTime;
    Transform _cam;

    bool _assessed = false;
    bool _bleedStep1Done = false;
    bool _bleedStep2Done = false;
    bool _triageAccepted = false;
    bool _stretcherSpawned = false;

    GameObject _stretcher;

    void Awake()
    {
        _spawnTime = Time.time;
        _cam = playerCamera ? playerCamera : (Camera.main ? Camera.main.transform : null);

        // สีตั้งต้น
        currentClass = (startClass == StartClass.Red) ? CurrentClass.Red : CurrentClass.Yellow;

        // ปิด UI/Socket ที่ยังไม่ถึงคิว
        if (uiAssessButton) uiAssessButton.gameObject.SetActive(false);
        if (uiCantWalkGraphic) uiCantWalkGraphic.SetActive(false);

        // *** ปิดซ็อกเก็ตของเลือดพุ่งทั้งหมดไว้ก่อนเสมอ (ต้องกด Assess ก่อนเท่านั้นถึงเปิดได้) ***
        SafeSetActive(tourniquetSocket, false);
        SafeSetActive(gauzeSocket, false);

        // เลือดพุ่งเริ่มต้นโชว์/ไม่โชว์ได้ตามต้องการ (ส่วนใหญ่จะโชว์ไว้ก่อน)
        if (bleedParticle) bleedParticle.SetActive(hasArterialBleed);

        // ปิดซ็อกเก็ตบัตรไว้ก่อน
        ShowTriageSocket(false);

        // ผูกอีเวนต์
        if (tourniquetSocket) tourniquetSocket.selectEntered.AddListener(OnTourniquetPlaced);
        if (gauzeSocket)      gauzeSocket.selectEntered.AddListener(OnGauzePlaced);
        if (triageTagSocket)  triageTagSocket.selectEntered.AddListener(OnTriageTagPlaced);
    }

    void OnDestroy()
    {
        if (tourniquetSocket) tourniquetSocket.selectEntered.RemoveListener(OnTourniquetPlaced);
        if (gauzeSocket)      gauzeSocket.selectEntered.RemoveListener(OnGauzePlaced);
        if (triageTagSocket)  triageTagSocket.selectEntered.RemoveListener(OnTriageTagPlaced);
    }

    void Start()
    {
        if (uiAssessButton) uiAssessButton.onClick.AddListener(() => StartCoroutine(Co_AssessFlow()));
    }

    void Update()
    {
        UpdateClassByTime();
        HandleAssessProximity();

        // กันอีเวนต์ตกหล่น: จะตรวจซ้ำเฉพาะ "หลังจากกด Assess แล้วเท่านั้น"
        if (_assessed && hasArterialBleed)
        {
            if (!_bleedStep1Done && tourniquetSocket && tourniquetSocket.hasSelection)
                OnTourniquetPlaced(new SelectEnterEventArgs());

            if (_bleedStep1Done && !_bleedStep2Done && gauzeSocket && gauzeSocket.hasSelection)
                OnGauzePlaced(new SelectEnterEventArgs());
        }

        if (_assessed && !_triageAccepted)
        {
            if ((!hasArterialBleed || (_bleedStep1Done && _bleedStep2Done)) &&
                triageTagSocket && triageTagSocket.hasSelection)
            {
                OnTriageTagPlaced(new SelectEnterEventArgs());
            }
        }
    }

    // ======================================================================
    //  ASSESSMENT FLOW
    // ======================================================================
    void HandleAssessProximity()
    {
        if (_assessed) { if (uiAssessButton) uiAssessButton.gameObject.SetActive(false); return; }
        if (_cam == null) return;

        bool near = Vector3.Distance(transform.position, _cam.position) <= showAssessRadius;
        if (uiAssessButton) uiAssessButton.gameObject.SetActive(near);
    }

    IEnumerator Co_AssessFlow()
    {
        _assessed = true;
        onAssessStarted?.Invoke();
        if (uiAssessButton) uiAssessButton.gameObject.SetActive(false);

        if (uiCantWalkGraphic)
        {
            uiCantWalkGraphic.SetActive(true);
            yield return new WaitForSeconds(Mathf.Max(0.1f, cantWalkShowSeconds));
            uiCantWalkGraphic.SetActive(false);
        }

        if (hasArterialBleed)
        {
            // *** เปิดเฉพาะตอนหลัง Assess เท่านั้น ***
            SafeSetActive(tourniquetSocket, true);    // เปิดขั้นที่ 1
            SafeSetActive(gauzeSocket, false);        // ขั้นที่ 2 รอไว้ก่อน
        }
        else
        {
            ShowTriageSocket(true); // ไม่มีเลือดพุ่ง → เปิดบัตรได้เลยหลัง Assess
        }
    }

    // ======================================================================
    //  BLEEDING STEPS
    // ======================================================================
    void OnTourniquetPlaced(SelectEnterEventArgs _)
    {
        // *** กันการวางก่อน Assess: ถ้ายังไม่ Assess ให้คายของออกทันที ***
        if (!_assessed) { EjectWrong(tourniquetSocket); return; }

        if (_bleedStep1Done) return;
        if (!IsObjectMatch(tourniquetSocket, tourniquetItemTag, tourniquetPrefab))
        {
            EjectWrong(tourniquetSocket);
            return;
        }

        _bleedStep1Done = true;
        SafeSetActive(gauzeSocket, true); // เปิดขั้นที่ 2 หลังผ่านขั้นที่ 1
    }

    void OnGauzePlaced(SelectEnterEventArgs _)
    {
        if (!_assessed) { EjectWrong(gauzeSocket); return; }

        if (!_bleedStep1Done || _bleedStep2Done) return;
        if (!IsObjectMatch(gauzeSocket, topGauzeItemTag, topGauzePrefab))
        {
            EjectWrong(gauzeSocket);
            return;
        }

        _bleedStep2Done = true;
        if (bleedParticle) bleedParticle.SetActive(false);
        onBleedFullyTreated?.Invoke();

        ShowTriageSocket(true); // ผ่านครบ → เปิดบัตร
    }

    bool IsObjectMatch(XRSocketInteractor socket, string wantTag, GameObject wantPrefab)
    {
        if (socket == null || !socket.hasSelection) return false;
        var sel = socket.interactablesSelected.FirstOrDefault();
        var tr  = (sel as Component)?.transform;
        if (tr == null) return false;

        // เทียบ Tag
        if (!string.IsNullOrEmpty(wantTag))
        {
            try { if (tr.CompareTag(wantTag)) return true; }
            catch { if (tr.tag == wantTag) return true; }
        }

        // เทียบชื่อ prefab (ตัด (Clone))
        if (wantPrefab != null && StripClone(tr.name) == wantPrefab.name)
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
    //  TRIAGE TAG
    // ======================================================================
    void ShowTriageSocket(bool on)
    {
        if (triageTagSocketObject) triageTagSocketObject.SetActive(on);
        if (triageTagSocket) triageTagSocket.enabled = on;
    }

    void OnTriageTagPlaced(SelectEnterEventArgs _)
    {
        // *** ต้อง Assess ก่อน และ (ถ้ามีเลือดพุ่ง) ต้องผ่านทั้ง 2 ขั้นก่อน ***
        if (!_assessed) { EjectWrong(triageTagSocket); return; }
        if (hasArterialBleed && !(_bleedStep1Done && _bleedStep2Done)) { EjectWrong(triageTagSocket); return; }
        if (triageTagSocket == null || !triageTagSocket.hasSelection) return;

        var sel = triageTagSocket.interactablesSelected.FirstOrDefault();
        var tr  = (sel as Component)?.transform;
        if (!IsCorrectTriageItem(tr))
        {
            EjectWrong(triageTagSocket);
            return;
        }

        _triageAccepted = true;
        onTriageAccepted?.Invoke();

        // ล็อกไว้ไม่ให้ถอนบัตร
        if (triageTagSocket) triageTagSocket.enabled = false;

        if (!_stretcherSpawned) StartCoroutine(Co_SpawnStretcherAndMove());
    }

    bool IsCorrectTriageItem(Transform tagTr)
    {
        if (tagTr == null) return false;
        bool afterDeadline = IsAfterDeadline();

        string wantTag  = afterDeadline ? validTagName_AfterDeadline  : validTagName_BeforeDeadline;
        GameObject want = afterDeadline ? validTagPrefab_AfterDeadline : validTagPrefab_BeforeDeadline;

        // Tag
        if (!string.IsNullOrEmpty(wantTag))
        {
            try { if (tagTr.CompareTag(wantTag)) return true; }
            catch { if (tagTr.tag == wantTag) return true; }
        }
        // Prefab name
        if (want != null && StripClone(tagTr.name) == want.name) return true;

        return false;
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
    //  TIME & STATUS
    // ======================================================================
    void UpdateClassByTime()
    {
        float elapsed = Time.time - _spawnTime;

        if (startClass == StartClass.Red)
        {
            float t = redDeadlineMinutes * 60f;
            currentClass = (elapsed >= t) ? CurrentClass.Black : CurrentClass.Red;
        }
        else
        {
            float t = yellowDeadlineMinutes * 60f;
            currentClass = (elapsed >= t) ? CurrentClass.Red : CurrentClass.Yellow;
        }
    }

    bool IsAfterDeadline()
    {
        float elapsed = Time.time - _spawnTime;
        return (startClass == StartClass.Red)
            ? elapsed >= redDeadlineMinutes * 60f
            : elapsed >= yellowDeadlineMinutes * 60f;
    }

    // ======================================================================
    //  HELPERS
    // ======================================================================
    static string StripClone(string s)
    {
        const string c = "(Clone)";
        return (!string.IsNullOrEmpty(s) && s.EndsWith(c)) ? s.Substring(0, s.Length - c.Length) : s;
    }

    void SafeSetActive(XRSocketInteractor socket, bool on)
    {
        if (socket == null) return;
        socket.enabled = on;
        if (socket.gameObject) socket.gameObject.SetActive(on);

        // กันค้าง: ถ้าปิดแล้วมีของอยู่ ให้คายออก
        if (!on && socket.hasSelection && socket.interactionManager != null)
        {
            var sel = socket.interactablesSelected.FirstOrDefault();
            if (sel != null) socket.interactionManager.SelectExit(socket, sel);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (destinationPoint)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(destinationPoint.position, 0.15f);
        }
    }
#endif
}

