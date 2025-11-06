using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class VRBagPackSpawner : MonoBehaviour
{
    [System.Serializable]
    public class ItemSlot
    {
        public string slotName;
        public XRSocketInteractor socket;  // ช่องเก็บของแต่ละช่อง
        public GameObject itemPrefab;      // Prefab ของไอเทมที่จะ Spawn
    }

    [Header("ตั้งค่าไอเทมในกระเป๋า")]
    public List<ItemSlot> itemSlots = new List<ItemSlot>();

    [Header("Spawn เมื่อเริ่มเกมหรือไม่")]
    public bool spawnOnStart = true;

    private void Start()
    {
        if (spawnOnStart)
            SpawnAllItems();

        // Subscribe event เมื่อหยิบของออก
        foreach (var slot in itemSlots)
        {
            if (slot.socket != null)
                slot.socket.selectExited.AddListener(OnItemRemoved);
        }
    }

    private void OnDestroy()
    {
        // ป้องกัน memory leak
        foreach (var slot in itemSlots)
        {
            if (slot.socket != null)
                slot.socket.selectExited.RemoveListener(OnItemRemoved);
        }
    }

    // Spawn ของทุกช่อง
    public void SpawnAllItems()
    {
        foreach (var slot in itemSlots)
        {
            SpawnItemInSlot(slot);
        }
    }

    // ฟังก์ชัน Spawn ไอเทมในแต่ละช่อง
    private void SpawnItemInSlot(ItemSlot slot)
    {
        if (slot.socket == null || slot.itemPrefab == null)
        {
            Debug.LogWarning($"Slot '{slot.slotName}' ไม่มี Socket หรือ Prefab");
            return;
        }

        // ถ้ามีของใน socket แล้ว ไม่ต้องสร้างซ้ำ
        if (slot.socket.hasSelection)
            return;

        // สร้างไอเทมใหม่
        GameObject newItem = Instantiate(slot.itemPrefab);
        newItem.name = slot.itemPrefab.name; // ชื่อให้อ่านง่ายใน Hierarchy

        // ผูกไอเทมกับ socket แบบ manual
        IXRSelectInteractable interactable = newItem.GetComponent<IXRSelectInteractable>();
        if (interactable != null)
        {
            slot.socket.interactionManager.SelectEnter(slot.socket, interactable);
            Debug.Log($"Spawn {newItem.name} เข้า {slot.slotName}");
        }
        else
        {
            Debug.LogWarning($"{newItem.name} ไม่มี XR Grab Interactable");
        }
    }

    // เมื่อของถูกหยิบออกจาก socket → สั่ง Spawn ใหม่อัตโนมัติ
    private void OnItemRemoved(SelectExitEventArgs args)
    {
        // หาว่า socket ไหนที่ของถูกหยิบออกไป
        XRSocketInteractor thisSocket = args.interactorObject as XRSocketInteractor;
        if (thisSocket == null) return;

        // หาช่องที่ตรงกับ socket นั้น
        ItemSlot targetSlot = itemSlots.Find(s => s.socket == thisSocket);
        if (targetSlot != null)
        {
            // Spawn ใหม่ทันที
            Invoke(nameof(RefillSlot), 0.5f); // รอ 0.5 วินาทีเพื่อให้ของเก่าออกจากระบบก่อน
        }
    }

    private void RefillSlot()
    {
        foreach (var slot in itemSlots)
        {
            if (!slot.socket.hasSelection)
                SpawnItemInSlot(slot);
        }
    }
}
