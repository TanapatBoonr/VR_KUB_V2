using UnityEngine;

[DisallowMultipleComponent]
public class PlayerFootstepSFX : MonoBehaviour
{
    [Header("Target to track (XR Origin or Camera Offset)")]
    public Transform target;                       // วัตถุที่ตำแหน่งขยับเวลาเดิน (เว้นว่าง = ตัวเอง)

    [Header("Step logic")]
    public float minMoveSpeed = 0.05f;             // เดินช้ากว่านี้ถือว่า "หยุด"
    public float stepDistance = 1.6f;              // ระยะต่อหนึ่งก้าว (เมตร)
    public bool  horizontalOnly = true;            // คิดเฉพาะระนาบ XZ
    public float teleportSkipDistance = 2.0f;      // ระยะเฟรมเดียวเกินค่านี้ = เทเลพอร์ต (ไม่นับเป็นก้าว)

    [Header("Audio")]
    public AudioSource audioSource;                // ปล่อยว่าง เดี๋ยวสร้างให้
    public AudioClip[] clips;                      // ใส่หลายคลิป จะสุ่มเลือก
    [Range(0f,1f)] public float volume = 1f;
    public float pitchMin = 0.95f;
    public float pitchMax = 1.05f;

    [Header("Stop behavior")]
    public bool stopInstantOnIdle = true;          // หยุดเดินแล้วตัดเสียงทันที

    Vector3 _lastPos;
    float   _accumDist;

    void Reset()
    {
        minMoveSpeed = 0.05f;
        stepDistance = 1.6f;
        horizontalOnly = true;
        teleportSkipDistance = 2.0f;
        volume = 0.85f;
        pitchMin = 0.96f;
        pitchMax = 1.04f;
        stopInstantOnIdle = true;
    }

    void Awake()
    {
        if (target == null) target = transform;

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake  = false;
            audioSource.loop         = false;   // สำคัญ: ไม่ loop
            audioSource.spatialBlend = 1f;      // 3D
        }
        else
        {
            audioSource.loop = false;
        }
    }

    void OnEnable()
    {
        if (target == null) target = transform;
        _lastPos = target.position;
        _accumDist = 0f;
        HardStop(); // กันเสียงค้างตอน Enable
    }

    void OnDisable()
    {
        HardStop();
    }

    void Update()
    {
        if (target == null) return;

        // คำนวณระยะเฟรมนี้
        Vector3 cur = target.position;
        Vector3 a = _lastPos, b = cur;
        if (horizontalOnly) { a.y = 0f; b.y = 0f; }

        float frameDist = Vector3.Distance(a, b);
        float speed = frameDist / Mathf.Max(Time.deltaTime, 0.0001f);

        // เทเลพอร์ต/สแน็ป: ไม่คิดเป็นก้าว และตัดเสียงเลย
        if (frameDist > teleportSkipDistance)
        {
            _lastPos = cur;
            if (stopInstantOnIdle) HardStop();
            return;
        }

        if (speed >= minMoveSpeed)
        {
            _accumDist += frameDist;

            if (_accumDist >= stepDistance)
            {
                PlayOneStep();
                _accumDist = 0f;
            }
        }
        else
        {
            _accumDist = 0f;

            // หยุดเดิน → ตัดเสียงทันที
            if (stopInstantOnIdle && audioSource.isPlaying)
                HardStop();
        }

        _lastPos = cur;
    }

    void PlayOneStep()
    {
        if (clips == null || clips.Length == 0 || audioSource == null) return;

        // สุ่มคลิป + pitch แล้ว "เล่นแบบกำหนด clip" (จะสามารถ Stop ได้ทันที)
        var clip = clips[Random.Range(0, clips.Length)];
        audioSource.clip = clip;
        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.volume = volume;
        audioSource.Stop();   // กันเคสคลิปเดิมยังไม่จบ
        audioSource.Play();
    }

    void HardStop()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();    // ตัดเงียบเดี๋ยวนั้น
    }
}
