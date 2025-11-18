using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SimpleSceneButtons : MonoBehaviour
{
    [Header("Optional: ผูกปุ่มจาก Inspector (ถ้าไม่ผูก ใช้ OnClick ของ Button เรียกเมธอดแทนได้)")]
    [SerializeField] private Button goToMenuButton;
    [SerializeField] private Button restartButton;

    [Header("Main Menu Settings")]
    [Tooltip("ใส่ชื่อ Scene ของหน้าเมนูหลัก (ต้องเพิ่มใน Build Settings)")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [Tooltip("ถ้าอยากใช้ Build Index แทนชื่อ scene ให้ติ๊กอันนี้")]
    [SerializeField] private bool useBuildIndexForMenu = false;
    [Tooltip("Build Index ของหน้าเมนู (ใช้เมื่อ useBuildIndexForMenu = true)")]
    [SerializeField] private int mainMenuBuildIndex = 0;

    private void Start()
    {
        // ผูกปุ่มที่ลากมา (ถ้าลาก)
        if (goToMenuButton != null)
            goToMenuButton.onClick.AddListener(GoToMainMenu);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartScene);
    }

    /// <summary>
    /// เรียกจากปุ่ม: ไปหน้า Main Menu
    /// </summary>
    public void GoToMainMenu()
    {
        if (useBuildIndexForMenu)
        {
            // โหลดด้วย Build Index
            if (mainMenuBuildIndex >= 0 && mainMenuBuildIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(mainMenuBuildIndex);
            }
            else
            {
                Debug.LogError($"[SimpleSceneButtons] Main menu build index {mainMenuBuildIndex} ไม่ถูกต้อง/ไม่อยู่ใน Build Settings.");
            }
        }
        else
        {
            // โหลดด้วยชื่อ Scene
            if (!string.IsNullOrEmpty(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
            else
            {
                Debug.LogError("[SimpleSceneButtons] ยังไม่ได้ตั้งชื่อ Main Menu Scene Name.");
            }
        }
    }

    /// <summary>
    /// เรียกจากปุ่ม: เริ่มซีนปัจจุบันใหม่ (Restart)
    /// </summary>
    public void RestartScene()
    {
        var current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }
}
