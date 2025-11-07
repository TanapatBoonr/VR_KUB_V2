using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
public class XRSocketSnapFix : MonoBehaviour
{
    public XRSocketInteractor socket;
    public Transform snapPoint;          // ใส่ SocketPoint
    public bool parentToSocket = false;  // true = ทำให้เป็นลูก socket หลังวาง

    void Reset()
    {
        socket = GetComponent<XRSocketInteractor>();
    }

    void OnEnable()
    {
        if (socket == null) socket = GetComponent<XRSocketInteractor>();
        if (socket != null)
        {
            socket.selectEntered.AddListener(OnSelected);
            socket.selectExited.AddListener(OnDeselected);
        }
    }

    void OnDisable()
    {
        if (socket != null)
        {
            socket.selectEntered.RemoveListener(OnSelected);
            socket.selectExited.RemoveListener(OnDeselected);
        }
    }

    void OnSelected(SelectEnterEventArgs args)
    {
        var tr = args.interactableObject.transform;

        // ถ้าวัตถุมี AttachTransform ของตัวเอง ให้ใช้มันเป็นตัวอ้างอิง
        Transform itemAttach = null;
        if (args.interactableObject is XRGrabInteractable grab && grab.attachTransform != null)
            itemAttach = grab.attachTransform;

        // จับล็อกตำแหน่ง/หมุนให้ตรง snapPoint
        if (snapPoint != null)
        {
            if (itemAttach != null)
            {
                // ขยับวัตถุทั้งชิ้นให้ itemAttach ทับ snapPoint
                var delta = snapPoint.localToWorldMatrix * itemAttach.worldToLocalMatrix;
                tr.SetPositionAndRotation(delta.MultiplyPoint3x4(tr.position), delta.rotation * tr.rotation);
            }
            tr.SetPositionAndRotation(snapPoint.position, snapPoint.rotation);
        }

        // ปิดฟิสิกส์ลอย/เด้ง
        var rb = tr.GetComponent<Rigidbody>();
        if (rb) { rb.isKinematic = true; rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

        if (parentToSocket && snapPoint != null)
            tr.SetParent(snapPoint, true);
    }

    void OnDeselected(SelectExitEventArgs args)
    {
        var tr = args.interactableObject.transform;
        var rb = tr.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = false;

        if (parentToSocket && tr.parent == snapPoint)
            tr.SetParent(null, true);
    }
}
