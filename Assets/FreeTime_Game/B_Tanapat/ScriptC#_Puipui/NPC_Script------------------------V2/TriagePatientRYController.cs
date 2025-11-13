using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
public class TriagePatientRYController : MonoBehaviour
{
    // ======================= CONFIG: Start class & deadline =======================
    public enum StartClass { Red, Yellow }
    [Header("Start Color")]
    public StartClass startClass = StartClass.Red;

    [Tooltip("แดงจะเสื่อมเป็น ดำ หลังเวลานี้ (นาที)")]
    public float redDeadlineMinutes = 2f;

    [Tooltip("เหลืองจะเสื่อมเป็น แดง หลังเวลานี้ (นาที)")]
    public float yellowDeadlineMinutes = 3f;

    public enum CurrentClass { Red, Yellow, Black }
    [SerializeField] private CurrentClass currentClass;

    // ======================= ASSESSMENT =======================
    [Header("Assessment / Proximity")]
    public Transform playerCamera;
    public float showAssessRadius = 2.2f;
    public Button uiAssessButton;
    public GameObject uiCantWalkGraphic;
    public float cantWalkShowSeconds = 1.5f;

    // ======================= BLEEDING (optional) =======================
    [Header("Arterial Bleed (Optional)")]
    public bool hasArterialBleed = false;
    public GameObject bleedParticle;

    public XRSocketInteractor tourniquetSocket;  // step 1
    public XRSocketInteractor gauzeSocket;       // step 2

    [Tooltip("ตรวจด้วย Tag ของไอเท็ม (วิธีที่ 1)")]
    public string tourniquetItemTag = "Tourniquet";
    public string topGauzeItemTag   = "TopGauze";

    [Tooltip("หรือเจาะจง Prefab (วิธีที่ 2)")]
    public GameObject tourniquetPrefab;
    public GameObject topGauzePrefab;

    // ======================= TRIAGE TAG (socket จริงที่รับบัตร) =======================
    [Header("Triage Tag Socket (ตัวรับจริง)")]
    public GameObject         triageTagSocketObject;
    public XRSocketInteractor triageTagSocket;

    [Header("บัตรที่ถูกต้อง (Prefab/Tag)")]
    public GameObject validTagPrefab_BeforeDeadline; // แดง→Red / เหลือง→Yellow
    public GameObject validTagPrefab_AfterDeadline;  // แดง→Black / เหลือง→Red
    public string    validTagName_BeforeDeadline;    // สำรองตรวจด้วย Tag
    public string    validTagName_AfterDeadline;

    // ======================= DISPLAY GROUP (ชุดโชว์หลังรับบัตร) =======================
    [Header("Display Tag (ชุดโชว์หลังรับบัตร)")]
    [Tooltip("ลาก GameObject กลุ่มโชว์บัตรจาก Hierarchy มาใส่ (ให้ปิดไว้ล่วงหน้า)")]
    public GameObject tagDisplayGroup;

    // ======================= STRETCHER =======================
    [Header("Stretcher Settings")]
    public GameObject stretcherPrefab;
    public Transform  patientRoot;
    public Transform  destinationPoint;

    [Header("การจัดวางเปล/ผู้บาดเจ็บ")]
    public float    stretcherVerticalOffset = -0.05f;
    public Vector3  stretcherRotationOffset = Vector3.zero;
    public bool     useStretcherAnchor = true;
    public string   stretcherAnchorName = "PatientAnchor";
    public Vector3  patientLocalOffset = new Vector3(0f, 0.05f, 0f);
    public Vector3  patientLocalEuler  = Vector3.zero;
    public bool     parentPatientToStretcher = true;

    [Header("การเคลื่อนย้ายเปล")]
    public float moveSpeed = 1.5f;
    public float arriveThreshold = 0.1f;

    // ======================= EXTRA EVENTS =======================
    [Header("Extra Events")]
    public UnityEvent onAssessStarted;
    public UnityEvent onBleedFullyTreated;
    public UnityEvent onTriageAccepted;
    public UnityEvent onStretcherSpawned;
    public UnityEvent onDelivered;

    // ======================= INTERNAL STATE =======================
    float _spawnTime;
    Transform _cam;

    bool _assessed = false;
    bool _bleedStep1Done = false;
    bool _bleedStep2Done = false;
    bool _triageAccepted = false;
    bool _stretcherSpawned = false;

    GameObject _stretcher;

    // ======================= ScoreV2.5 hooks =======================
    private ScoreV2_5 _score;
    private ScoreV2_5 Score() { if (_score == null) _score = FindObjectOfType<ScoreV2_5>(); return _score; }
    private void RegisterColor(ScoreV2_5.TriageColor color, bool correct = true) { Score()?.RegisterTagResult(color, correct); }
    private void RegisterFinished() { Score()?.RegisterPatientFinished(); }

    private ScoreV2_5.TriageColor GetExpectedColorNow()
    {
        bool after = IsAfterDeadline();
        if (after)
            return (startClass == StartClass.Red) ? ScoreV2_5.TriageColor.Black : ScoreV2_5.TriageColor.Red;
        else
            return (startClass == StartClass.Red) ? ScoreV2_5.TriageColor.Red   : ScoreV2_5.TriageColor.Yellow;
    }

