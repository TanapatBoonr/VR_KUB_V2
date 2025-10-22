using UnityEngine;

public class UIFollowPlayer : MonoBehaviour
{
    private Transform mainCameraTransform;

    void Start()
    {
        mainCameraTransform = Camera.main.transform;
    }

    void Update()
    {
        if (mainCameraTransform != null)
        {
            Vector3 directionToCamera = mainCameraTransform.position - transform.position;
            Quaternion lookRotation = Quaternion.LookRotation(directionToCamera);
            transform.rotation = lookRotation * Quaternion.Euler(0, 180, 0);
        }
    }
}