using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class EmergencyPatientController : MonoBehaviour
{
    [Header("Triage Settings")]
    [Tooltip("Red หรือ Yellow")]
    public TriageColor patientColor = TriageColor.Red;

    [Tooltip("Prefab ของบัตรที่ถูกต้อง (ลาก Prefab เข้ามา)")]
    public GameObject correctTagPrefab;

    [Tooltip("XR Socket Interactor ของ NPC")]
    public XRSocketInteractor tagSocket;

    private bool isTagged = false;

    void Start()
    {
        if (tagSocket != null)
        {
            tagSocket.selectEntered.AddListener(OnTagPlaced);
            Debug.Log($"{name}: XR Socket พร้อมทำงาน");
        }
        else
        {
            Debug.LogError($"{name}: ❌ ยังไม่ได้ตั้งค่า XR Socket ใน Inspector!");
        }
    }

    void OnDestroy()
    {
        if (tagSocket != null)
            tagSocket.selectEntered.RemoveListener(OnTagPlaced);
    }

    // ✅ เมื่อบัตรถูกวางลงใน XR Socket สำเร็จ
    private void OnTagPlaced(SelectEnterEventArgs args)
    {
        if (isTagged) return;

        GameObject tagObj = args.interactableObject.transform.gameObject;
        Debug.Log($"{name}: ตรวจพบว่ามีบัตร '{tagObj.name}' วางใน XR Socket แล้ว");

        // ตรวจสอบว่าบัตรถูกต้องหรือไม่
        if (correctTagPrefab != null && tagObj.name.Contains(correctTagPrefab.name))
        {
            Debug.Log($"{name}: ✅ บัตรถูกต้อง ตรงกับ {correctTagPrefab.name}");
            isTagged = true;
        }
        else
        {
            Debug.LogWarning($"{name}: ❌ บัตรไม่ถูกต้อง ต้องใช้ {correctTagPrefab?.name}");
            tagSocket.interactionManager.SelectExit(tagSocket, args.interactableObject);
        }
    }

    // ✅ ฟังก์ชันป้องกัน Error จากสคริปต์อื่น
    public void ReceiveTriageTag(string color)
    {
        Debug.Log($"{name}: ReceiveTriageTag() ถูกเรียก (ข้าม เพราะใช้ XR Socket เท่านั้น)");
    }

    public void OnWoundTreated() { }
}