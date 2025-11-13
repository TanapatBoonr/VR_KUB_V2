using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
public class TriagePatientRY_TypeB : MonoBehaviour
{
    // ======================================================================
    //  CONFIG: แบบ B = "พูดได้" + เลือกว่ามีแผลถูกแทงหรือไม่
    // ======================================================================
    public enum StartClass { Red, Yellow }

    [Header("เริ่มต้นเป็นสีอะไร (Red/Yellow) [ใช้เพื่อแสดงสถานะทั่วไป]")]
    public StartClass startClass = StartClass.Red;

    [Tooltip("ติ๊กถ้า 'พูดได้และมีแผลถูกแทง' (ไม่ติ๊ก = พูดได้แต่ไม่มีแผลถูกแทง)")]
    public bool hasStabWound = false;

    [Tooltip("เส้นตาย (นาที) - Red จะเสื่อมเป็น Black, Yellow จะเสื่อมเป็น Red")]
    public float redDeadlineMinutes = 2f;
    public float yellowDeadlineMinutes = 3f;

    public enum CurrentClass { Red, Yellow, Black }
    [SerializeField] private CurrentClass currentClass;

    // ======================================================================
    //  ASSESSMENT / PROXIMITY
    // ======================================================================
    [Header("Assessment / Proximity")]
    public Transform playerCamera;
    public float showAssessRadius = 2.2f;

    public Button uiAssessButton;
    public GameObject uiResultGraphic;
    public float resultShowSeconds = 1.0f;

    // ======================================================================
    //  TRIAGE TAG SOCKET (โชว์หลัง Assess)
    // ======================================================================
    [Header("Triage Tag Socket (โชว์หลัง Assess)")]
    public GameObject triageTagSocketObject;
    public XRSocketInteractor triageTagSocket;

    [Header("บัตรที่ถูกต้อง (ลาก Prefab)")]
    [Tooltip("ก่อนเส้นตาย: ถ้าโดนแทง = Red, ถ้าไม่โดนแทง = Yellow")]
    public GameObject correctTagPrefab_BeforeDeadline;
    [Tooltip("หลังเส้นตาย: Red→Black, Yellow→Red")]
    public GameObject correctTagPrefab_AfterDeadline;

    [Header("สำรอง: ตรวจด้วยชื่อ Tag ของ GameObject (ถ้าไม่ได้ตรวจด้วย Prefab)")]
    public string correctTagName_BeforeDeadline; // "Red"/"Yellow"
    public string correctTagName_AfterDeadline;  // "Black"/"Red"

    // ======================================================================
    //  TAG MOUNT (ให้บัตรติดไปกับตัว/เปล)
    // ======================================================================
    [Header("Triage Tag Mount")]
    public Transform tagMountPoint;                 // จุดติดบัตรบนตัวคนเจ็บ/เปล
    public Vector3  tagLocalOffset = Vector3.zero;
    public Vector3  tagLocalEuler  = Vector3.zero;
    public Vector3  tagLocalScale  = Vector3.one;

    // ======================================================================
    //  STRETCHER
    // ======================================================================
    [Header("Stretcher Settings")]
    public GameObject stretcherPrefab;
    public Transform  patientRoot;
    public Transform  destinationPoint;

    [Header("การจัดวางเปล/ผู้บาดเจ็บ")]
    public float   stretcherVerticalOffset = -0.05f;
    public Vector3 stretcherRotationOffset = Vector3.zero;
    public bool    useStretcherAnchor = true;
    public string  stretcherAnchorName = "PatientAnchor";
    public Vector3 patientLocalOffset = new Vector3(0f, 0.05f, 0f);
    public Vector3 patientLocalEuler  = Vector3.zero;
    public bool    parentPatientToStretcher = true;

    [Header("การเคลื่อนย้ายเปล")]
    public float moveSpeed = 1.5f;
    public float arriveThreshold = 0.1f;

    [Header("Events (เลือกใช้)")]
    public UnityEvent onAssessStarted;
    public UnityEvent onTriageAccepted;
    public UnityEvent onStretcherSpawned;
    public UnityEvent onDelivered;

    // ======================================================================
    //  INTERNAL
    // ======================================================================
    float _spawnTime;
    Transform _cam;
    bool _assessed = false;
    bool _triageAccepted = false;
    bool _stretcherSpawned = false;
    GameObject _stretcher;

    // ======================================================================
    //  ScoreV2.5 hooks
    // ======================================================================
    private ScoreV2_5 _score;
    private ScoreV2_5 Score() { if (_score == null) _score = FindObjectOfType<ScoreV2_5>(); return _score; }
    private void RegisterColor(ScoreV2_5.TriageColor color, bool correct = true) { Score()?.RegisterTagResult(color, correct); }
    private void RegisterFinished() { Score()?.RegisterPatientFinished(); }

    private ScoreV2_5.TriageColor GetExpectedColorNow()
    {
        // ก่อนเดดไลน์: โดนแทง = Red, ไม่โดนแทง = Yellow
        // หลังเดดไลน์: Red→Black, Yellow→Red
        bool after = IsAfterDeadline();
        if (!after)
            return hasStabWound ? ScoreV2_5.TriageColor.Red : ScoreV2_5.TriageColor.Yellow;
        else
            return hasStabWound ? ScoreV2_5.TriageColor.Black : ScoreV2_5.TriageColor.Red;
    }

