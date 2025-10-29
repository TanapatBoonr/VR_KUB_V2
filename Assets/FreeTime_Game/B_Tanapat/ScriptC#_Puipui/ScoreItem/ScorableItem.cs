using UnityEngine;

public class ScorableItem : MonoBehaviour
{
    // กำหนดชื่อที่ชัดเจนของไอเทมสำหรับแสดงใน Scoreboard
    [Tooltip("ชื่อไอเทมที่จะใช้ในการแสดงผลคะแนน เช่น 'Red_Tag' หรือ 'Medical Bandage A'")]
    public string ItemDisplayName; 

    [Tooltip("เป็นไอเทมที่ถูกต้องและต้องหยิบหรือไม่")]
    public bool IsCorrectItem = true;
    
    // กำหนดคะแนนที่จะได้รับ
    [Tooltip("คะแนนที่จะได้รับเมื่อหยิบไอเทมนี้ใส่กระเป๋า")]
    public int ScoreValue = 10;
}