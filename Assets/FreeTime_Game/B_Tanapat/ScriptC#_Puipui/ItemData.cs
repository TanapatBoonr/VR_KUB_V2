using UnityEngine;

public class ItemData : MonoBehaviour
{
    // ตัวแปรสำหรับเก็บข้อมูลของไอเทมแต่ละชิ้น
    public string itemName;
    public string itemDescription;

    // ตัวแปรสำหรับอ้างอิงถึง UI Prefab
    public GameObject uiPrefab;

    // **ใหม่**: ตัวแปรสำหรับกำหนดตำแหน่งที่จะให้ UI แสดงผล
    public Transform uiDisplayPoint;

    // ตัวแปรส่วนตัวเพื่อเก็บการอ้างอิงถึง UI ที่ถูกสร้างขึ้นในฉาก
    private GameObject currentUIInstance;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (currentUIInstance == null)
            {
                // สร้าง UI Prefab ขึ้นมาในตำแหน่งที่กำหนดไว้
                // โดยใช้ uiDisplayPoint.position แทนการคำนวณจาก transform.position
                currentUIInstance = Instantiate(uiPrefab, uiDisplayPoint.position, Quaternion.identity);

                ItemUIController controller = currentUIInstance.GetComponent<ItemUIController>();
                if (controller != null)
                {
                    controller.UpdateUI(itemName, itemDescription);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (currentUIInstance != null)
            {
                Destroy(currentUIInstance);
            }
        }
    }
}