using UnityEngine;

public class FacingCameraYaw : MonoBehaviour
{
    [Tooltip("ลาก Camera Offset หรือ Main Camera มาใส่")]
    public Transform cameraTransform;

    [Tooltip("ระยะห่างจาก Player ที่ Socket ควรอยู่ (สำหรับปรับตำแหน่ง)")]
    public float followDistance = 0.5f;

    [Tooltip("ความสูงสัมพัทธ์ของ Socket (สำหรับปรับความสูง)")]
    public float yOffset = -0.3f;
    
    [Header("--- Smoothing Settings ---")]
    [Tooltip("ความเร็วในการหมุนตามกล้อง (ค่าที่เหมาะสมคือ 5 - 15)")]
    public float rotationSpeed = 10f; // NEW: ความเร็วในการหมุน
    
    [Tooltip("ความเร็วในการตามตำแหน่งของ Player (ค่าที่เหมาะสมคือ 5 - 15)")]
    public float positionSpeed = 10f; // NEW: ความเร็วในการตามตำแหน่ง

    private Vector3 initialLocalPosition;

    void Start()
    {
        // บันทึกตำแหน่ง Local Position เดิมของ Socket
        // เราจะใช้ค่า X (ซ้าย/ขวา) ของมันเป็นตัวกำหนด Lateral Offset
        initialLocalPosition = transform.localPosition;
    }

    void LateUpdate()
    {
        if (cameraTransform == null)
        {
            Debug.LogError("Camera Transform is not assigned to FacingCameraYaw on " + gameObject.name);
            return;
        }

        // ********** 1. จัดการการหมุน (Rotation) - ใช้ Slerp **********
        
        // คำนวณ Rotation เป้าหมาย (ใช้ Yaw ของกล้องเท่านั้น)
        Quaternion targetYawRotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
        
        // ใช้ Slerp (Spherical Linear Interpolation) เพื่อให้การหมุนนุ่มนวล
        transform.rotation = Quaternion.Slerp(
            transform.rotation, 
            targetYawRotation, 
            Time.deltaTime * rotationSpeed
        );

        // ********** 2. จัดการตำแหน่ง (Position) - ใช้ Lerp **********
        
        // 2.1 คำนวณตำแหน่งศูนย์กลางของ Rig
        Vector3 rigCenterPosition = cameraTransform.parent.position;
        
        // 2.2 คำนวณตำแหน่งเป้าหมายสุดท้าย (Target Position)
        Vector3 targetPosition = rigCenterPosition;
        targetPosition.y += yOffset;
        targetPosition += transform.forward * followDistance; // เดินหน้าตามทิศทางที่ Socket กำลังหัน
        targetPosition += transform.right * initialLocalPosition.x; // ชดเชยซ้าย/ขวา

        // ใช้ Lerp (Linear Interpolation) เพื่อให้การตามตำแหน่งนุ่มนวล
        transform.position = Vector3.Lerp(
            transform.position, 
            targetPosition, 
            Time.deltaTime * positionSpeed
        );
    }
}
