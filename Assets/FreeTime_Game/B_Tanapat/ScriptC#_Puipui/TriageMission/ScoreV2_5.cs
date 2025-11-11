using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem; // debug Space บน New Input System
#endif

public class ScoreV2_5 : MonoBehaviour
{
    // ========= Singleton =========
    public static ScoreV2_5 Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        InitDictionaries();
        SetupInitialUI();
    }

    // ========= Types =========
    public enum TriageColor { Green, Yellow, Red, Black }

    // ========= Intro / Selection =========
    [Header("Intro / Selection UI")]
    public GameObject introPanel;
    public Button continueButton;
    public GameObject buttonPlaneCanvas;

    [Header("Plane Buttons (optional)")]
    public Button planeA_Button, planeB_Button, planeC_Button, planeD_Button, planeE_Button, planeF_Button;

    [Header("Mission / Plane UI Roots (assign from Hierarchy)")]
    public GameObject missionIntroRoot; // กล่องข้อความ + continue (ถ้ามี)
    public GameObject buttonPlaneRoot;  // Canvas ---------------- Button_Plane (A–F)

    // ========= Timer / Explosion =========
    [Header("Mission Timer & Explosion")]
    [Tooltip("เวลาภารกิจทั้งหมด (วินาที)")]
    public float missionDurationSeconds = 5 * 60f;
    [Tooltip("เวลาถอยหลังจนเกิดระเบิด (วินาที) — เริ่มนับทันทีหลังเลือกโซน")]
    public float explosionCountdownSeconds = 60f;
    [Tooltip("เริ่มเตือนก่อนระเบิดกี่วินาที")]
    public float preExplosionWarningSeconds = 15f;

    [Header("Explosion Objects")]
    [Tooltip("วัตถุเอฟเฟกต์ระเบิด (Prefab หรือ Scene object)")]
    public GameObject explosionObject;
    [Tooltip("ถ้าเป็น Prefab ให้ Instantiate ตอนระเบิด")]
    public bool instantiateIfPrefab = true;
    [Tooltip("จุดเกิดระเบิด (ถ้าไม่เซ็ตจะใช้ sphereBomCenter หรือ transform นี้)")]
    public Transform explosionSpawnPoint;
    [Tooltip("Fallback/จุดคำนวณรัศมี (มักเป็น SphereBom)")]
    public Transform sphereBomCenter;
    [Tooltip("อ่านรัศมีจาก SphereBom (SphereCollider.radius * scale)")]
    public bool radiusFromSphereBom = true;
    [Tooltip("รัศมีสำรอง เมื่อไม่ได้อ่านจาก SphereBom")]
    public float sphereBomRadius = 6f;

    [Header("Explosion Hold / Particles")]
    [Tooltip("หลังจุดระเบิด ให้แสดงค้างไว้นานเท่านี้ (วินาที)")]
    public float explosionHoldSeconds = 3f;
    [Tooltip("ถ้าเปิด จะรอจนอนุภาคเล่นจบ (สูงสุด ~5 วินาที) ก่อนปิด")]
    public bool waitParticlesToFinish = true;

    [Header("Timer/Warning UI")]
    public TMP_Text timerText;
    public TMP_Text explosionWarningText;
    public TMP_Text gameOverText;

    // ========= Player / Teleport =========
    [Header("Player / Teleport")]
    public Transform playerRoot;
    public Transform summaryWarpPoint;

    // ========= Restart =========
    [Header("Restart Buttons (in-areas)")]
    public Button restartA_Button, restartB_Button, restartC_Button, restartD_Button, restartE_Button, restartF_Button;

    // ========= Summary =========
    [Header("Summary UI")]
    public GameObject summaryPanel;
    public TMP_Text summaryTimeText, summaryGreenText, summaryYellowText, summaryRedText, summaryBlackText;

    // ========= Targets =========
    [Header("Target counts (ตั้งตามภารกิจ)")]
    public int targetGreen = 10, targetYellow = 10, targetRed = 8, targetBlack = 2;

    [Header("Expected number of patients to finish before success")]
    public int expectedPatientsToFinish = 30;

    // ========= Runtime =========
    readonly Dictionary<TriageColor, int> _correctByColor = new();
    readonly Dictionary<TriageColor, int> _totalByColor   = new();

    float _missionStartTime = -1f;
    bool  _missionRunning   = false;

    bool _explosionScheduled    = false;
    bool _explosionWarningShown = false;
    bool _exploded              = false;
    bool _gameOver              = false;

    string _currentArea = ""; // A..F
    int    _patientsFinished = 0;

    GameObject _explosionInstance;

    // ========= Setup =========
    void Start()
    {
        if (continueButton) continueButton.onClick.AddListener(OnContinueClicked);

        if (planeA_Button) planeA_Button.onClick.AddListener(() => OnPlaneSelected("A"));
        if (planeB_Button) planeB_Button.onClick.AddListener(() => OnPlaneSelected("B"));
        if (planeC_Button) planeC_Button.onClick.AddListener(() => OnPlaneSelected("C"));
        if (planeD_Button) planeD_Button.onClick.AddListener(() => OnPlaneSelected("D"));
        if (planeE_Button) planeE_Button.onClick.AddListener(() => OnPlaneSelected("E"));
        if (planeF_Button) planeF_Button.onClick.AddListener(() => OnPlaneSelected("F"));

        if (restartA_Button) restartA_Button.onClick.AddListener(RestartScene);
        if (restartB_Button) restartB_Button.onClick.AddListener(RestartScene);
        if (restartC_Button) restartC_Button.onClick.AddListener(RestartScene);
        if (restartD_Button) restartD_Button.onClick.AddListener(RestartScene);
        if (restartE_Button) restartE_Button.onClick.AddListener(RestartScene);
        if (restartF_Button) restartF_Button.onClick.AddListener(RestartScene);

        // ถ้าเป็น Scene object ให้ปิดไว้ก่อน
        if (IsSceneObject(explosionObject)) SafeSetActive(explosionObject, false);
    }

    void Update()
    {
        UpdateTimerUI();

        // debug space
        if (DebugSpacePressed()) OnContinueClicked();

        // time up -> game over (ถ้ายังไม่ครบ)
        if (_missionRunning && !_gameOver)
        {
            float elapsed = Time.time - _missionStartTime;
            if (elapsed >= missionDurationSeconds && _patientsFinished < Mathf.Max(1, expectedPatientsToFinish))
            {
                _gameOver = true;
                if (gameOverText)
                {
                    gameOverText.gameObject.SetActive(true);
                    gameOverText.text = "TIME UP - GAME OVER";
                }
                FinishMission(false);
            }
        }

        // เริ่มนับระเบิดทันทีหลังเริ่มภารกิจ
        if (_missionRunning && !_explosionScheduled)
        {
            StartCoroutine(Co_ExplosionTimeline());
            _explosionScheduled = true;
        }
    }

    bool DebugSpacePressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    // ========= Hooks จากผู้บาดเจ็บ =========
    public void RegisterTagResult(TriageColor color, bool correct)
    {
        if (!_totalByColor.ContainsKey(color))   _totalByColor[color]   = 0;
        if (!_correctByColor.ContainsKey(color)) _correctByColor[color] = 0;
        _totalByColor[color]++;
        if (correct) _correctByColor[color]++;
    }

    public void RegisterPatientFinished()
    {
        _patientsFinished++;
        if (!_gameOver && _patientsFinished >= Mathf.Max(1, expectedPatientsToFinish))
        {
            FinishMission(true);
        }
    }

    // ========= Flow =========
    void SetupInitialUI()
    {
        SafeSetActive(introPanel,        true);
        SafeSetActive(buttonPlaneCanvas, false);
        SafeSetActive(summaryPanel,      false);

        if (explosionWarningText) explosionWarningText.gameObject.SetActive(false);
        if (gameOverText)         gameOverText.gameObject.SetActive(false);

        SafeSetActive(missionIntroRoot, true);
        SafeSetActive(buttonPlaneRoot,  false);
    }

    void InitDictionaries()
    {
        foreach (TriageColor c in System.Enum.GetValues(typeof(TriageColor)))
        {
            _correctByColor.TryAdd(c, 0);
            _totalByColor  .TryAdd(c, 0);
        }
    }

    public void OnContinueClicked()
    {
        SafeSetActive(introPanel,       false);
        SafeSetActive(missionIntroRoot, false);
        SafeSetActive(buttonPlaneCanvas, true);
        SafeSetActive(buttonPlaneRoot,   true);
    }

    public void OnPlaneSelected(string areaId)
    {
        _currentArea      = areaId;
        _missionStartTime = Time.time;
        _missionRunning   = true;

        SafeSetActive(buttonPlaneCanvas, false);
        SafeSetActive(buttonPlaneRoot,   false);
    }

    // ========= Timer UI =========
    void UpdateTimerUI()
    {
        if (!timerText) return;

        if (!_missionRunning) { timerText.text = "00:00"; return; }

        float elapsed = Time.time - _missionStartTime;
        float remain  = Mathf.Max(0, missionDurationSeconds - elapsed);
        int m = Mathf.FloorToInt(remain / 60f);
        int s = Mathf.FloorToInt(remain % 60f);
        timerText.text = $"{m:00}:{s:00}";
    }

    // ========= Explosion Timeline =========
    IEnumerator Co_ExplosionTimeline()
    {
        float tEnd = _missionStartTime + Mathf.Max(0.1f, explosionCountdownSeconds);

        while (Time.time < tEnd && !_exploded && !_gameOver)
        {
            float remain = tEnd - Time.time;
            if (remain <= preExplosionWarningSeconds && !_explosionWarningShown)
            {
                _explosionWarningShown = true;
                if (explosionWarningText)
                {
                    explosionWarningText.gameObject.SetActive(true);
                    explosionWarningText.text = "Warning! Explosion imminent! Move away!";
                }
            }
            yield return null;
        }

        if (!_exploded && !_gameOver)
        {
            _exploded = true;

            PlayExplosion();

            float radius = GetEffectiveRadius();
            Transform center = GetExplosionCenter();

            if (playerRoot && center && radius > 0f)
            {
                float dist = Vector3.Distance(
                    new Vector3(playerRoot.position.x, 0f, playerRoot.position.z),
                    new Vector3(center.position.x,     0f, center.position.z)
                );
                if (dist <= radius) GameOverByExplosion();
            }

            float hold = Mathf.Max(0f, explosionHoldSeconds);
            if (waitParticlesToFinish && _explosionInstance != null)
            {
                float timeout = 5f;
                float end = Time.time + timeout;
                while (Time.time < end && AnyParticlesAlive(_explosionInstance))
                    yield return null;
                if (hold > 0f) yield return new WaitForSeconds(hold);
            }
            else
            {
                if (hold > 0f) yield return new WaitForSeconds(hold);
            }

            StopExplosion();
            if (explosionWarningText) explosionWarningText.gameObject.SetActive(false);
        }
    }

    // ========= Explosion Helpers =========
    bool IsSceneObject(GameObject go) => go != null && go.scene.IsValid();

    Transform GetExplosionCenter()
    {
        if (explosionSpawnPoint) return explosionSpawnPoint;
        if (sphereBomCenter)     return sphereBomCenter;
        return transform;
    }

    float GetEffectiveRadius()
    {
        if (radiusFromSphereBom && sphereBomCenter != null)
        {
            var sc = sphereBomCenter.GetComponent<SphereCollider>();
            if (sc != null)
            {
                float maxScale = Mathf.Max(
                    sphereBomCenter.lossyScale.x,
                    Mathf.Max(sphereBomCenter.lossyScale.y, sphereBomCenter.lossyScale.z)
                );
                return sc.radius * maxScale;
            }
            return Mathf.Max(0.1f, sphereBomCenter.lossyScale.x * 0.5f);
        }
        return Mathf.Max(0f, sphereBomRadius);
    }

    void PlayExplosion()
    {
        if (explosionObject == null) return;

        Transform center = GetExplosionCenter();
        Vector3 pos = center ? center.position : transform.position;
        Quaternion rot = center ? center.rotation : transform.rotation;

        if (IsSceneObject(explosionObject))
        {
            _explosionInstance = explosionObject;
            _explosionInstance.transform.SetPositionAndRotation(pos, rot);
            SafeSetActive(_explosionInstance, true);
        }
        else
        {
            if (instantiateIfPrefab)
                _explosionInstance = Instantiate(explosionObject, pos, rot);
            else
                _explosionInstance = null; // ไม่สร้าง ก็ไม่แสดงเอฟเฟกต์
        }

        if (_explosionInstance != null)
        {
            // เล่นอนุภาคทั้งหมด (ParticleSystem เท่านั้น — ไม่มี ParticleEmitter legacy แล้ว)
            foreach (var ps in _explosionInstance.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Clear(true);
                ps.Play(true);
            }
        }
    }

    bool AnyParticlesAlive(GameObject go)
    {
        if (!go) return false;
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
            if (ps.IsAlive(true)) return true;
        return false;
    }

    void StopExplosion()
    {
        if (_explosionInstance == null) return;

        if (IsSceneObject(_explosionInstance))
            SafeSetActive(_explosionInstance, false);
        else
            Destroy(_explosionInstance);

        _explosionInstance = null;
    }

    // ========= Finish / GameOver =========
    void GameOverByExplosion()
    {
        _gameOver = true;

        if (gameOverText)
        {
            gameOverText.gameObject.SetActive(true);
            gameOverText.text = "GAME OVER";
        }
        FinishMission(false);
    }

    void FinishMission(bool success)
    {
        _missionRunning = false;

        if (playerRoot && summaryWarpPoint)
        {
            playerRoot.position = summaryWarpPoint.position;
            playerRoot.rotation = summaryWarpPoint.rotation;
        }

        SafeSetActive(summaryPanel, true);

        if (summaryTimeText)
        {
            float elapsed = (_missionStartTime > 0f) ? Mathf.Max(0, Time.time - _missionStartTime) : 0f;
            int m = Mathf.FloorToInt(elapsed / 60f);
            int s = Mathf.FloorToInt(elapsed % 60f);
            summaryTimeText.text = $"Time Used : {m:00}:{s:00}";
        }

        if (summaryGreenText)  summaryGreenText.text  = $"Green  : {_correctByColor[TriageColor.Green]} / {targetGreen}";
        if (summaryYellowText) summaryYellowText.text = $"Yellow : {_correctByColor[TriageColor.Yellow]} / {targetYellow}";
        if (summaryRedText)    summaryRedText.text    = $"Red    : {_correctByColor[TriageColor.Red]} / {targetRed}";
        if (summaryBlackText)  summaryBlackText.text  = $"Black  : {_correctByColor[TriageColor.Black]} / {targetBlack}";

        if (!_gameOver && gameOverText) gameOverText.gameObject.SetActive(false);
    }

    // ========= Restart =========
    public void RestartScene()
    {
        int idx = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(idx);
    }

    public void RestartSceneAtCurrentArea() => RestartScene();

    // ========= Utils =========
    void SafeSetActive(GameObject go, bool on)
    {
        if (!go) return;
        if (go.activeSelf != on) go.SetActive(on);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var c = GetExplosionCenter();
        float r = GetEffectiveRadius();
        if (c && r > 0f)
        {
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(c.position, r);
        }
    }
#endif
}