    // ======================================================================
    //  LIFECYCLE
    // ======================================================================
    void Awake()
    {
        _spawnTime = Time.time;
        _cam = playerCamera ? playerCamera : (Camera.main ? Camera.main.transform : null);

        currentClass = (startClass == StartClass.Red) ? CurrentClass.Red : CurrentClass.Yellow;

        if (uiAssessButton)  uiAssessButton.gameObject.SetActive(false);
        if (uiResultGraphic) uiResultGraphic.SetActive(false);

        ShowTriageSocket(false);
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

        // กันอีเวนต์ตกหล่น
        if (_assessed && !_triageAccepted && triageTagSocket && triageTagSocket.hasSelection)
        {
            OnTriageTagPlaced(new SelectEnterEventArgs());
        }
    }

    // ======================================================================
    //  ASSESSMENT
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

        if (uiResultGraphic)
        {
            uiResultGraphic.SetActive(true);
            yield return new WaitForSeconds(Mathf.Max(0.1f, resultShowSeconds));
            uiResultGraphic.SetActive(false);
        }

        ShowTriageSocket(true);
    }

    // ======================================================================
    //  TRIAGE TAG
    // ======================================================================
    void ShowTriageSocket(bool on)
    {
        if (triageTagSocketObject) triageTagSocketObject.SetActive(on);
        if (triageTagSocket) triageTagSocket.enabled = on;

        if (!on && triageTagSocket && triageTagSocket.hasSelection && triageTagSocket.interactionManager != null)
        {
            var sel = triageTagSocket.interactablesSelected.FirstOrDefault();
            if (sel != null) triageTagSocket.interactionManager.SelectExit(triageTagSocket, sel);
        }
    }

    void OnTriageTagPlaced(SelectEnterEventArgs _)
    {
        if (!_assessed) { EjectWrong(triageTagSocket); return; }
        if (triageTagSocket == null || !triageTagSocket.hasSelection) return;

        var sel  = triageTagSocket.interactablesSelected.FirstOrDefault();
        var tagTr = (sel as Component)?.transform;

        if (!IsCorrectTriageItem(tagTr))
        {
            EjectWrong(triageTagSocket);
            return;
        }

        // ปลดจาก Socket อย่างเป็นทางการ แล้วติดบัตรเข้าตัว/เปล
        if (triageTagSocket.interactionManager != null && sel != null)
            triageTagSocket.interactionManager.SelectExit(triageTagSocket, sel);

        AttachTagToMount(tagTr);

        // ส่งคะแนนเข้า Score
        RegisterColor(GetExpectedColorNow(), true);

        _triageAccepted = true;
        onTriageAccepted?.Invoke();

        if (triageTagSocket) triageTagSocket.enabled = false;

        if (!_stretcherSpawned) StartCoroutine(Co_SpawnStretcherAndMove());
    }

    bool IsCorrectTriageItem(Transform tagTr)
    {
        if (tagTr == null) return false;

        bool after = IsAfterDeadline();
        GameObject wantPref = after ? correctTagPrefab_AfterDeadline : correctTagPrefab_BeforeDeadline;
        string     wantTag  = after ? correctTagName_AfterDeadline  : correctTagName_BeforeDeadline;

        if (!string.IsNullOrEmpty(wantTag))
        {
            try { if (tagTr.CompareTag(wantTag)) return true; }
            catch { if (tagTr.tag == wantTag) return true; }
        }
        if (wantPref != null && StripClone(tagTr.name) == wantPref.name) return true;

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

    void AttachTagToMount(Transform tagTr)
    {
        if (tagTr == null) return;

        var grab = tagTr.GetComponent<XRGrabInteractable>();
        if (grab) grab.enabled = false;

        var rb = tagTr.GetComponent<Rigidbody>();
        if (rb) { rb.isKinematic = true; rb.useGravity = false; rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

        Transform parent = tagMountPoint ? tagMountPoint : (patientRoot ? patientRoot : transform);
        tagTr.SetParent(parent, worldPositionStays: false);
        tagTr.localPosition = tagLocalOffset;
        tagTr.localRotation = Quaternion.Euler(tagLocalEuler);
        tagTr.localScale    = (tagLocalScale == Vector3.zero) ? Vector3.one : tagLocalScale;
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

        Quaternion baseRot = patientRoot.rotation * Quaternion.Euler(stretcherRotationOffset);
        Vector3 spawnPos   = patientRoot.position + new Vector3(0f, stretcherVerticalOffset, 0f);
        _stretcher = Instantiate(stretcherPrefab, spawnPos, baseRot);
        onStretcherSpawned?.Invoke();

        yield return new WaitForSeconds(0.05f);

        Transform anchor = _stretcher.transform;
        if (useStretcherAnchor)
        {
            var found = _stretcher.transform.Find(stretcherAnchorName);
            if (found) anchor = found;
        }

        Vector3 worldTarget =
            anchor.position +
            anchor.right   * patientLocalOffset.x +
            anchor.up      * patientLocalOffset.y +
            anchor.forward * patientLocalOffset.z;

        patientRoot.position = worldTarget;
        patientRoot.rotation = anchor.rotation * Quaternion.Euler(patientLocalEuler);

        if (parentPatientToStretcher) patientRoot.SetParent(_stretcher.transform, true);

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
        RegisterFinished(); // <- แจ้ง Score ว่าเคสนี้เสร็จสมบูรณ์
    }

    // ======================================================================
    //  TIME / STATUS
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
        if (tagMountPoint)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(tagMountPoint.position, Vector3.one * 0.05f);
        }
    }
#endif
}
