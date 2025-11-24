using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
public class TriagePatientBlack_WithStretcher : MonoBehaviour
{
    // ======================================================================
    //  TRIAGE TAG SOCKET (ตัวรับบัตรจริง)
    // ======================================================================
    [Header("Triage Tag Socket (Receiver)")]
    [Tooltip("GameObject ของซ็อกเก็ตตัวรับ (เปิดตอนเริ่ม)")]
    public GameObject triageTagSocketObject;

    [Tooltip("XRSocketInteractor ของซ็อกเก็ตตัวรับ")]
    public XRSocketInteractor triageTagSocket;

    [Header("บัตรที่ถูกต้อง (ตั้งให้เป็น Black)")]
    [Tooltip("ลาก Prefab/Instance ของบัตรดำ (เช่น Black_Tag-Triage) มาเพื่อตรวจด้วยชื่อ")]
    public GameObject validBlackTagPrefab;

    [Tooltip("หรือกำหนด Tag ของบัตร เช่น \"Black\" (ถ้าอยากตรวจด้วย Tag)")]
    public string validBlackTagName = "Black";

    // ======================================================================
    //  DISPLAY GROUP (ตัวโชว์ทับที่ซ็อกเก็ตรับหลังรับบัตรแล้ว)
    // ======================================================================
    [Header("Tag Display Group (ตัวโชว์แทนหลังรับบัตร)")]
    [Tooltip("กลุ่มอ็อบเจ็กต์ที่ใช้โชว์บัตร/ซ็อกเก็ตสำเหร้บ (ลากจาก Hierarchy)")]
    public GameObject tagDisplayGroup;

    // ======================================================================
    //  DESTINATIONS PER PLANE (A–F)
    // ======================================================================
    [Header("Destinations per Plane (A–F)")]
    [Tooltip("จุดไปส่งเมื่อเลือกเล่น PlaneForSpawn_A")]
    public Transform destinationA;
    [Tooltip("จุดไปส่งเมื่อเลือกเล่น PlaneForSpawn_B")]
    public Transform destinationB;
    [Tooltip("จุดไปส่งเมื่อเลือกเล่น PlaneForSpawn_C")]
    public Transform destinationC;
    [Tooltip("จุดไปส่งเมื่อเลือกเล่น PlaneForSpawn_D")]
    public Transform destinationD;
    [Tooltip("จุดไปส่งเมื่อเลือกเล่น PlaneForSpawn_E")]
    public Transform destinationE;
    [Tooltip("จุดไปส่งเมื่อเลือกเล่น PlaneForSpawn_F")]
    public Transform destinationF;

    [Tooltip("ปลายทางสำรอง ถ้าอ่านพื้นที่ไม่เจอหรือยังไม่ได้เลือก")]
    public Transform defaultDestination;

    // ======================================================================
    //  STRETCHER (สปอว์นเปล แล้วยก/ย้ายผู้บาดเจ็บไปยังจุดหมาย)
    // ======================================================================
    [Header("Stretcher Settings")]
    [Tooltip("Prefab ของเปลที่จะสปอว์น")]
    public GameObject stretcherPrefab;

    [Tooltip("Transform ของผู้บาดเจ็บ (root ที่ต้องย้าย/จับวางบนเปล)")]
    public Transform patientRoot;

    [Tooltip("ปลายทางที่ต้องพาคนเจ็บไป (ถูกเขียนทับอัตโนมัติจาก A–F เมื่อเริ่มเคลื่อน)")]
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
    //  EVENTS (เลือกใช้)
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

    // ======================================================================
    //  ScoreV2.5 hooks
    // ======================================================================
    private ScoreV2_5 _score;
    private ScoreV2_5 Score() { if (_score == null) _score = FindObjectOfType<ScoreV2_5>(); return _score; }
    private void RegisterColor(bool correct = true) { Score()?.RegisterTagResult(ScoreV2_5.TriageColor.Black, correct); }
    private void RegisterFinished() { Score()?.RegisterPatientFinished(); }

    // ======================================================================
    //  LIFECYCLE
    // ======================================================================
    void Awake()
    {
        // เปิดตัวรับ / ปิดตัวโชว์ตอนเริ่ม
        if (triageTagSocketObject) triageTagSocketObject.SetActive(true);
        if (tagDisplayGroup) tagDisplayGroup.SetActive(false);

        if (triageTagSocket)
            triageTagSocket.selectEntered.AddListener(OnTriageTagPlaced);

        // เผื่ออ่านพื้นที่ได้ตั้งแต่ต้น ก็เลือกปลายทางให้ก่อน (ไม่ซีเรียส ถ้าเปลี่ยนภายหลังจะรีเฟรชอีกครั้ง)
        RefreshDestinationFromArea();
    }

