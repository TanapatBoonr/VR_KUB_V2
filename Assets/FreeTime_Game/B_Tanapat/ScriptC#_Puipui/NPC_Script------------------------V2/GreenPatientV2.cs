using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class GreenPatientV2 : MonoBehaviour
{
    [Header("พื้นฐาน / การเคลื่อนที่")]
    public NavMeshAgent agent;
    public Transform playerCamera;
    [Tooltip("ระยะที่หยุดหน้าผู้เล่น")]
    public float stopDistanceFromPlayer = 1.5f;

    [Header("ปลายทางหลังได้รับบัตร (ค่าเริ่มต้น)")]
    [Tooltip("ใช้เมื่อหาเป้าหมายตาม Plane ไม่ได้")]
    public Transform finalDestination;

    // =========================== NEW: ปลายทางตาม Plane ===========================
    [Header("Per-Plane Destinations (เลือกปลายทางตามโซนที่เล่น)")]
    public Transform destinationA;
    public Transform destinationB;
    public Transform destinationC;
    public Transform destinationD;
    public Transform destinationE;
    public Transform destinationF;

    [Header("Active Plane (optional)")]
    [Tooltip("ปล่อยว่างได้ ถ้ามี ScoreV2_5 อยู่ในซีน สคริปต์จะพยายามอ่านโซนปัจจุบันจาก ScoreV2_5 อัตโนมัติ")]
    public string activePlaneId = ""; // "A".."F" หรือเว้นว่างให้สคริปต์ลองอ่านจาก ScoreV2_5
    // ============================================================================

    [Header("Animator (ใช้พารามิเตอร์เดิมของคุณ)")]
    public Animator animator;
    [Tooltip("Bool เดิน/หยุด (true=เดิน, false=Idle)")]
    public string paramMoveBool = "Move";
    [Tooltip("Trigger ให้ลุกยืน")]
    public string paramStandUpTrigger = "StandUp";
    [Tooltip("ชื่อ State ท่าลุก (ต้องตรงกับชื่อใน Animator)")]
    public string animStandUp = "G2 Standing Up";
    [Tooltip("ชื่อ State Idle (ต้องตรงกับชื่อใน Animator)")]
    public string animIdle = "G2 Idle";

    [Header("การรอ StandUp และ Idle")]
    [Tooltip("เวลาขั้นต่ำที่ต้องปล่อยให้ท่าลุกเล่น")]
    public float standUpMinSeconds = 0.35f;
    [Tooltip("รอให้ท่าลุกจบครบอย่างน้อย 1 รอบ (ถ้าระบบหา State ได้)")]
    public bool waitStandUpOneCycle = true;

    [Tooltip("เวลาขั้นต่ำที่ต้องยืน Idle ก่อนเริ่มเดิน")]
    public float idleMinSeconds = 0.75f;
    [Tooltip("รอให้อนิเมชัน Idle จบครบอย่างน้อย 1 รอบ (ถ้าระบบหา State ได้)")]
    public bool waitIdleOneCycle = true;
    [Tooltip("จำนวนรอบ Idle ที่อยากรอก่อนเดิน (ใช้เมื่อเปิด waitIdleOneCycle)")]
    public int idleCyclesToWait = 1;

    [Header("XR Socket / บัตร")]
    public GameObject tagSocketObject;                // Cube ที่มี XRSocketInteractor
    public XRSocketInteractor tagSocket;

    [Tooltip("วิธีที่ 1: เช็ค Tag ของบัตร (ถ้ามี Tag จริงใน Project)")]
    public string requiredTagName = "Green";
    [Tooltip("วิธีที่ 2: เช็คจากชื่อ GameObject เช่น 'Green_Tag-Triage'")]
    public string requiredNameContains = "Green_Tag-Triage";
    [Tooltip("วิธีที่ 3: ลาก Prefab ของบัตรที่ยอมรับได้ (หนึ่งหรือหลายอัน)")]
    public GameObject[] allowedPrefabs;
    private HashSet<string> _allowedPrefabNames;

    [Header("ผู้บาดเจ็บหูหนวก")]
    public bool isDeaf = false;

    [Header("UI (World Space) สำหรับหูหนวก")]
    public Button uiAskSpeakBtn;
    public Button uiAskMoveBtn;
    public GameObject uiAnswerYesGraphic;
    public float uiAnswerDuration = 1.2f;

    [Header("ตัวเลือกเพิ่มเติม")]
    [Tooltip("เมื่อยืนถึงผู้เล่นแล้ว เปิด Socket ให้วางบัตร")]
    public bool enableSocketWhenReachPlayer = true;

    // สถานะ
    private bool _isWalkingToPlayer = false;
    private bool _reachedPlayer = false;
    private bool _tagAttached = false;
    private bool _finished = false;

    // -------------------- ScoreV2.5 hooks --------------------
    private ScoreV2_5 _score;
    private ScoreV2_5 Score() {
        if (_score == null) _score = FindObjectOfType<ScoreV2_5>();
        return _score;
    }
    private void RegisterColor(ScoreV2_5.TriageColor color, bool correct = true) {
        Score()?.RegisterTagResult(color, correct);
    }
    private void RegisterFinished() {
        Score()?.RegisterPatientFinished();
    }
    // ---------------------------------------------------------

    void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();
        if (playerCamera == null && Camera.main != null) playerCamera = Camera.main.transform;

        if (tagSocketObject != null) tagSocketObject.SetActive(false);
        if (tagSocket != null) tagSocket.selectEntered.AddListener(OnTagAttachedEvent);

        _allowedPrefabNames = new HashSet<string>();
        if (allowedPrefabs != null)
            foreach (var p in allowedPrefabs)
                if (p != null) _allowedPrefabNames.Add(p.name);

        MegaphoneController.OnMegaphoneStateChanged += OnMegaphoneToggle;

        SetupUIDeaf(false);
        SetMove(false); // เริ่มต้นเป็น Idle
        if (agent != null) agent.isStopped = true;
    }

    void OnDestroy()
    {
        if (tagSocket != null) tagSocket.selectEntered.RemoveListener(OnTagAttachedEvent);
        MegaphoneController.OnMegaphoneStateChanged -= OnMegaphoneToggle;
    }

    void Start()
    {
        // ถ้าตั้ง activePlaneId มาตั้งแต่แรก ก็ sync ปลายทางให้เลย
        if (!string.IsNullOrEmpty(activePlaneId))
            ApplyPlaneDestination(activePlaneId);

        if (isDeaf)
        {
            SetupUIDeaf(true);
            if (uiAskSpeakBtn) uiAskSpeakBtn.onClick.AddListener(OnAskSpeak);
            if (uiAskMoveBtn)  uiAskMoveBtn.onClick.AddListener(OnAskMove);
        }
    }

    void Update()
    {
        // ขับ Move bool ด้วยความเร็วของ Agent → กันอาการ “สไลด์ด้วย Idle”
        DriveAnimatorByAgentSpeed();

        // เผื่อ event ไม่ยิง ตรวจซ้ำว่ามีของใน socket ไหม
        if (!_tagAttached && !_finished && tagSocket != null && tagSocket.hasSelection)
        {
            var sel = tagSocket.interactablesSelected.FirstOrDefault();
            TryHandleTag(sel);
        }
    }

    // ---------- โทรโข่ง ----------
    private void OnMegaphoneToggle(bool isOn)
    {
        if (_finished || _tagAttached || _isWalkingToPlayer) return;
        if (!isOn) return;
        if (isDeaf) return;

        StartCoroutine(Co_StandUpThenIdleThenWalkToPlayer());
    }

    private IEnumerator Co_StandUpThenIdleThenWalkToPlayer()
    {
        // 0) หยุดเคลื่อนที่ระหว่างลุก/Idle
        if (agent) agent.isStopped = true;
        SetMove(false);

        // 1) เล่น StandUp
        yield return StartCoroutine(Co_PlayStateFully(animStandUp, useTrigger: true, minSeconds: standUpMinSeconds, waitStandUpOneCycle));

        // 2) เข้า Idle แล้ว “รอ Idle ให้พอ”
        yield return StartCoroutine(Co_PlayIdlePhase());

        // 3) เริ่มเดินเข้าหาผู้เล่น
        _isWalkingToPlayer = true;
        EnsureAgentOnNavMesh();
        MoveTo(GetPointInFrontOfPlayer());

        while (!_reachedPlayer && !_tagAttached)
        {
            if (playerCamera != null)
            {
                var a = new Vector3(transform.position.x, 0f, transform.position.z);
                var b = new Vector3(playerCamera.position.x, 0f, playerCamera.position.z);
                if (Vector3.Distance(a, b) <= stopDistanceFromPlayer + 0.05f)
                {
                    _reachedPlayer = true;
                    if (agent) agent.isStopped = true;
                    SetMove(false);
                    if (enableSocketWhenReachPlayer && tagSocketObject != null)
                        tagSocketObject.SetActive(true);
                }
            }
            yield return null;
        }
    }

    private IEnumerator Co_PlayStateFully(string stateName, bool useTrigger, float minSeconds, bool waitOneCycle)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
        {
            if (minSeconds > 0f) yield return new WaitForSeconds(minSeconds);
            yield break;
        }

        if (useTrigger && !string.IsNullOrEmpty(paramStandUpTrigger))
        {
            animator.ResetTrigger(paramStandUpTrigger);
            animator.SetTrigger(paramStandUpTrigger);
        }
        else
        {
            animator.CrossFadeInFixedTime(stateName, 0.1f);
        }

        int hash = Animator.StringToHash(stateName);
        float safetyEnter = 1.5f;
        float enterEnd = Time.time + safetyEnter;
        while (Time.time < enterEnd)
        {
            if (!animator.IsInTransition(0))
            {
                var st = animator.GetCurrentAnimatorStateInfo(0);
                if (st.shortNameHash == hash || st.IsName(stateName))
                    break;
            }
            yield return null;
        }

        float atLeast = Mathf.Max(0f, minSeconds);

        float length = 0f;
        bool hasLen = false;
        if (waitOneCycle)
        {
            var st = animator.GetCurrentAnimatorStateInfo(0);
            length = st.length / Mathf.Max(0.001f, animator.speed);
            hasLen = length > 0.001f;
            if (hasLen) atLeast = Mathf.Max(atLeast, length);
        }

        if (atLeast > 0f) yield return new WaitForSeconds(atLeast);
    }

    private IEnumerator Co_PlayIdlePhase()
    {
        float t0 = Time.time;

        if (!string.IsNullOrEmpty(animIdle) && animator != null)
            animator.CrossFadeInFixedTime(animIdle, 0.1f);

        float idleLen = 0f;
        bool hasLen = false;
        int idleHash = !string.IsNullOrEmpty(animIdle) ? Animator.StringToHash(animIdle) : 0;

        if (animator != null && idleHash != 0)
        {
            float safety = 1.5f;
            float end = Time.time + safety;
            while (Time.time < end)
            {
                if (!animator.IsInTransition(0))
                {
                    var st = animator.GetCurrentAnimatorStateInfo(0);
                    if (st.shortNameHash == idleHash || st.IsName(animIdle))
                    {
                        idleLen = st.length / Mathf.Max(0.001f, animator.speed);
                        hasLen = idleLen > 0.001f;
                        break;
                    }
                }
                yield return null;
            }
        }

        float atLeast = Mathf.Max(0f, idleMinSeconds);
        if (waitIdleOneCycle && hasLen)
            atLeast = Mathf.Max(atLeast, idleLen * Mathf.Max(1, idleCyclesToWait));

        float remain = atLeast - (Time.time - t0);
        if (remain > 0f) yield return new WaitForSeconds(remain);
    }

    // ---------- หูหนวก ----------
    private void SetupUIDeaf(bool on)
    {
        if (uiAskSpeakBtn) uiAskSpeakBtn.gameObject.SetActive(on);
        if (uiAskMoveBtn)  uiAskMoveBtn.gameObject.SetActive(false);
        if (uiAnswerYesGraphic) uiAnswerYesGraphic.SetActive(false);
    }

    private void OnAskSpeak()
    {
        StartCoroutine(Co_ShowYesThen(() =>
        {
            if (uiAskMoveBtn) uiAskMoveBtn.gameObject.SetActive(true);
            if (uiAskSpeakBtn) uiAskSpeakBtn.gameObject.SetActive(false);
        }));
    }

    private void OnAskMove()
    {
        StartCoroutine(Co_ShowYesThen(() =>
        {
            if (tagSocketObject) tagSocketObject.SetActive(true);
            if (uiAskMoveBtn) uiAskMoveBtn.gameObject.SetActive(false);
        }));
    }

    private IEnumerator Co_ShowYesThen(System.Action after)
    {
        if (uiAnswerYesGraphic)
        {
            uiAnswerYesGraphic.SetActive(true);
            yield return new WaitForSeconds(Mathf.Max(0.2f, uiAnswerDuration));
            uiAnswerYesGraphic.SetActive(false);
        }
        after?.Invoke();
    }

    // ---------- รับบัตร ----------
    private void OnTagAttachedEvent(SelectEnterEventArgs args)
    {
        TryHandleTag(args.interactableObject as IXRSelectInteractable);
    }

    private void TryHandleTag(IXRSelectInteractable sel)
    {
        if (sel == null || _tagAttached || _finished) return;

        var tr = (sel as Component)?.transform;
        if (tr == null) return;

        bool ok = false;
        if (!string.IsNullOrEmpty(requiredTagName)) ok |= SafeCompareTag(tr.gameObject, requiredTagName);
        if (!ok && !string.IsNullOrEmpty(requiredNameContains)) ok |= tr.name.Contains(requiredNameContains);
        if (!ok && _allowedPrefabNames != null && _allowedPrefabNames.Count > 0)
            ok |= _allowedPrefabNames.Contains(StripClone(tr.name));

        if (!ok)
        {
            tagSocket.interactionManager.SelectExit(tagSocket, sel);
            Debug.LogWarning($"{name}: Wrong triage item.");
            return;
        }

        _tagAttached = true;

        // ---- ScoreV2.5: นับว่า "บัตรเขียวถูกต้อง" ----
        RegisterColor(ScoreV2_5.TriageColor.Green, true);

        // ---- NEW: เลือกปลายทางตาม Plane ที่ผู้เล่นเลือก ----
        ResolveActivePlaneFromScoreIfNeeded();
        var dest = GetDestinationFor(activePlaneId);
        if (dest != null) finalDestination = dest;

        EnsureAgentOnNavMesh();
        if (agent != null) agent.isStopped = false;

        if (finalDestination != null)
        {
            MoveTo(finalDestination.position);
            StartCoroutine(Co_WaitUntilArriveThenFinish());
        }
        else
        {
            SetMove(false);
            _finished = true;
            RegisterFinished();
        }
    }

    private IEnumerator Co_WaitUntilArriveThenFinish()
    {
        while (finalDestination != null && !_finished)
        {
            if (agent == null || !agent.isOnNavMesh) yield break;

            float dist = Vector3.Distance(transform.position, finalDestination.position);
            if (dist <= Mathf.Max(agent.stoppingDistance, 0.3f))
            {
                agent.isStopped = true;
                SetMove(false);
                _finished = true;

                // ---- ScoreV2.5: รายงานว่า "ผู้บาดเจ็บรายนี้เสร็จแล้ว" ----
                RegisterFinished();

                yield break;
            }
            yield return null;
        }
    }

    // ---------- Utilities ----------
    private void SetMove(bool moving)
    {
        if (animator == null || string.IsNullOrEmpty(paramMoveBool)) return;
        animator.SetBool(paramMoveBool, moving);
    }

    private void DriveAnimatorByAgentSpeed()
    {
        if (animator == null || string.IsNullOrEmpty(paramMoveBool)) return;

        bool moving = false;
        if (agent != null && agent.isOnNavMesh)
        {
            moving = !agent.isStopped &&
                     agent.velocity.magnitude > 0.05f &&
                     agent.remainingDistance > agent.stoppingDistance;
        }
        SetMove(moving);
    }

    private void EnsureAgentOnNavMesh()
    {
        if (agent == null) return;
        if (agent.isOnNavMesh) return;

        if (NavMesh.SamplePosition(transform.position, out var hit, 1.0f, NavMesh.AllAreas))
            agent.Warp(hit.position);
        else
            Debug.LogError($"{name}: not on NavMesh and cannot find nearby surface.");
    }

    private void MoveTo(Vector3 worldPos)
    {
        if (agent == null) return;
        EnsureAgentOnNavMesh();
        if (!agent.isOnNavMesh) return;

        agent.isStopped = false;
        agent.SetDestination(worldPos);
        // Move bool จะถูกตั้งอัตโนมัติจาก DriveAnimatorByAgentSpeed()
    }

    private Vector3 GetPointInFrontOfPlayer()
    {
        if (playerCamera == null) return transform.position;
        Vector3 camPos = playerCamera.position;
        Vector3 camFwd = playerCamera.forward; camFwd.y = 0f; camFwd.Normalize();
        Vector3 target = camPos + camFwd * (-stopDistanceFromPlayer);
        target.y = transform.position.y;
        return target;
    }

    private static bool SafeCompareTag(GameObject go, string tagText)
    {
        if (go == null || string.IsNullOrEmpty(tagText)) return false;
        try { return go.CompareTag(tagText); }
        catch { return go.tag == tagText; }
    }
    private static string StripClone(string n)
    {
        const string c = "(Clone)";
        return !string.IsNullOrEmpty(n) && n.EndsWith(c) ? n.Substring(0, n.Length - c.Length) : n;
    }

    // =========================== NEW: helpers สำหรับ Plane ===========================
    public void ApplyPlaneDestination(string planeId)
    {
        activePlaneId = planeId;
        var dest = GetDestinationFor(activePlaneId);
        if (dest != null) finalDestination = dest;
    }

    private Transform GetDestinationFor(string planeId)
    {
        if (string.IsNullOrEmpty(planeId)) return null;
        switch (planeId.Trim().ToUpper())
        {
            case "A": return destinationA;
            case "B": return destinationB;
            case "C": return destinationC;
            case "D": return destinationD;
            case "E": return destinationE;
            case "F": return destinationF;
            default:  return null;
        }
    }

    private void ResolveActivePlaneFromScoreIfNeeded()
    {
        if (!string.IsNullOrEmpty(activePlaneId)) return;

        var sc = Score();
        if (sc == null) return;

        // พยายามอ่านทั้ง Property และ Field (รองรับโปรเจ็กต์เดิมของคุณ)
        var t = sc.GetType();

        // public string CurrentAreaId {get;}
        var prop = t.GetProperty("CurrentAreaId", BindingFlags.Instance | BindingFlags.Public);
        if (prop != null && prop.PropertyType == typeof(string))
        {
            var v = prop.GetValue(sc) as string;
            if (!string.IsNullOrEmpty(v)) { activePlaneId = v; return; }
        }

        // private string _currentArea;
        var field = t.GetField("_currentArea", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(string))
        {
            var v = field.GetValue(sc) as string;
            if (!string.IsNullOrEmpty(v)) { activePlaneId = v; return; }
        }
    }
    // ================================================================================
}
