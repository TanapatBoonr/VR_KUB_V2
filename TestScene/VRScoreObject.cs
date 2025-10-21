using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class VRScoreObject : MonoBehaviour
{
    [Header("Effect Settings")]
    public GameObject sparkleEffectPrefab;  // เอฟเฟกต์ระยิบระยับ
    public AudioClip scoreSound;            // เสียง "ติ๊ง"
    [Range(0f, 1f)] public float soundVolume = 0.7f;
    [Tooltip("ดีเลย์ก่อนเสียงเล่น (วินาที)")]
    public float soundDelay = 3f;

    private bool isPressed = false;
    private bool hasPlayedEffect = false;
    private XRSimpleInteractable interactable;
    private AudioSource audioSource;

    private void Awake()
    {
        // ✅ ตรวจและเพิ่ม XRSimpleInteractable หากไม่มี
        interactable = GetComponent<XRSimpleInteractable>();
        if (interactable == null)
            interactable = gameObject.AddComponent<XRSimpleInteractable>();

        interactable.activated.AddListener(OnActivated);

        // ✅ ตรวจและเพิ่ม AudioSource หากไม่มี
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // เสียงแบบ 3D ใน VR
    }

    private void OnDestroy()
    {
        if (interactable != null)
            interactable.activated.RemoveListener(OnActivated);
    }

    private void OnActivated(ActivateEventArgs args)
    {
        // 🧠 ตรวจว่ากด Object นี้จริงไหม
        if ((Object)args.interactableObject != interactable)
            return;


        if (isPressed) return;
        isPressed = true;

        // ✅ เพิ่มคะแนนใน GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore();
            Debug.Log($"✅ [{name}] ได้คะแนน! ตอนนี้: {GameManager.Instance.GetScore()}");
        }

        // ✨ แสดงเอฟเฟกต์ประกายทันที
        PlayEffect();

        // 🎵 หน่วงเสียง 3 วิก่อนเล่น
        if (scoreSound != null)
            StartCoroutine(PlaySoundAfterDelay(soundDelay));

        // 🔒 ปิด Collider ป้องกันกดซ้ำ
        DisableCollider();
    }

    private void PlayEffect()
    {
        if (hasPlayedEffect || sparkleEffectPrefab == null) return;

        hasPlayedEffect = true;

        GameObject effect = Instantiate(sparkleEffectPrefab, transform.position, Quaternion.identity);
        var ps = effect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Simulate(0, true, true);
            ps.Play(true);
        }
    }

    private IEnumerator PlaySoundAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        audioSource.PlayOneShot(scoreSound, soundVolume);
    }

    private void DisableCollider()
    {
        var col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;
    }
}
