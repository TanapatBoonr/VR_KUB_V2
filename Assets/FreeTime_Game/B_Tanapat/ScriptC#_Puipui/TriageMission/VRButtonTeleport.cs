using UnityEngine;
using UnityEngine.UI;   // สำหรับปุ่ม UI
using UnityEngine.XR.Interaction.Toolkit; // ถ้าใช้ XR Rig
using UnityEngine.SceneManagement;        // เผื่อในอนาคตต้องโหลด Scene

public class VRButtonTeleport : MonoBehaviour
{
    [Header("ตั้งค่าตำแหน่งปลายทาง")]
    [Tooltip("จุดที่จะวาร์ปผู้เล่นไป (ลาก Transform ของ TeleportTarget มาใส่)")]
    public Transform teleportTarget;

    [Header("ตั้งค่า Player/XR Rig")]
    [Tooltip("ลาก Player หรือ XR Rig ที่จะย้ายตำแหน่ง")]
    public Transform playerRig;

    [Header("ปุ่ม UI ที่จะใช้กดวาร์ป")]
    [Tooltip("ลากปุ่ม Button จาก Canvas มาใส่")]
    public Button teleportButton;

    private void Start()
    {
        // ตรวจสอบว่าปุ่มถูกกำหนดแล้วหรือยัง
        if (teleportButton != null)
        {
            teleportButton.onClick.AddListener(OnTeleportButtonPressed);
        }
        else
        {
            Debug.LogWarning($"{name}: ยังไม่ได้กำหนด Button ใน Inspector!");
        }
    }

    private void OnDestroy()
    {
        // ล้าง Event เมื่อถูกลบออกจาก Scene
        if (teleportButton != null)
        {
            teleportButton.onClick.RemoveListener(OnTeleportButtonPressed);
        }
    }

    private void OnTeleportButtonPressed()
    {
        if (playerRig == null || teleportTarget == null)
        {
            Debug.LogWarning($"{name}: PlayerRig หรือ TeleportTarget ยังไม่ถูกตั้งค่า!");
            return;
        }

        // ย้ายตำแหน่งของ PlayerRig ไปที่จุดที่กำหนด
        playerRig.position = teleportTarget.position;
        playerRig.rotation = teleportTarget.rotation;

        Debug.Log($"Player ถูกวาร์ปไปยัง {teleportTarget.name}");
    }
}