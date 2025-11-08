using UnityEngine;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit;

public class StretcherSpawner_Adjustable : MonoBehaviour
{
    [Header("XR Socket (ต้องใส่)")]
    [Tooltip("XR Socket ของผู้บาดเจ็บ (ให้ลาก XRSocketInteractor มาวางที่นี่)")]
    public XRSocketInteractor tagSocket;

    [Header("เงื่อนไขบัตรที่อนุญาต")]
    [Tooltip("Tag ของบัตรที่ต้องการ (เช่น Black, Red, Yellow, Green)")]
    public string requiredItemTag = "Black";
    [Tooltip("ถ้าตั้งไว้ จะตรวจชื่อ Prefab/วัตถุด้วย (เช่น Black_Tag-Triage) — ไม่ตั้งได้")]
    public string optionalRequiredNameContains = "Black_Tag-Triage";

    [Header("References")]
    [Tooltip("Prefab ของ Army Stretcher")]
    public GameObject stretcherPrefab;

    [Tooltip("Transform ของผู้บาดเจ็บ (root ที่จะย้ายวางบนเปล)")]
    public Transform patientTransform;

    [Tooltip("จุดปลายทางที่เปลจะเคลื่อนไป (ไม่บังคับ)")]
    public Transform destinationPoint;

    [Header("Stretcher Placement")]
    [Tooltip("ระยะยก/กดเปลจากตำแหน่งผู้บาดเจ็บ (โลก)")]
    public float stretcherVerticalOffset = -0.05f;
    [Tooltip("หมุนเพิ่มของเปล (องศา)")]
    public Vector3 stretcherRotationOffset = Vector3.zero;

    [Header("Patient Alignment (บนเปล)")]
    [Tooltip("ใช้ Anchor บนเปลหรือไม่ (เช่น Empty ชื่อ PatientAnchor)")]
    public bool useStretcherAnchor = true;
    [Tooltip("ชื่อ Anchor บนเปล (ถ้าไม่พบจะใช้ตัวเปลเอง)")]
    public string stretcherAnchorName = "PatientAnchor";

    [Tooltip("ออฟเซ็ตตำแหน่ง 'ท้องถิ่น' ของผู้บาดเจ็บบนเปล (แกนของ Anchor/เปล)")]
    public Vector3 patientLocalOffset = new Vector3(0f, 0.05f, 0f);

    [Tooltip("ออฟเซ็ตตำแหน่ง 'โลก' เพิ่มเติม (ขยับละเอียดหลังจากคำนวณ local แล้ว)")]
    public Vector3 patientWorldOffset = Vector3.zero;

    [Tooltip("ไม่เปลี่ยน Rotation ของผู้บาดเจ็บ (โฟกัสเฉพาะ Position)")]
    public bool dontChangePatientRotation = true;

    [Tooltip("อัปเดตการจัดวางผู้บาดเจ็บซ้ำ ๆ ทุกเฟรม (ช่วยปรับค่าใน Play Mode แบบเรียลไทม์)")]
    public bool continuousRepositionWhilePlaying = true;

    [Tooltip("ให้ผู้บาดเจ็บเป็นลูกของเปล (จะเคลื่อนตาม)")]
    public bool parentPatientToStretcher = true;

    [Header("Movement Settings")]
    public float moveSpeed = 1.5f;
    public float arriveThreshold = 0.1f;

    // ภายใน
    private bool stretcherSpawned = false;
    private GameObject spawnedStretcher;
    private Transform cachedAnchor;

    void OnEnable()
    {
        if (tagSocket != null)
        {
            tagSocket.selectEntered.AddListener(OnSocketSelectEntered);
            // (ไม่จำเป็น แต่ถ้าอยากรู้ตอนเอาออก)
            // tagSocket.selectExited.AddListener(OnSocketSelectExited);
        }
        else
        {
            Debug.LogWarning($"{name}: ยังไม่ได้อ้างอิง XRSocketInteractor (tagSocket).");
        }
    }

    void OnDisable()
    {
        if (tagSocket != null)
        {
            tagSocket.selectEntered.RemoveListener(OnSocketSelectEntered);
            // tagSocket.selectExited.RemoveListener(OnSocketSelectExited);
        }
    }