    void OnDestroy()
    {
        if (triageTagSocket)
            triageTagSocket.selectEntered.RemoveListener(OnTriageTagPlaced);
    }

    void Update()
    {
        // กันอีเวนต์หลุด: ถ้ามีของค้างในซ็อกเก็ตและยังไม่ accept ให้ตรวจซ้ำ
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
        var tagTr = (sel as Component)?.transform;
        if (tagTr == null) return;

        // ตรวจว่าบัตร "ดำ" ถูกต้องหรือไม่
        if (!IsCorrectBlackTag(tagTr))
        {
            EjectWrong(triageTagSocket);
            return;
        }

        // ===== โหมด "โชว์กลุ่มแทน" ตามที่ร้องขอ =====
        // 1) ปิด/ล้างซ็อกเก็ตตัวรับ
        if (triageTagSocket.interactionManager != null && sel != null)
            triageTagSocket.interactionManager.SelectExit(triageTagSocket, sel);

        triageTagSocket.enabled = false;
        if (triageTagSocketObject) triageTagSocketObject.SetActive(false);

        // 2) ซ่อนบัตรที่ผู้เล่นนำมาวาง (ไม่ทำลาย)
        tagTr.gameObject.SetActive(false);

        // 3) เปิด "Tag Display Group" ที่คุณคัดลอกมาวางทับตำแหน่งเดิม
        if (tagDisplayGroup) tagDisplayGroup.SetActive(true);

        _triageAccepted = true;
        onBlackTagAccepted?.Invoke();

        // ส่งคะแนน (บัตรดำถูกต้อง)
        RegisterColor(true);

        // รีเฟรชปลายทางจากพื้นที่ที่เลือก ณ ตอนนี้ ก่อนจะเริ่มเคลื่อน
        RefreshDestinationFromArea();

        // เคลื่อนย้ายด้วยเปล
        if (!_stretcherSpawned) StartCoroutine(Co_SpawnStretcherAndMove());
    }

    bool IsCorrectBlackTag(Transform tagTr)
    {
        if (tagTr == null) return false;

        // 1) ตรวจด้วย Tag
        if (!string.IsNullOrEmpty(validBlackTagName))
        {
            try { if (tagTr.CompareTag(validBlackTagName)) return true; }
            catch { if (tagTr.tag == validBlackTagName) return true; }
        }

        // 2) ตรวจด้วยชื่อ Prefab (ตัด "(Clone)")
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
    //  DESTINATION RESOLUTION (A–F)
    // ======================================================================
    public void RefreshDestinationFromArea()
    {
        destinationPoint = ResolveDestinationForArea(GetCurrentAreaId()) ?? destinationPoint ?? defaultDestination;
    }

    Transform ResolveDestinationForArea(string areaId)
    {
        if (string.IsNullOrEmpty(areaId)) return defaultDestination;

        switch (areaId.Trim().ToUpperInvariant())
        {
            case "A": return destinationA ? destinationA : defaultDestination;
            case "B": return destinationB ? destinationB : defaultDestination;
            case "C": return destinationC ? destinationC : defaultDestination;
            case "D": return destinationD ? destinationD : defaultDestination;
            case "E": return destinationE ? destinationE : defaultDestination;
            case "F": return destinationF ? destinationF : defaultDestination;
            default:  return defaultDestination;
        }
    }

    string GetCurrentAreaId()
    {
        var s = Score();
        if (s == null) return null;

        // 1) ถ้ามี property public เช่น CurrentAreaId / CurrentArea / AreaId ให้ใช้ก่อน
        var propNames = new[] { "CurrentAreaId", "CurrentArea", "AreaId" };
        foreach (var pn in propNames)
        {
            var p = s.GetType().GetProperty(pn, BindingFlags.Instance | BindingFlags.Public);
            if (p != null && p.PropertyType == typeof(string))
            {
                var v = p.GetValue(s) as string;
                if (!string.IsNullOrEmpty(v)) return v;
            }
        }

        // 2) รองรับ field ภายใน (จากเวอร์ชันก่อน ๆ): _currentArea / currentArea
        var fieldNames = new[] { "_currentArea", "currentArea" };
        foreach (var fn in fieldNames)
        {
            var f = s.GetType().GetField(fn, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (f != null && f.FieldType == typeof(string))
            {
                var v = f.GetValue(s) as string;
                if (!string.IsNullOrEmpty(v)) return v;
            }
        }

        return null;
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

        // 4) เคลื่อนเปลไปยังจุดหมาย (ใช้ปลายทางที่รีเฟรชแล้ว)
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

        // ส่งผลคะแนน: รายนี้เสร็จสมบูรณ์
        RegisterFinished();
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
