using UnityEngine;
using UnityEngine.SceneManagement; // ต้องเพิ่มไลบรารีนี้เพื่อจัดการ Scene

public class RestartGame : MonoBehaviour
{
    // ฟังก์ชันนี้จะถูกเรียกเมื่อมี Collider อื่นเข้ามาในพื้นที่ Trigger
    private void OnTriggerEnter(Collider other)
    {
        // ตรวจสอบว่า Collider ที่เข้ามามี Tag เป็น "Player"
        if (other.CompareTag("Player"))
        {
            // รับชื่อ Scene ปัจจุบัน
            string currentSceneName = SceneManager.GetActiveScene().name;

            // โหลด Scene นั้นซ้ำอีกครั้งเพื่อรีสตาร์ท
            SceneManager.LoadScene(currentSceneName);
        }
    }
}