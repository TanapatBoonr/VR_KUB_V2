using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;
using System.Linq;

public class PlayerInventory : MonoBehaviour
{
    // ตัวแปรสำหรับระบุ GameObject แม่ของ Socket ทั้งหมด
    [Tooltip("ลาก GameObject ที่เป็น Parent ของ XRSocketInteractor ทั้งหมดในกระเป๋ามาใส่")]
    public GameObject socketsParent; 

    // ฟังก์ชันสำหรับดึงไอเทมทั้งหมดที่ถูกยึดอยู่ใน Socket ภายในกระเป๋า
    public List<GameObject> GetAllItemsInSockets()
    {
        List<GameObject> items = new List<GameObject>();

        if (socketsParent == null)
        {
            Debug.LogError("Sockets Parent is not assigned on PlayerInventory script.");
            return items;
        }

        // ค้นหา XRSocketInteractor ทั้งหมดที่อยู่ใต้ Parent
        // การใช้ GetComponentsInChildren จะหา Component ที่แนบกับ GameObject ลูกทั้งหมด
        XRSocketInteractor[] sockets = socketsParent.GetComponentsInChildren<XRSocketInteractor>();

        foreach (XRSocketInteractor socket in sockets)
        {
            // *** แก้ไขคำเตือน CS0618: ใช้ GetOldestInteractableSelected() แทน selectTarget ***
            
            // GetOldestInteractableSelected() จะคืนค่า Interactable ที่ถูกเลือกอยู่ 
            // ซึ่งเป็นฟังก์ชันที่ถูกแนะนำให้ใช้แทน selectTarget
            IXRSelectInteractable interactable = socket.GetOldestInteractableSelected();

            if (interactable != null) 
            {
                // ดึง GameObject ที่ถูกยึดมา
                GameObject item = interactable.transform.gameObject;

                if (item != null)
                {
                    items.Add(item);
                }
            }
        }
        
        return items;
    }
}