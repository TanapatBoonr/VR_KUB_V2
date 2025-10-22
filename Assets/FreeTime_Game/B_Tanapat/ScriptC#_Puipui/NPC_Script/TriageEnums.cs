using UnityEngine;

// หมายเหตุ: ไฟล์นี้ไม่ต้องสืบทอดจาก MonoBehaviour
// และไม่ต้องมี class ใดๆ ห่อหุ้ม ถ้าต้องการให้ TriageColor เป็น Global Enum
public enum TriageColor
{
    Green,  // Minor / Walking Wounded
    Yellow, // Delayed / Serious
    Red,    // Immediate / Life Threatening
    Black   // Deceased / Morgue
}