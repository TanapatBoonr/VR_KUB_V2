using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
public class TriagePatientRY_TypeB_WithStretcher : MonoBehaviour
{
    // ======================================================================
    //  CONFIG: สีตั้งต้น + เส้นตายเปลี่ยนสถานะ
    // ======================================================================
    public enum StartClass { Red, Yellow }
    [Header("เริ่มต้นเป็นสีอะไร (Red/Yellow)")]
    public StartClass startClass = StartClass.Red;

    [Tooltip("แดงจะเสื่อมเป็น ดำ หลังเวลานี้ (นาที)")]
    public float redDeadlineMinutes = 2f;

    [Tooltip("เหลืองจะเสื่อมเป็น แดง หลังเวลานี้ (นาที)")]
    public float yellowDeadlineMinutes = 3f;

    public enum CurrentClass { Red, Yellow, Black }
    [SerializeField] private CurrentClass currentClass;

    // ======================================================================
    //  แบบ B: สถานะพูดได้ + ตัวเลือกว่ามีแผลถูกแทงหรือไม่
    // ======================================================================
    [Header("แบบ B: ติ๊กถ้าเป็น พูดได้และโดนแทง")]
    [Tooltip("ติ๊กถ้าเป็น 'พูดได้และมีแผลถูกแทง' (ไม่ติ๊ก = พูดได้แต่ไม่มีแผลถูกแทง)")]
    public bool hasStabWound = false;

    // ======================================================================
    //  ASSESSMENT (เข้าใกล้ → ปุ่ม Assess → แสดงข้อความผล)
    // ======================================================================
    [Header("Assessment / Proximity")]
    public Transform playerCamera;
    public float showAssessRadius = 2.2f;

    [Tooltip("ปุ่ม UI World-Space ที่ตัวผู้บาดเจ็บ (กดเพื่อเริ่มประเมิน)")]
    public Button uiAssessButton;

    [Tooltip("กราฟิก/ข้อความที่จะโชว์ช่วงสั้น ๆ หลัง Assess (เช่น 'พูดได้และมีแผลโดนแทง' หรือ 'พูดได้และไม่มีแผลโดนแทง')")]
    public GameObject uiResultGraphic;

    [Tooltip("เวลาที่โชว์กราฟิกผลลัพธ์ (วินาที)")]
    public float resultShowSeconds = 1.0f;

    // ======================================================================
    //  TRIAGE TAG SOCKET (จะแสดงหลัง Assess สำเร็จแล้วเท่านั้น)
    // ======================================================================
    [Header("Triage Tag Socket (โชว์หลัง Assess)")]
    public GameObject triageTagSocketObject;
    public XRSocketInteractor triageTagSocket;

    [Header("บัตรที่ถูกต้องสำหรับเคสนี้ (ลาก Prefab)")]
    [Tooltip("บัตรที่ถูกต้อง 'ก่อนเดดไลน์' สำหรับเคสนี้ (เช่น มีแผลแทง → Red, ไม่มีแผลแทง → Yellow)")]
    public GameObject correctTagPrefab_BeforeDeadline;

    [Tooltip("บัตรที่ถูกต้อง 'หลังเดดไลน์' ตามกติกา (Red→Black / Yellow→Red)")]
    public GameObject correctTagPrefab_AfterDeadline;

    [Header("สำรอง: ตรวจด้วยชื่อ Tag ของ GameObject บัตร (ไม่จำเป็นต้องใช้ถ้าตรวจด้วย Prefab)")]
    public string correctTagName_BeforeDeadline; // เช่น "Red" หรือ "Yellow"
    public string correctTagName_AfterDeadline;  // เช่น "Black" หรือ "Red"

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
    //  EVENTS (เผื่ออยากเชื่อมระบบอื่นเพิ่ม)
    // ======================================================================
    [Header("Extra Events (เลือกใช้)")]
    public UnityEvent onAssessStarted;
    public UnityEvent onTriageAccepted;
    public UnityEvent onStretcherSpawned;
    public UnityEvent onDelivered;

