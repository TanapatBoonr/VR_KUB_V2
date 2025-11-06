using UnityEngine;
using System.Collections;

public class StretcherSpawner_Adjustable : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Prefab ของ Army Stretcher")]
    public GameObject stretcherPrefab;

    [Tooltip("Transform ของผู้บาดเจ็บ")]
    public Transform patientTransform;

    [Tooltip("จุดปลายทางที่เปลจะเคลื่อนไป")]
    public Transform destinationPoint;

    [Header("Triage Tag Settings")]
    [Tooltip("Prefab ของบัตรที่ถูกต้อง (Red_Tag-Triage หรือ Yellow_Tag-Triage)")]
    public GameObject correctTagPrefab;

    [Header("Alignment Settings")]
    [Tooltip("ระยะห่างของเปลจากตำแหน่งผู้บาดเจ็บ (แกน Y ลบคืออยู่ใต้ผู้บาดเจ็บ)")]
    public float stretcherVerticalOffset = -0.05f;

    [Tooltip("ระยะห่างของผู้บาดเจ็บจากเปล (แกน Y บวกคือสูงขึ้น)")]
    public float patientVerticalOffset = 0.05f;

    [Tooltip("หมุนเพิ่มของเปล (องศา)")]
    public Vector3 stretcherRotationOffset = Vector3.zero;

    [Tooltip("หมุนเพิ่มของผู้บาดเจ็บ (องศา)")]
    public Vector3 patientRotationOffset = Vector3.zero;

    [Tooltip("ให้ผู้บาดเจ็บเป็นลูกของเปล (เคลื่อนไปพร้อมกัน)")]
    public bool parentPatientToStretcher = true;

    [Header("Movement Settings")]
    public float moveSpeed = 1.5f;

    private bool stretcherSpawned = false;
    private GameObject spawnedStretcher;

    void OnTriggerEnter(Collider other)
    {
        if (stretcherSpawned) return;

        if (correctTagPrefab != null && other.name.Contains(correctTagPrefab.name))
        {
            Debug.Log($"{name}: ✅ ตรวจพบบัตร {other.name} แล้วจะสร้างเปลหาม!");
            StartCoroutine(SpawnAndAlign());
        }
    }

    private IEnumerator SpawnAndAlign()
    {
        stretcherSpawned = true;

        // 1️⃣ สร้างเปลตรงใต้ผู้ป่วย โดยหมุนตามผู้ป่วย + ค่าที่ตั้งใน Inspector
        Quaternion baseRot = patientTransform.rotation * Quaternion.Euler(stretcherRotationOffset);
        Vector3 spawnPos = patientTransform.position + new Vector3(0, stretcherVerticalOffset, 0);

        spawnedStretcher = Instantiate(stretcherPrefab, spawnPos, baseRot);
        Debug.Log($"{name}: ✅ สร้างเปลแล้วที่ {spawnPos}");

        yield return new WaitForSeconds(0.2f);

        // 2️⃣ จัดตำแหน่งผู้ป่วยให้นอนบนเปล พร้อมการหมุนที่ปรับได้
        Quaternion patientRot = patientTransform.rotation * Quaternion.Euler(patientRotationOffset);
        Vector3 targetPos = spawnedStretcher.transform.position + new Vector3(0, patientVerticalOffset, 0);

        patientTransform.rotation = patientRot;
        patientTransform.position = targetPos;

        if (parentPatientToStretcher)
        {
            patientTransform.SetParent(spawnedStretcher.transform, true);
        }

        Debug.Log($"{name}: ✅ ผู้ป่วยแนบกับเปลเรียบร้อย");

        yield return new WaitForSeconds(0.5f);

        // 3️⃣ เคลื่อนไปจุดปลายทาง (หากตั้งค่าไว้)
        if (destinationPoint != null)
        {
            StartCoroutine(MoveToDestination());
        }
    }

    private IEnumerator MoveToDestination()
    {
        if (spawnedStretcher == null || destinationPoint == null)
            yield break;

        while (Vector3.Distance(spawnedStretcher.transform.position, destinationPoint.position) > 0.1f)
        {
            Vector3 dir = (destinationPoint.position - spawnedStretcher.transform.position).normalized;
            spawnedStretcher.transform.position += dir * moveSpeed * Time.deltaTime;

            yield return null;
        }

        Debug.Log($"{name}: ✅ เปลถึงจุดปลายทางเรียบร้อยแล้ว");
    }
}
