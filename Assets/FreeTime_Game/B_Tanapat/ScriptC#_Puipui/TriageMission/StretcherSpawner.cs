using UnityEngine;
using System.Collections;

public class StretcherSpawner_Adjustable : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Prefab ของ Army Stretcher")]
    public GameObject stretcherPrefab;

    [Tooltip("Transform ของผู้บาดเจ็บ (root ที่จะย้ายวางบนเปล)")]
    public Transform patientTransform;

    [Tooltip("จุดปลายทางที่เปลจะเคลื่อนไป (ไม่บังคับ)")]
    public Transform destinationPoint;

    [Header("Triage Tag Settings")]
    [Tooltip("Prefab ของบัตรที่ถูกต้อง (Red_Tag-Triage หรือ Yellow_Tag-Triage)")]
    public GameObject correctTagPrefab;

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

    // ====== จุดสำคัญ: ตัวปรับตำแหน่งผู้บาดเจ็บบนเปล ======
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

    private bool stretcherSpawned = false;
    private GameObject spawnedStretcher;
    private Transform cachedAnchor; // เก็บ Anchor ที่ใช้จริง

    void OnTriggerEnter(Collider other)
    {
        if (stretcherSpawned) return;

        // ตรวจด้วยชื่อ (กันกรณี Tag ยังไม่ตั้ง)
        if (correctTagPrefab != null && other.name.Contains(correctTagPrefab.name))
        {
            StartCoroutine(SpawnAndAlign());
        }
    }

    private IEnumerator SpawnAndAlign()
    {
        stretcherSpawned = true;

        if (stretcherPrefab == null || patientTransform == null)
        {
            Debug.LogError($"{name}: อ้างอิงไม่ครบ (stretcherPrefab / patientTransform)");
            yield break;
        }

        // 1) สร้างเปล “ใต้ผู้บาดเจ็บ” และหมุนตามผู้บาดเจ็บ + offset
        Quaternion baseRot = patientTransform.rotation * Quaternion.Euler(stretcherRotationOffset);
        Vector3 spawnPos = patientTransform.position + new Vector3(0, stretcherVerticalOffset, 0);

        spawnedStretcher = Instantiate(stretcherPrefab, spawnPos, baseRot);

        // เผื่อให้ Mesh/Physics ประกอบตัว
        yield return new WaitForSeconds(0.05f);

        // 2) หา Anchor บนเปล (ถ้าเลือกใช้)
        cachedAnchor = spawnedStretcher.transform;
        if (useStretcherAnchor)
        {
            Transform found = spawnedStretcher.transform.Find(stretcherAnchorName);
            if (found != null) cachedAnchor = found;
        }

        // 3) จัดวางผู้บาดเจ็บบนเปล (ปรับเฉพาะตำแหน่ง)
        ApplyPatientPosition();

        if (parentPatientToStretcher)
        {
            patientTransform.SetParent(spawnedStretcher.transform, true);
        }

        // 4) เคลื่อนไปจุดปลายทางถ้ามี
        if (destinationPoint != null)
        {
            StartCoroutine(MoveToDestination());
        }
    }

    // เรียกทุกเฟรมเพื่อให้ปรับค่า X/Y/Z ได้สด ๆ ตอนเทส
    void LateUpdate()
    {
        if (continuousRepositionWhilePlaying && spawnedStretcher != null && cachedAnchor != null)
        {
            ApplyPatientPosition();
        }
    }

    /// <summary>
    /// จัดตำแหน่งผู้บาดเจ็บบนเปล โดย "เน้นเฉพาะ Position"
    /// - ใช้ local offset บนแกนของ Anchor/เปล
    /// - บวก world offset เพิ่มเติม
    /// - Rotation ของผู้บาดเจ็บจะไม่ถูกเปลี่ยนถ้า dontChangePatientRotation = true
    /// </summary>
    private void ApplyPatientPosition()
    {
        if (patientTransform == null || cachedAnchor == null) return;

        // world position จาก local offset
        Vector3 worldFromLocal = cachedAnchor.TransformPoint(patientLocalOffset);
        Vector3 finalWorldPos = worldFromLocal + patientWorldOffset;

        patientTransform.position = finalWorldPos;

        if (!dontChangePatientRotation)
        {
            // ถ้าต้องการให้หมุนตาม Anchor ให้เปิด option นี้ (ปิดค่า default)
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

    // วาด gizmo ช่วยตั้งตำแหน่ง (ตอนเลือกวัตถุ)
    void OnDrawGizmosSelected()
    {
        if (spawnedStretcher != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(spawnedStretcher.transform.position, new Vector3(0.2f, 0.02f, 0.6f));
        }
    }
}