    // ======================= LIFECYCLE =======================
    void Awake()
    {
        _spawnTime = Time.time;
        _cam = playerCamera ? playerCamera : (Camera.main ? Camera.main.transform : null);

        currentClass = (startClass == StartClass.Red) ? CurrentClass.Red : CurrentClass.Yellow;

        if (uiAssessButton) uiAssessButton.gameObject.SetActive(false);
        if (uiCantWalkGraphic) uiCantWalkGraphic.SetActive(false);

        SafeSetActive(tourniquetSocket, false);
        SafeSetActive(gauzeSocket, false);

        if (bleedParticle) bleedParticle.SetActive(hasArterialBleed);

        // ปิดของฝั่งโชว์ไว้ก่อน
        if (tagDisplayGroup) tagDisplayGroup.SetActive(false);

        ShowTriageSocket(false);

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

        // กันตกหล่นหลัง Assess
        if (_assessed && hasArterialBleed)
        {
            if (!_bleedStep1Done && tourniquetSocket && tourniquetSocket.hasSelection)
                OnTourniquetPlaced(new SelectEnterEventArgs());

            if (_bleedStep1Done && !_bleedStep2Done && gauzeSocket && gauzeSocket.hasSelection)
                OnGauzePlaced(new SelectEnterEventArgs());
        }

        if (_assessed && !_triageAccepted &&
            (!hasArterialBleed || (_bleedStep1Done && _bleedStep2Done)) &&
            triageTagSocket && triageTagSocket.hasSelection)
        {
            OnTriageTagPlaced(new SelectEnterEventArgs());
        }
    }

    // ======================= ASSESSMENT FLOW =======================
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
            SafeSetActive(tourniquetSocket, true);
            SafeSetActive(gauzeSocket, false);
        }
        else
        {
            ShowTriageSocket(true);
        }
    }

    // ======================= BLEED STEPS =======================
    void OnTourniquetPlaced(SelectEnterEventArgs _)
    {
        if (!_assessed) { EjectWrong(tourniquetSocket); return; }

        if (_bleedStep1Done) return;
        if (!IsObjectMatch(tourniquetSocket, tourniquetItemTag, tourniquetPrefab))
        {
            EjectWrong(tourniquetSocket);
            return;
        }

        _bleedStep1Done = true;
        SafeSetActive(gauzeSocket, true);
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

        ShowTriageSocket(true);
    }

    bool IsObjectMatch(XRSocketInteractor socket, string wantTag, GameObject wantPrefab)
    {
        if (socket == null || !socket.hasSelection) return false;
        var sel = socket.interactablesSelected.FirstOrDefault();
        var tr  = (sel as Component)?.transform;
        if (tr == null) return false;

        if (!string.IsNullOrEmpty(wantTag))
        {
            try { if (tr.CompareTag(wantTag)) return true; }
            catch { if (tr.tag == wantTag) return true; }
        }

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

    // ======================= TRIAGE TAG ACCEPT (เปิดชุดโชว์) =======================
    void ShowTriageSocket(bool on)
    {
        if (triageTagSocketObject) triageTagSocketObject.SetActive(on);
        if (triageTagSocket) triageTagSocket.enabled = on;
    }

    void OnTriageTagPlaced(SelectEnterEventArgs _)
    {
        if (!_assessed) { EjectWrong(triageTagSocket); return; }
        if (hasArterialBleed && !(_bleedStep1Done && _bleedStep2Done)) { EjectWrong(triageTagSocket); return; }
        if (triageTagSocket == null || !triageTagSocket.hasSelection) return;

        var sel   = triageTagSocket.interactablesSelected.FirstOrDefault();
        var tagTr = (sel as Component)?.transform;

        if (!IsCorrectTriageItem(tagTr))
        {
            EjectWrong(triageTagSocket);
            return;
        }

        _triageAccepted = true;
        onTriageAccepted?.Invoke();

        // คายของเดิมออกจากซ็อกเก็ตจริงและปิดความสามารถโต้ตอบของมัน
        if (triageTagSocket.interactionManager != null && sel != null)
            triageTagSocket.interactionManager.SelectExit(triageTagSocket, sel);

        if (tagTr)
        {
            var grab = tagTr.GetComponent<XRGrabInteractable>();
            if (grab) grab.enabled = false;
            var rb = tagTr.GetComponent<Rigidbody>();
            if (rb) { rb.isKinematic = true; rb.useGravity = false; }
            tagTr.gameObject.SetActive(false); // ซ่อนบัตรที่ผู้เล่นนำมา
        }

        // เปิด “ชุดโชว์” ที่เตรียมไว้ (วางถูกตำแหน่งในซีนอยู่แล้ว)
        if (tagDisplayGroup) tagDisplayGroup.SetActive(true);

        // ปิดซ็อกเก็ตจริง ไม่ให้รับอะไรต่อ
        triageTagSocket.enabled = false;
        if (triageTagSocketObject) triageTagSocketObject.SetActive(false);

        // แจ้งผลสีให้ Score
        RegisterColor(GetExpectedColorNow(), true);

        if (!_stretcherSpawned) StartCoroutine(Co_SpawnStretcherAndMove());
    }

    bool IsCorrectTriageItem(Transform tagTr)
    {
        if (tagTr == null) return false;
        bool after = IsAfterDeadline();

        string    wantTag = after ? validTagName_AfterDeadline : validTagName_BeforeDeadline;
        GameObject want   = after ? validTagPrefab_AfterDeadline : validTagPrefab_BeforeDeadline;

        if (!string.IsNullOrEmpty(wantTag))
        {
            try { if (tagTr.CompareTag(wantTag)) return true; }
            catch { if (tagTr.tag == wantTag) return true; }
        }
        if (want != null && StripClone(tagTr.name) == want.name) return true;

        return false;
    }

    // ======================= STRETCHER =======================
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
        RegisterFinished();
    }

    // ======================= TIME =======================
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

    // ======================= HELPERS =======================
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

        if (!on && socket.hasSelection && socket.interactionManager != null)
        {
            var sel = socket.interactablesSelected.FirstOrDefault();
            if (sel != null) socket.interactionManager.SelectExit(socket, sel);
        }
    }
}
