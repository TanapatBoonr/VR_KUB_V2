using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // ต้องเพิ่ม
using System.Linq;

// เพิ่ม Interfaces สำหรับการตรวจจับลำแสง (Raycast/Hover)
public class ScenarioButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("ชื่อของ Plane ที่จะถูกเลือกเมื่อกดปุ่ม (เช่น PlaneForSpawn_A)")]
    public string targetPlaneName;
    
    // ** NEW ** ตัวแปรสำหรับเก็บ GameObject ของ Plane ที่เกี่ยวข้อง
    private GameObject targetPlane; 
    
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        
        // 1. ค้นหา Plane ที่เกี่ยวข้องล่วงหน้า
        targetPlane = GameObject.Find(targetPlaneName);

        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
        else
        {
            Debug.LogError("ScenarioButton requires a Button component on the same GameObject.");
        }
    }
    
    // ----------------------------------------------------------------------
    // NEW: 1. เมื่อลำแสงเข้าสู่ปุ่ม (Hover Enter)
    // ----------------------------------------------------------------------
    public void OnPointerEnter(PointerEventData eventData)
    {
        // ตรวจสอบว่า Manager กำลังรอการเลือกอยู่ (คือยังไม่มี Plane ไหนถูกเลือก)
        if (ScenarioManager.Instance.IsScenarioSelected() == false)
        {
            // สั่งให้ ScenarioManager เปิดโหมด Preview สำหรับ Plane นี้
            ScenarioManager.Instance.PreviewScenario(targetPlaneName, true);
        }
    }

    // ----------------------------------------------------------------------
    // NEW: 2. เมื่อลำแสงออกจากปุ่ม (Hover Exit)
    // ----------------------------------------------------------------------
    public void OnPointerExit(PointerEventData eventData)
    {
         if (ScenarioManager.Instance.IsScenarioSelected() == false)
         {
             // สั่งให้ ScenarioManager ปิดโหมด Preview สำหรับ Plane นี้
             ScenarioManager.Instance.PreviewScenario(targetPlaneName, false);
         }
    }

    private void OnButtonClick()
    {
        if (ScenarioManager.Instance != null && !string.IsNullOrEmpty(targetPlaneName))
        {
            // ** 3. สำคัญ: เมื่อกดเลือกแล้ว ต้องมั่นใจว่าโหมด Preview ถูกปิด **
            ScenarioManager.Instance.PreviewScenario(targetPlaneName, true); // เปิดมันขึ้นมาอีกครั้งก่อนเลือก
            ScenarioManager.Instance.SelectScenario(targetPlaneName);
            
            // ปิด UI Panel หลังจากเลือกแล้ว
            // transform.parent.gameObject.SetActive(false); 
        }
    }
}