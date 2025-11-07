using UnityEngine;

/// <summary>
/// แนบไว้กับ World-Space UI / ป้าย / โมเดล
/// จะหมุนหากล้องผู้เล่นตลอดเวลา (Billboard)
/// รองรับโหมดหมุนแค่แกน Y (ป้ายหันตามแนวนอน) หรือหันเต็ม 3D
/// </summary>
[ExecuteAlways]
public class FacePlayer : MonoBehaviour
{
    [Header("Target (ถ้าเว้นว่างจะหา MainCamera ให้อัตโนมัติ)")]
    public Transform target;
    public bool findMainCameraOnStart = true;

    [Header("หมุนแบบไหน")]
    [Tooltip("เปิด = หมุนเฉพาะแกน Y (ป้ายหันหาแบบแนวนอน), ปิด = หมุนเต็ม 3D")]
    public bool horizontalOnly = true;

    [Tooltip("ใช้แนว up ของกล้อง (ตามการเอียงหัว) เมื่อ horizontalOnly ปิด")]
    public bool matchCameraRoll = false;

    [Header("ปรับจูนการหมุน")]
    [Tooltip("หมุนแบบ Smooth (ในโหมด Play)")]
    public bool smooth = true;
    [Tooltip("ค่ายืดหยุ่นของการหมุน ยิ่งมากยิ่งตามไว (หน่วย ≈ 1/วินาที)")]
    public float smoothSpeed = 10f;

    [Tooltip("ชดเชยมุมเพิ่มหลังหมุนหาเป้าหมายแล้ว (องศา)")]
    public Vector3 rotationOffsetEuler = Vector3.zero;

    void Reset()
    {
        findMainCameraOnStart = true;
        horizontalOnly = true;
        smooth = true;
        smoothSpeed = 10f;
    }

    void Awake()  { TryFindTarget(); }
    void OnEnable(){ TryFindTarget(); }

    void TryFindTarget()
    {
        if (target == null && findMainCameraOnStart)
        {
            var cam = Camera.main;
            if (cam != null) target = cam.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null) { TryFindTarget(); if (target == null) return; }

        Quaternion targetRot;

        if (horizontalOnly)
        {
            // มองเฉพาะระนาบพื้น (แกน Y)
            Vector3 look = target.position - transform.position;
            look.y = 0f;                           // ตัดความสูงออก
            if (look.sqrMagnitude < 0.0001f) return;
            targetRot = Quaternion.LookRotation(look.normalized, Vector3.up);
        }
        else
        {
            // มองแบบเต็ม 3D
            Vector3 dir = (target.position - transform.position);
            if (dir.sqrMagnitude < 0.0001f) return;
            Vector3 up = matchCameraRoll ? target.up : Vector3.up;
            targetRot = Quaternion.LookRotation(dir.normalized, up);
        }

        // ชดเชยมุมเพิ่มเติม
        if (rotationOffsetEuler != Vector3.zero)
            targetRot *= Quaternion.Euler(rotationOffsetEuler);

        // หมุนแบบลื่นในโหมดเล่นเกม
        if (smooth && Application.isPlaying)
        {
            // exponential smoothing
            float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
        }
        else
        {
            transform.rotation = targetRot;
        }
    }
}
