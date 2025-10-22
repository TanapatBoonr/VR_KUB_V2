using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;
using System.Linq; 
using System.Collections; 

public class CarryOverManager : MonoBehaviour
{
    public static CarryOverManager Instance { get; private set; }

    [Header("Carry Over Configuration")]
    public GameObject playerBag; 
    
    [HideInInspector] 
    public string destinationSocketName = ""; 
    
    // NEW: สำหรับการเก็บข้อมูลว่า Item เดิมอยู่ใต้ Socket ใด (สำคัญ)
    private Dictionary<GameObject, string> carriedItemSocketNames = new Dictionary<GameObject, string>();

    private readonly List<string> validBeltSockets = new List<string> { "Cube Socket L", "Cube Socket R" };


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PrepareForSceneChange()
    {
        if (playerBag == null)
        {
            Debug.LogError("Player Bag is not assigned to CarryOverManager. Cannot carry items.");
            return;
        }

        XRGrabInteractable grabInteractable = playerBag.GetComponent<XRGrabInteractable>();
        carriedItemSocketNames.Clear(); // เคลียร์สถานะก่อนเริ่มใหม่
        
        // 1. **ตรวจสอบ Socket บนเข็มขัดและบันทึกชื่อ**
        XRBaseInteractor currentInteractor = grabInteractable.selectingInteractor as XRBaseInteractor;
        
        if (currentInteractor != null && validBeltSockets.Contains(currentInteractor.name))
        {
            destinationSocketName = currentInteractor.name;
            Debug.Log("Bag attached to: " + destinationSocketName);
        }
        else
        {
            destinationSocketName = validBeltSockets.First(); 
            Debug.LogWarning("Bag is not attached to a belt socket. Defaulting to: " + destinationSocketName);
        }

        // 2. **หยุดการปฏิสัมพันธ์ (สำคัญที่สุด)**
        if (grabInteractable.isSelected)
        {
            IXRSelectInteractor firstInteractor = grabInteractable.interactorsSelecting.FirstOrDefault();
            if (firstInteractor != null && firstInteractor is XRBaseInteractor baseInteractor)
            {
                XRInteractionManager interactionManager = baseInteractor.interactionManager;
                if (interactionManager != null)
                {
                    // ยกเลิกการเลือกกระเป๋า (ออกจากมือหรือเข็มขัดชั่วคราว)
                    interactionManager.SelectExit(baseInteractor, grabInteractable); 
                }
            }
        }
        
        // 3. **จัดการสิ่งของในกระเป๋า (Reparenting)**
        XRSocketInteractor[] socketsInBag = playerBag.GetComponentsInChildren<XRSocketInteractor>();
        
        foreach (XRSocketInteractor socket in socketsInBag)
        {
            if (socket.selectTarget != null) 
            {
                GameObject carriedItem = socket.selectTarget.transform.gameObject;
                
                // *** KEY FIX: บันทึกชื่อ Socket ของ Item ก่อนถอดออก ***
                carriedItemSocketNames.Add(carriedItem, socket.name);

                // *** บังคับถอด Item ออกจาก Socket ก่อนย้าย (เคลียร์สถานะ) ***
                socket.interactionManager.SelectExit(socket, socket.selectTarget);
                
                // *** KEY FIX 2: Reparent Item to Manager เพื่อป้องกันการหลุดหาย ***
                carriedItem.transform.SetParent(transform);
                DontDestroyOnLoad(carriedItem);
                
                Debug.Log("Carried item " + carriedItem.name + " is now a child of CarryOverManager.");
            }
        }
        
        // 4. **Reparent กระเป๋า**
        // ย้ายกระเป๋าไปเป็น Child ของ Manager ชั่วคราว
        playerBag.transform.SetParent(transform); 
        DontDestroyOnLoad(playerBag);
        
        // ตั้งค่า Transform เป็น Zero
        playerBag.transform.position = Vector3.zero;
        playerBag.transform.rotation = Quaternion.identity;
    }

