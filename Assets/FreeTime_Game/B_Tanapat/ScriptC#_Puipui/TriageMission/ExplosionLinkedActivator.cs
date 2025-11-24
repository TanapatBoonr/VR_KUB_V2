using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// เปิด/อินสแตนซ์ GameObject เป็นชุด ๆ ให้ “ตรงจังหวะระเบิด” ของ ScoreV2_5
/// - ซิงก์เวลาอัตโนมัติกับ ScoreV2_5 (Explosion Countdown Seconds)
/// - เลือกลำดับ/ดีเลย์รายชิ้น เพื่อลดเฟรมดรอป
/// - รองรับทั้งวัตถุที่อยู่ในซีน (SetActive) และ Prefab (Instantiate)
/// - โชว์เวลาที่จะทริกเกอร์ให้ตรวจสอบได้ใน Inspector (Debug Preview)
/// หมายเหตุ: สคริปต์นี้พยายามอ่าน “เวลาเริ่มภารกิจ” จาก ScoreV2_5 ด้วยรีเฟล็กชัน
/// ถ้าหาไม่ได้ ให้ใช้ปุ่ม Manual Start หรือเรียก StartNow() เองได้
/// </summary>
public class ExplosionLinkedActivator : MonoBehaviour
{
    [Header("Link to Score")]
    [Tooltip("ถ้าเว้นว่าง จะพยายามหา ScoreV2_5.Instance ให้อัตโนมัติ")]
    public ScoreV2_5 score;

    [Tooltip("โหมดนับถอยหลังของระเบิดในโปรเจ็กต์ตอนนี้เป็นแบบไหน ?\n" +
             "- Immediate: นับ Explosion Countdown ทันทีที่กดเลือกโซน -> ระเบิดเมื่อครบ Countdown\n" +
             "- Legacy: Explosion เกิดเมื่อครบ Mission Duration ทั้งหมด")]
    public CountdownMode countdownMode = CountdownMode.Immediate;

    public enum CountdownMode { Immediate, Legacy }

    [Header("Global Delay")]
    [Tooltip("หน่วงเวลารวมก่อนเริ่มเปิดเอฟเฟ็กต์ทั้งหมด (วินาที)\nเช่น 0.2 = เปิดหลังระเบิด 0.2 วิ")]
    public float extraDelayAfterExplosion = 0.2f;

    [System.Serializable]
    public class EffectItem
    {
        [Tooltip("วัตถุ/พรีแฟ็บที่จะเปิด (ถ้าวางในซีน = SetActive(true), ถ้าเป็น Prefab = Instantiate)")]
        public GameObject effect;

        [Tooltip("กรณีเป็น Prefab ให้เกิดที่จุดไหน (เว้นว่าง = ที่ Transform นี้)")]
        public Transform spawnPoint;

        [Tooltip("ออฟเซ็ต ณ จุดเกิด (ใช้กับกรณี Instantiate)")]
        public Vector3 spawnLocalOffset;

        [Tooltip("ดีเลย์รายชิ้น (วินาที) นับถัดจากจังหวะระเบิด + extraDelay")]
        public float delayFromExplosion = 0f;

        [Tooltip("ถ้าเป็นวัตถุในซีน ให้สั่ง SetActive(true) แทนการ Instantiate")]
        public bool setActiveIfInScene = true;

        [Tooltip("ถ้าเป็น Prefab ให้ Instantiate")]
        public bool instantiateIfPrefab = true;
    }

    [Header("Effects (เรียงลำดับตามดีเลย์)")]
    public EffectItem[] effects;

    // ================= Debug / Preview =================
    [Header("Debug Preview (อ่านอย่างเดียว)")]
    [Tooltip("เวลาที่จะทริกเกอร์เปิดเอฟเฟ็กต์ ตามการอ่านค่าจาก ScoreV2_5 (วินาที, timeSinceStartup)")]
    public float previewTriggerAt = -1f;

    [Tooltip("เวลาที่เหลือจากตอนนี้จนถึงทริกเกอร์ (วินาที)")]
    public float previewRemaining = -1f;

    [Tooltip("เวลา Countdown ของระเบิดจาก ScoreV2_5 (เพื่อเทียบใน Inspector)")]
    public float previewExplosionCountdownFromScore = -1f;

    Coroutine _runner;
    bool _started;

    void Start()
    {
        if (score == null) score = ScoreV2_5.Instance;
        // เริ่มโหมดลิงก์อัตโนมัติ
        _runner = StartCoroutine(Co_WaitAndFire());
    }

    void Update()
    {
        // อัปเดตค่าดีบักในอินสเปกเตอร์ (ไม่กระทบเกมเพลย์)
        UpdatePreviewTimes();
    }

    /// <summary>
    /// ใช้เมื่อไม่อยาก/ไม่สามารถลิงก์กับ Score ได้ เรียกเองเพื่อเริ่มจับเวลาตอนนี้เลย
    /// </summary>
    public void StartNow(float countdownSeconds)
    {
        if (_runner != null) StopCoroutine(_runner);
        _runner = StartCoroutine(Co_StartNow(countdownSeconds));
    }

