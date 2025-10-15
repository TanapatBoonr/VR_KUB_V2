using UnityEngine;

public class UIBagTrigger : MonoBehaviour
{
    // ตัวแปรสำหรับอ้างอิงถึง Canvas ที่มีปุ่มอยู่
    public GameObject bagUI;

    private void OnTriggerEnter(Collider other)
    {
        // เมื่อผู้เล่นเข้าใกล้ ให้แสดง UI ของกระเป๋า
        if (other.CompareTag("Player") && bagUI != null)
        {
            bagUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // เมื่อผู้เล่นเดินออก ให้ซ่อน UI ของกระเป๋า
        if (other.CompareTag("Player") && bagUI != null)
        {
            bagUI.SetActive(false);
        }
    }
}