    // ======================================================================
    //  INTERNAL STATE
    // ======================================================================
    float _spawnTime;
    Transform _cam;

    bool _assessed = false;
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
        if (uiResultGraphic) uiResultGraphic.SetActive(false);

        ShowTriageSocket(false); // ห้ามวางบัตรก่อน Assess
        if (triageTagSocket) triageTagSocket.selectEntered.AddListener(OnTriageTagPlaced);
    }

    void OnDestroy()
    {
        if (triageTagSocket) triageTagSocket.selectEntered.RemoveListener(OnTriageTagPlaced);
    }

    void Start()
    {
        if (uiAssessButton) uiAssessButton.onClick.AddListener(() => StartCoroutine(Co_AssessFlow()));
    }

    void Update()
    {
        UpdateClassByTime();
        HandleAssessProximity();

        // Fallback: ถ้า (แสดง socket แล้ว) และมีบัตรวางค้าง ให้ตรวจยืนยันอีกครั้ง
        if (_assessed && !_triageAccepted && triageTagSocket && triageTagSocket.hasSelection)
        {
            OnTriageTagPlaced(new SelectEnterEventArgs());
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

        // โชว์ข้อความผลลัพธ์ตามประเภท (พูดได้ + มี/ไม่มีแผลแทง)
        if (uiResultGraphic)
        {
            uiResultGraphic.SetActive(true);
            yield return new WaitForSeconds(Mathf.Max(0.1f, resultShowSeconds));
            uiResultGraphic.SetActive(false);
        }

        // เปิดให้วางบัตรหลัง Assess เท่านั้น
        ShowTriageSocket(true);
    }

    // ======================================================================
    //  TRIAGE TAG
    // ======================================================================
    void ShowTriageSocket(bool on)
    {
        if (triageTagSocketObject) triageTagSocketObject.SetActive(on);
        if (triageTagSocket) triageTagSocket.enabled = on;

        // ถ้าปิดแล้วมีของอยู่ ให้คายออกกันค้าง
        if (!on && triageTagSocket && triageTagSocket.hasSelection && triageTagSocket.interactionManager != null)
        {
            var sel = triageTagSocket.interactablesSelected.FirstOrDefault();
            if (sel != null) triageTagSocket.interactionManager.SelectExit(triageTagSocket, sel);
        }
    }

    void OnTriageTagPlaced(SelectEnterEventArgs _)
    {
        // ต้อง Assess ก่อน
        if (!_assessed) { EjectWrong(triageTagSocket); return; }
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

        // ล็อกไม่ให้ถอนบัตร
        if (triageTagSocket) triageTagSocket.enabled = false;

        // สปอว์นเปล + ยก + ย้ายไปปลายทาง
        if (!_stretcherSpawned) StartCoroutine(Co_SpawnStretcherAndMove());
    }

    bool IsCorrectTriageItem(Transform tagTr)
    {
        if (tagTr == null) return false;

        bool afterDeadline = IsAfterDeadline();

        // ชุดตรวจที่ "กำหนดเฉพาะเคสนี้" ผ่าน Inspector
        string wantTagName  = afterDeadline ? correctTagName_AfterDeadline  : correctTagName_BeforeDeadline;
        GameObject wantPref = afterDeadline ? correctTagPrefab_AfterDeadline : correctTagPrefab_BeforeDeadline;

        // 1) ตรวจด้วย Tag
        if (!string.IsNullOrEmpty(wantTagName))
        {
            try { if (tagTr.CompareTag(wantTagName)) return true; }
            catch { if (tagTr.tag == wantTagName) return true; }
        }

        // 2) ตรวจด้วยชื่อ Prefab (ตัด (Clone))
        if (wantPref != null && StripClone(tagTr.name) == wantPref.name)
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
    //  STRETcher FLOW (เหมือน StretcherSpawner.cs แบบย่อ)
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
