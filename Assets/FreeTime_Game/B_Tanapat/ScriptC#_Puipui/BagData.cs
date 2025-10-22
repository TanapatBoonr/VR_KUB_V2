using UnityEngine;
using UnityEngine.UI; // อย่าลืมเพิ่ม!

public class BagData : MonoBehaviour
{
    public GameObject openButtonPrefab;
    public GameObject closeButtonPrefab;
    public Transform buttonSpawnPoint;
    public BagController bagController;

    private GameObject currentOpenButton;
    private GameObject currentCloseButton;

    void Start()
    {
        // สร้างปุ่ม Open และ Close ขึ้นมาตั้งแต่แรก แต่ซ่อนไว้
        if (buttonSpawnPoint != null)
        {
            currentOpenButton = Instantiate(openButtonPrefab, buttonSpawnPoint.position, Quaternion.identity);
            currentOpenButton.transform.SetParent(buttonSpawnPoint);
            currentOpenButton.GetComponent<Button>().onClick.AddListener(bagController.OpenBag);
            currentOpenButton.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f); // ปรับขนาดให้เล็กลง

            currentCloseButton = Instantiate(closeButtonPrefab, buttonSpawnPoint.position, Quaternion.identity);
            currentCloseButton.transform.SetParent(buttonSpawnPoint);
            currentCloseButton.GetComponent<Button>().onClick.AddListener(bagController.CloseBag);
            currentCloseButton.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f); // ปรับขนาดให้เล็กลง

            // ปิดการทำงานของปุ่มทั้งสองในตอนแรก
            currentOpenButton.SetActive(false);
            currentCloseButton.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // แสดงปุ่ม Open เมื่อเข้าใกล้
            if (currentOpenButton != null)
            {
                currentOpenButton.SetActive(true);
                currentOpenButton.transform.localPosition = Vector3.zero; // รีเซ็ตตำแหน่งให้ตรงกับจุด Spawn Point
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // ซ่อนปุ่มทั้งหมดเมื่อออกไป
            if (currentOpenButton != null) currentOpenButton.SetActive(false);
            if (currentCloseButton != null) currentCloseButton.SetActive(false);
        }
    }
}