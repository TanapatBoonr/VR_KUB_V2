using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    [Tooltip("ใส่ชื่อ Scene ที่เป็นด่านแรกของเกม (เช่น TriageRoom)")]
    public string startGameSceneName = "TriageRoom";

    /// <summary>
    /// ฟังก์ชันสำหรับเรียกใช้เมื่อผู้เล่นกดปุ่ม 'Play'
    /// </summary>
    public void StartGame()
    {
        Debug.Log("MainMenuController: Loading Game Scene...");
        
        // ใช้ SceneManager เพื่อโหลด Scene ถัดไป
        // ต้องแน่ใจว่าได้เพิ่ม Scene ปลายทางใน Build Settings แล้ว
        SceneManager.LoadScene(startGameSceneName);
    }

    /// <summary>
    /// ฟังก์ชันสำหรับเรียกใช้เมื่อผู้เล่นกดปุ่ม 'Quit'
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("MainMenuController: Quitting Application.");
        
        // ตรวจสอบว่ากำลังทำงานอยู่ใน Unity Editor หรือ Build
        #if UNITY_EDITOR
            // ถ้าอยู่ใน Editor ให้หยุดการเล่น
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // ถ้าเป็น Build ให้ปิด Application
            Application.Quit();
        #endif
    }
}