    // ถูกเรียก “เมื่อตัวบัตรถูกวางลงใน Socket แล้วจริง ๆ”
    private void OnSocketSelectEntered(SelectEnterEventArgs args)
    {
        if (stretcherSpawned) return; // ทำครั้งเดียวพอ

        var tr = args.interactableObject.transform;
        var go = tr.gameObject;

        // 1) ตรวจ Tag (ถ้ากำหนด)
        if (!string.IsNullOrEmpty(requiredItemTag))
        {
            if (!go.CompareTag(requiredItemTag))
            {
                // ถ้าบัตรไม่ใช่ Tag ที่ต้องการ ให้ Socket ปล่อยออก (กันวางผิด)
                tagSocket.interactionManager.SelectExit(tagSocket, args.interactableObject);
                Debug.LogWarning($"{name}: วางบัตรผิดสี/ผิด Tag (ต้องการ Tag: {requiredItemTag})");
                return;
            }
        }

        // 2) ตรวจชื่อ (ถ้าตั้งไว้เพื่อกันผิด Prefab)
        if (!string.IsNullOrEmpty(optionalRequiredNameContains))
        {
            if (!go.name.Contains(optionalRequiredNameContains))
            {
                tagSocket.interactionManager.SelectExit(tagSocket, args.interactableObject);
                Debug.LogWarning($"{name}: วางบัตรไม่ถูกต้อง (ต้องมีชื่อรวมคำว่า: {optionalRequiredNameContains})");
                return;
            }
        }

        // ผ่านเงื่อนไข → ค่อยสั่งสร้างเปล
        StartCoroutine(SpawnAndAlign());
    }

    private IEnumerator SpawnAndAlign()
    {
        if (stretcherSpawned) yield break;
        stretcherSpawned = true;

        if (stretcherPrefab == null || patientTransform == null)
        {
            Debug.LogError($"{name}: อ้างอิงไม่ครบ (stretcherPrefab / patientTransform)");
            yield break;
        }

        // สร้างเปล “ใต้ผู้บาดเจ็บ” และหมุนตาม + offset
        Quaternion baseRot = patientTransform.rotation * Quaternion.Euler(stretcherRotationOffset);
        Vector3 spawnPos = patientTransform.position + new Vector3(0, stretcherVerticalOffset, 0);
        spawnedStretcher = Instantiate(stretcherPrefab, spawnPos, baseRot);

        yield return new WaitForSeconds(0.05f); // เผื่อประกอบ

        // หา Anchor
        cachedAnchor = spawnedStretcher.transform;
        if (useStretcherAnchor)
        {
            Transform found = spawnedStretcher.transform.Find(stretcherAnchorName);
            if (found != null) cachedAnchor = found;
        }

        // จัดวางผู้บาดเจ็บบนเปล (เฉพาะตำแหน่ง)
        ApplyPatientPosition();

        if (parentPatientToStretcher)
            patientTransform.SetParent(spawnedStretcher.transform, true);

        // ย้ายไปจุดปลายทาง (ถ้ามี)
        if (destinationPoint != null)
            StartCoroutine(MoveToDestination());
    }

    void LateUpdate()
    {
        if (continuousRepositionWhilePlaying && spawnedStretcher != null && cachedAnchor != null)
        {
            ApplyPatientPosition();
        }
    }

    private void ApplyPatientPosition()
    {
        if (patientTransform == null || cachedAnchor == null) return;

        Vector3 worldFromLocal = cachedAnchor.TransformPoint(patientLocalOffset);
        Vector3 finalWorldPos = worldFromLocal + patientWorldOffset;

        patientTransform.position = finalWorldPos;

        if (!dontChangePatientRotation)
        {
            patientTransform.rotation = cachedAnchor.rotation;
        }
    }

    private IEnumerator MoveToDestination()
    {
        if (spawnedStretcher == null || destinationPoint == null) yield break;

        while (Vector3.Distance(spawnedStretcher.transform.position, destinationPoint.position) > arriveThreshold)
        {
            Vector3 dir = (destinationPoint.position - spawnedStretcher.transform.position).normalized;
            spawnedStretcher.transform.position += dir * moveSpeed * Time.deltaTime;
            yield return null;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (spawnedStretcher != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(spawnedStretcher.transform.position, new Vector3(0.2f, 0.02f, 0.6f));
        }
    }
}
