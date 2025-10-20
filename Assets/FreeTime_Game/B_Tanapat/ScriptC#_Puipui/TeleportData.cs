using UnityEngine;

// Static Class สำหรับเก็บข้อมูลที่ต้องการส่งผ่านระหว่าง Scene
public static class TeleportData
{
    private static string _destinationPointName = "";

    // บันทึกชื่อจุดหมายก่อนเปลี่ยน Scene
    public static void SetDestinationPointName(string name)
    {
        _destinationPointName = name;
    }

    // ดึงชื่อจุดหมายเมื่อ Scene ใหม่โหลดเสร็จแล้ว
    public static string GetDestinationPointName()
    {
        return _destinationPointName;
    }
}