using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartButton : MonoBehaviour
{
    [Header("Restart Options")]
    [Tooltip("หน่วงเวลาก่อนรีสตาร์ท (วินาที)")]
    public float delaySeconds = 0f;

    [Tooltip("คืนค่า Time.timeScale ให้เป็น 1 ก่อนรีสตาร์ท (กันกรณีเกมถูก Pause)")]
    public bool resetTimeScaleToOne = true;

    [Tooltip("ใช้การโหลดแบบ Async (นุ่มนวลกว่า แต่ซับซ้อนขึ้นเล็กน้อย)")]
    public bool useAsyncLoad = false;

    [Header("Optional: Click SFX")]
    [Tooltip("ถ้ามีใส่ AudioSource ไว้ จะเล่นเสียงเมื่อกด")]
    public AudioSource audioSource;
    public AudioClip clickSfx;

    bool _restarting = false;

    // ====== เรียกจากปุ่ม UI ได้เลย ======
    public void RestartNow()
    {
        if (_restarting) return;
        _restarting = true;

        if (audioSource && clickSfx)
        {
            audioSource.PlayOneShot(clickSfx);
        }

        if (resetTimeScaleToOne)
            Time.timeScale = 1f;

        if (delaySeconds > 0f)
            Invoke(nameof(DoRestart), delaySeconds);
        else
            DoRestart();
    }

    // ====== เรียกจาก XR Push Button / XR Simple Interactable ======
    // ตัวอย่าง: ผูก UnityEvent ของ XR Base Interactable มาที่เมธอดนี้
    public void OnXRPressed()
    {
        RestartNow();
    }

    void DoRestart()
    {
        var active = SceneManager.GetActiveScene().buildIndex;

        if (!useAsyncLoad)
        {
            SceneManager.LoadScene(active);
            return;
        }

        // Async load (ไม่จำเป็นต้องแสดง progress ก็ได้)
        var op = SceneManager.LoadSceneAsync(active);
        op.allowSceneActivation = true;
    }
}