    IEnumerator Co_StartNow(float seconds)
    {
        if (_started) yield break;
        _started = true;

        yield return new WaitForSeconds(Mathf.Max(0f, seconds + extraDelayAfterExplosion));
        yield return StartCoroutine(Co_FireEffects());
    }

    IEnumerator Co_WaitAndFire()
    {
        if (_started) yield break;
        _started = true;

        // รอจนหา Score ได้
        while (score == null)
        {
            score = ScoreV2_5.Instance;
            yield return null;
        }

        // พยายามอ่าน missionStartTime จาก ScoreV2_5 (เป็น private จึงใช้รีเฟล็กชัน)
        float missionStart = -1f;
        var fi = typeof(ScoreV2_5).GetField("_missionStartTime",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        while (missionStart <= 0f)
        {
            if (fi != null)
            {
                object v = fi.GetValue(score);
                if (v is float f) missionStart = f;
            }
            yield return null;
        }

        // เวลาระเบิดตามโหมด
        float triggerAt = (countdownMode == CountdownMode.Immediate)
            ? missionStart + Mathf.Max(0f, score.explosionCountdownSeconds)    // ระเบิดเมื่อครบ Countdown
            : missionStart + Mathf.Max(0f, score.missionDurationSeconds);      // ระเบิดเมื่อครบ Mission

        // รอจนถึงระเบิด + ดีเลย์รวม
        float wait = triggerAt - Time.time + extraDelayAfterExplosion;
        if (wait > 0f) yield return new WaitForSeconds(wait);

        // เปิดเป็นชุด ๆ (ตามดีเลย์รายชิ้น)
        yield return StartCoroutine(Co_FireEffects());
    }

    IEnumerator Co_FireEffects()
    {
        if (effects == null || effects.Length == 0) yield break;

        // เรียงตาม delay เพื่อลดซ้อนทับ
        foreach (var item in effects.OrderBy(e => e.delayFromExplosion))
        {
            if (item == null || item.effect == null) continue;

            if (item.delayFromExplosion > 0f)
                yield return new WaitForSeconds(item.delayFromExplosion);

            // ถ้าเป็นวัตถุในซีน (อยู่ใน Scene) -> SetActive
            if (item.effect.scene.IsValid() && item.setActiveIfInScene)
            {
                item.effect.SetActive(true);
            }
            else
            {
                // เป็น Prefab -> Instantiate (ถ้าเปิดใช้งาน)
                if (item.instantiateIfPrefab)
                {
                    Transform spawnAt = item.spawnPoint != null ? item.spawnPoint : transform;
                    var inst = Instantiate(item.effect,
                        spawnAt.position + spawnAt.TransformVector(item.spawnLocalOffset),
                        spawnAt.rotation);
                    // เผื่อเป็นพาร์ติเคิล/วีเอฟเอกซ์ ให้สั่ง Play
                    PlayAll(inst);
                }
                else
                {
                    // ไม่อินสแตนซ์ ก็พยายามเปิดตัวเดิม (กรณีเผลอลากซีนอ็อบเจ็กต์มา)
                    item.effect.SetActive(true);
                    PlayAll(item.effect);
                }
            }
        }
    }

    void PlayAll(GameObject go)
    {
        if (go == null) return;
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
            ps.Play(true);
        // รองรับ VisualEffect (ถ้ามี URP+VFXGraph)
        var comps = go.GetComponentsInChildren<Component>(true);
        foreach (var c in comps)
        {
            if (c == null) continue;
            var t = c.GetType();
            if (t.Name == "VisualEffect")
            {
                var m = t.GetMethod("Play", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (m != null) m.Invoke(c, null);
            }
        }
    }

    void UpdatePreviewTimes()
    {
        // แค่โชว์ไว้เช็กในอินสเปกเตอร์
        if (score == null)
        {
            score = ScoreV2_5.Instance;
            previewExplosionCountdownFromScore = -1f;
            previewTriggerAt = -1f;
            previewRemaining = -1f;
            return;
        }

        previewExplosionCountdownFromScore = score.explosionCountdownSeconds;

        // รีเฟล็กชันอ่านเวลาเริ่ม (เพื่อทำ preview)
        float missionStart = -1f;
        var fi = typeof(ScoreV2_5).GetField("_missionStartTime",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (fi != null)
        {
            object v = fi.GetValue(score);
            if (v is float f) missionStart = f;
        }

        if (missionStart > 0f)
        {
            float triggerAt = (countdownMode == CountdownMode.Immediate)
                ? missionStart + Mathf.Max(0f, score.explosionCountdownSeconds)
                : missionStart + Mathf.Max(0f, score.missionDurationSeconds);

            previewTriggerAt = triggerAt;
            previewRemaining = triggerAt - Time.time;
        }
        else
        {
            previewTriggerAt = -1f;
            previewRemaining = -1f;
        }
    }
}
