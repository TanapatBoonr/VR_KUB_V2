using UnityEngine;

public class BagController : MonoBehaviour
{
    public GameObject topBagpack;
    public GameObject socketsParent;

    public void OpenBag()
    {
        topBagpack.SetActive(false);
        socketsParent.SetActive(true);

        // ซ่อนปุ่ม Open และแสดงปุ่ม Close
        GameObject openBtn = GetButtonFromChildren("OpenButton(Clone)");
        GameObject closeBtn = GetButtonFromChildren("CloseButton(Clone)");

        if (openBtn != null) openBtn.SetActive(false);
        if (closeBtn != null) closeBtn.SetActive(true);
    }

    public void CloseBag()
    {
        topBagpack.SetActive(true);
        socketsParent.SetActive(false);

        // ซ่อนปุ่ม Close และแสดงปุ่ม Open
        GameObject openBtn = GetButtonFromChildren("OpenButton(Clone)");
        GameObject closeBtn = GetButtonFromChildren("CloseButton(Clone)");

        if (openBtn != null) openBtn.SetActive(true);
        if (closeBtn != null) closeBtn.SetActive(false);
    }

    // ฟังก์ชันช่วยสำหรับค้นหาปุ่มลูก
    private GameObject GetButtonFromChildren(string name)
    {
        Transform child = transform.Find(name);
        if (child != null)
        {
            return child.gameObject;
        }
        return null;
    }
}