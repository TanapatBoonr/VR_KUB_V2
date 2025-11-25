using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
public class XRPickupSFX : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip grabClip;     // เสียงตอนหยิบ
    public AudioClip dropClip;     // เสียงตอนปล่อย
    public AudioClip hoverClip;    // (ไม่บังคับ) เสียงตอนโฮเวอร์เข้าไอเท็ม

    [Header("Volume / Pitch")]
    [Range(0f, 1f)] public float volume = 0.8f;
    [Tooltip("สุ่ม pitch เล็กน้อยให้ฟังไม่ซ้ำ")]
    public Vector2 pitchRandomRange = new Vector2(0.95f, 1.05f);

    [Header("3D Sound")]
    [Tooltip("ระยะ Max ที่ได้ยิน (ถ้าใช้ 3D)")]
    public float maxDistance = 10f;
    [Tooltip("ค่ามาก = เสียงตกเร็วเมื่อไกล")]
    public float rolloff = 1f; // ใช้กับ Custom rolloff

    [Header("Cooldown (กันสแปมเสียง)")]
    public float minInterval = 0.05f;

    XRGrabInteractable grab;
    AudioSource src;
    float lastPlayTime = -999f;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();

        // เตรียม AudioSource 3D แบบ one-shot reuse
        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 1f;           // 3D
        src.rolloffMode = AudioRolloffMode.Custom; // ใช้ AnimationCurve เองให้คุมง่าย
        src.maxDistance = maxDistance;

        // กำหนด rolloff curve (จางลงตามระยะ)
        var curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(Mathf.Max(1f, maxDistance * 0.5f), Mathf.Clamp01(1f / (1f + rolloff)));
        curve.AddKey(maxDistance, 0f);
        src.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);

        // ผูกอีเวนต์
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnDrop);
        grab.hoverEntered.AddListener(OnHover);
    }

    void OnDestroy()
    {
        if (grab != null)
        {
            grab.selectEntered.RemoveListener(OnGrab);
            grab.selectExited.RemoveListener(OnDrop);
            grab.hoverEntered.RemoveListener(OnHover);
        }
    }

    void OnGrab(SelectEnterEventArgs args) => Play(grabClip);
    void OnDrop(SelectExitEventArgs args)   => Play(dropClip);
    void OnHover(HoverEnterEventArgs args)  => Play(hoverClip);

    void Play(AudioClip clip)
    {
        if (!clip) return;
        if (Time.time - lastPlayTime < minInterval) return;

        lastPlayTime = Time.time;
        src.pitch = Random.Range(pitchRandomRange.x, pitchRandomRange.y);
        src.PlayOneShot(clip, volume);
    }
}
