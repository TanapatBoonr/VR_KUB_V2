using UnityEngine;

// คลาส Static ใช้สำหรับเก็บข้อมูลข้าม Scene โดยที่ไม่ถูกทำลาย
public static class TeleportData
{
    // ตัวแปรส่วนตัวสำหรับเก็บชื่อจุดหมายปลายทาง
    private static string _destinationPointName = "";

    /// <summary>
    /// บันทึกชื่อจุดหมายปลายทาง (GameObject name) ที่ผู้เล่นควรไปโผล่ใน Scene ถัดไป
    /// </summary>
    /// <param name="name">ชื่อของ GameObject จุดหมายปลายทางใน Scene ปลายทาง</param>
    public static void SetDestinationPointName(string name)
    {
        _destinationPointName = name;
        // ใช้การต่อ String แบบดั้งเดิม
        Debug.Log("TeleportData: Destination point set to '" + name + "'");
    }

    /// <summary>
    /// ดึงชื่อจุดหมายปลายทางที่ถูกบันทึกไว้
    /// </summary>
    /// <returns>ชื่อของ GameObject จุดหมายปลายทาง</returns>
    public static string GetDestinationPointName()
    {
        return _destinationPointName;
    }
}