    public void PlaceCarriedItemsInNewScene(Transform playerRig)
    {
        if (playerBag == null) return;
        
        // 1. ค้นหา Socket ปลายทาง
        string socketToFind = destinationSocketName;
        Transform destinationSocket = FindDeepChild(playerRig.gameObject, socketToFind);

        if (destinationSocket != null)
        {
            // 2. **กำหนด Attach Point**
            Transform attachPoint = destinationSocket; 
            XRSocketInteractor beltSocketInteractor = null;
            
            if (destinationSocket.TryGetComponent(out beltSocketInteractor))
            {
                // *** KEY FIX 3: ให้ความสำคัญกับ Attach Transform เป็นอันดับแรก ***
                if (beltSocketInteractor.attachTransform != null)
                {
                    attachPoint = beltSocketInteractor.attachTransform;
                }
            }
            
            // 3. **วางกระเป๋าลงบน Attach Point**
            playerBag.transform.SetParent(attachPoint);
            playerBag.transform.localPosition = Vector3.zero; // ใช้ Transform ของ Attach Point/Socket
            playerBag.transform.localRotation = Quaternion.identity;
            
            // 4. **บังคับให้ Socket 'จับ' กระเป๋า**
            if (beltSocketInteractor != null && playerBag.TryGetComponent(out XRGrabInteractable bagInteractable))
            {
                // บังคับให้ Socket เลือกกระเป๋า (จำลองการวาง)
                beltSocketInteractor.interactionManager.SelectEnter(beltSocketInteractor, bagInteractable);
            }
            
            Debug.Log("Bag successfully placed onto " + destinationSocketName + " using " + attachPoint.name);

            // 5. **ย้าย Item กลับเข้า Socket ในกระเป๋า**
            // ค้นหา Socket ทั้งหมดในกระเป๋า (ซึ่งตอนนี้อยู่ใต้ Rig ใหม่แล้ว)
            XRSocketInteractor[] socketsInBag = playerBag.GetComponentsInChildren<XRSocketInteractor>();

            // ลูปผ่าน Item ที่ถูก Reparent ไว้ใต้ Manager
            foreach (Transform carriedItemTransform in transform)
            {
                if (carriedItemTransform.TryGetComponent(out XRGrabInteractable itemInteractable))
                {
                    // 5.1. ดึงชื่อ Socket เดิมที่ Item นี้เคยอยู่
                    carriedItemSocketNames.TryGetValue(carriedItemTransform.gameObject, out string originalSocketName);

                    // 5.2. ค้นหา Socket ตัวที่ถูกต้องในกระเป๋า
                    XRSocketInteractor targetSocket = socketsInBag.FirstOrDefault(s => s.name == originalSocketName);
                    
                    if (targetSocket != null)
                    {
                        // 5.3. บังคับให้ Socket ในกระเป๋า 'จับ' Item
                        targetSocket.interactionManager.SelectEnter(targetSocket, itemInteractable);
                        Debug.Log("Restored item: " + carriedItemTransform.name + " into " + targetSocket.name);
                    }
                    else
                    {
                        Debug.LogWarning("Target socket (" + originalSocketName + ") not found in bag or already filled. Placing item near bag.");
                        // ถ้าหา Socket ไม่เจอ ให้วางไว้ใกล้ๆ กระเป๋าแทน
                        carriedItemTransform.SetParent(playerBag.transform); // เป็น Child ของกระเป๋า
                        carriedItemTransform.localPosition = new Vector3(0, 0.1f, 0); // วางไว้เหนือกระเป๋า
                    }
                }
            }

        }
        else
        {
            Debug.LogError("Destination Socket '" + socketToFind + "' not found under Player Rig! Placing bag near player.");
            playerBag.transform.position = playerRig.position + playerRig.forward * 0.5f;
            playerBag.transform.SetParent(playerRig);
        }
    }

    private Transform FindDeepChild(GameObject parent, string name)
    {
        // ... (โค้ด FindDeepChild เดิม) ...
        Queue<Transform> queue = new Queue<Transform>();
        queue.Enqueue(parent.transform);
        while (queue.Count > 0)
        {
            var child = queue.Dequeue();
            if (child.name == name)
                return child;
            foreach (Transform t in child)
                queue.Enqueue(t);
        }
        return null;
    }
}