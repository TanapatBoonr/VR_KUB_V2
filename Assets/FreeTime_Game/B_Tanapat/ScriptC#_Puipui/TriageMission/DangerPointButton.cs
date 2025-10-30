using UnityEngine;
using UnityEngine.UI;
using TMPro;

// กำหนดให้ต้องมี Collider และ AudioSource อยู่บน GameObject เดียวกัน
[RequireComponent(typeof(Collider))] 
[RequireComponent(typeof(AudioSource))] 
public class DangerPointButton : MonoBehaviour
{
    [Header("--- UI & Audio Settings ---")]
    [Tooltip("GameObject ที่มีปุ่ม UI (เช่น Text UI ที่มี Button Component)")]
    public GameObject buttonUI; 
    [Tooltip("ข้อความที่แสดงบนปุ่ม (เช่น 'จุดเสี่ยงที่ 5: สารเคมีรั่วไหล')")]
    public string buttonName = "จุดอันตราย"; 
    [Tooltip("AudioClip ที่จะเล่นเมื่อผู้เล่นกดปุ่ม")]
    public AudioClip collectSound; 
    [Tooltip("สีปุ่มเมื่อยังไม่ถูกเก็บ")]
    public Color defaultColor = Color.yellow; 
    [Tooltip("สีปุ่มเมื่อถูกเก็บเรียบร้อยแล้ว")]
    public Color collectedColor = Color.green; 

    // Components ภายใน
    private AudioSource audioSource;
    private Button buttonComponent;
    private TextMeshProUGUI buttonText;
    private bool isCollected = false;
    private bool isActive = false; // สถานะการเปิดใช้งานโดย Manager

    void Start()
    {
        // 1. ตั้งค่า Components
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (buttonUI != null)
        {
            buttonComponent = buttonUI.GetComponent<Button>();
            buttonText = buttonUI.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonComponent != null)
            {
                // 2. เชื่อมต่อฟังก์ชันเมื่อกดปุ่ม
                buttonComponent.onClick.AddListener(OnPointClicked);
            }
            
            if (buttonText != null)
            {
                // 3. ตั้งชื่อปุ่ม
                buttonText.text = buttonName;
            }
            
            // 4. ซ่อนปุ่มไว้ก่อน
            buttonUI.SetActive(false);
        }
        else
        {
            Debug.LogError($"DangerPointButton on {gameObject.name} is missing Button UI reference.");
        }
    }
    
    // ฟังก์ชันที่เรียกโดย TriageMissionManager เพื่อเปิด/ปิดการทำงาน
    public void SetButtonActive(bool active)
    {
        isActive = active;
        // หากภารกิจจบแล้ว ให้ซ่อนปุ่ม UI ทันที
        if (!active && buttonUI != null)
        {
            buttonUI.SetActive(false);
        }
    }

    // เมื่อผู้เล่นเข้าสู่ Sphere Collider
    private void OnTriggerEnter(Collider other)
    {
        // ตรวจสอบว่าผู้เล่นเข้ามาใกล้ และภารกิจกำลังทำงานอยู่
        if (other.CompareTag("Player") && isActive && !isCollected)
        {
            if (buttonUI != null)
            {
                buttonUI.SetActive(true);
                UpdateColor(defaultColor); // แสดงสีเริ่มต้น
            }
        }
    }
    
    // เมื่อผู้เล่นออกนอก Sphere Collider
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // ซ่อนปุ่มเมื่อออกไป ยกเว้นถ้าถูกเก็บแล้ว (ซึ่งควรจะหายไปเองใน OnPointClicked)
            if (buttonUI != null && !isCollected)
            {
                buttonUI.SetActive(false);
            }
        }
    }

    // ฟังก์ชันที่ถูกเรียกเมื่อผู้เล่นกดปุ่ม UI
    private void OnPointClicked()
    {
        if (isCollected || !isActive) return;

        // 1. ตั้งค่าว่าถูกเก็บแล้ว
        isCollected = true;
        
        // 2. เล่นเสียง
        if (collectSound != null)
        {
            audioSource.PlayOneShot(collectSound);
        }
        
        // 3. เปลี่ยนสีปุ่มเป็นสีเขียว
        UpdateColor(collectedColor);
        
        // 4. แจ้ง Manager ว่าเก็บจุดเสี่ยงนี้แล้ว
        if (TriageMissionManager.Instance != null)
        {
            TriageMissionManager.Instance.ReportDangerPointCollected(gameObject.name);
        }
        
        // 5. ปิดการทำงานของปุ่ม
        buttonComponent.interactable = false;
        
        // 6. หน่วงเวลา 1 วินาทีแล้วซ่อนปุ่ม
        Invoke("HideButtonUI", 1.0f); 
    }
    
    private void UpdateColor(Color color)
    {
        // สมมติว่าปุ่มเป็น Image หรือมี Graphic Component ที่เราต้องการเปลี่ยนสี
        Graphic graphic = buttonComponent.GetComponent<Image>();
        if (graphic == null) graphic = buttonText; // ถ้าไม่มี Image ให้เปลี่ยนสี Text แทน
        
        if (graphic != null)
        {
            graphic.color = color;
        }
    }

    private void HideButtonUI()
    {
        if (buttonUI != null)
        {
            buttonUI.SetActive(false);
        }
    }
}
