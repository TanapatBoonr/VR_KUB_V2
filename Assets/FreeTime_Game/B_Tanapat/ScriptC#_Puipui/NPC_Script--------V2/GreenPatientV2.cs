using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class GreenPatientV2 : MonoBehaviour
{
    [Header("พื้นฐาน / การเคลื่อนที่")]
    public NavMeshAgent agent;
    public Transform playerCamera;
    public float stopDistanceFromPlayer = 1.5f;
    public float idleAfterStandSeconds = 0.75f;

    [Header("ปลายทางเมื่อภารกิจเสร็จ")]
    public Transform finalDestination;

    [Header("Animator (ถ้ามี)")]
    public string animStandUp = "G2 Standing Up";
    public string animIdle   = "G2 Idle";
    public string animWalk   = "G2 Walk";
    private Animator _anim;

    [Header("XR Socket / บัตร")]
    public GameObject tagSocketObject;       // Cube ที่มี XRSocketInteractor
    public XRSocketInteractor tagSocket;

    [Tooltip("ทางที่ 1: ถ้ามี Tag จริงใน Project (เช่น 'Green') ให้ใส่ตรงนี้")]
    public string requiredTagName = "Green";

    [Tooltip("ทางที่ 2: ถ้าไม่สร้าง Tag ให้เช็คจากชื่อ เช่น 'Green_Tag-Triage'")]
    public string requiredNameContains = "Green_Tag-Triage";

    [Tooltip("ทางที่ 3: ลาก Prefab ของบัตรที่ยอมรับได้ (หนึ่งหรือหลายชิ้น)")]
    public GameObject[] allowedPrefabs;
    private HashSet<string> _allowedPrefabNames; // เก็บชื่อ Prefab เพื่อเทียบตอนรัน

    [Header("ผู้บาดเจ็บหูหนวก")]
    public bool isDeaf = false;

    [Header("UI (World Space) สำหรับเคสหูหนวก")]
    public Button uiAskSpeakBtn;
    public Button uiAskMoveBtn;
    public GameObject uiAnswerYesGraphic;
    public float uiAnswerDuration = 1.2f;

    [Header("ตัวเลือกเพิ่มเติม")]
    public bool enableSocketWhenReachPlayer = true;

    // สถานะภายใน
    private bool _isWalkingToPlayer = false;
    private bool _reachedPlayer = false;
    private bool _tagAttached = false;
    private bool _finished = false;

    void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<Animator>();
        if (playerCamera == null && Camera.main != null) playerCamera = Camera.main.transform;

        if (tagSocketObject != null) tagSocketObject.SetActive(false);
        SetupUIDeaf(false);

        if (tagSocket != null)
            tagSocket.selectEntered.AddListener(OnTagAttachedEvent);

        // เตรียมชุดชื่อ Prefab ที่ยอมรับ (ไม่มี PrefabUtility → ใช้ชื่อแทน ใช้ได้ตอน Build)
        _allowedPrefabNames = new HashSet<string>();
        if (allowedPrefabs != null)
        {
            foreach (var p in allowedPrefabs)
                if (p != null) _allowedPrefabNames.Add(p.name);
        }

        MegaphoneController.OnMegaphoneStateChanged += OnMegaphoneToggle;
    }

    void OnDestroy()
    {
        if (tagSocket != null)
            tagSocket.selectEntered.RemoveListener(OnTagAttachedEvent);

        MegaphoneController.OnMegaphoneStateChanged -= OnMegaphoneToggle;
    }

    // ---------------- โทรโข่ง ----------------
    private void OnMegaphoneToggle(bool isOn)
    {
        if (_finished || _tagAttached || _isWalkingToPlayer) return;
        if (!isOn) return;
        if (isDeaf) return;

        StartCoroutine(Co_StandIdleThenWalkToPlayer());
    }

    private IEnumerator Co_StandIdleThenWalkToPlayer()
    {
        PlayAnim(animStandUp);
        yield return new WaitForSeconds(Mathf.Max(0.1f, idleAfterStandSeconds * 0.5f));

        PlayAnim(animIdle);
        yield return new WaitForSeconds(idleAfterStandSeconds);

        _isWalkingToPlayer = true;
        EnsureAgentOnNavMesh();
        MoveTo(GetPointInFrontOfPlayer());
        PlayAnim(animWalk);

        while (!_reachedPlayer && !_tagAttached)
        {
            if (playerCamera != null)
            {
                var a = new Vector3(transform.position.x, 0f, transform.position.z);
                var b = new Vector3(playerCamera.position.x, 0f, playerCamera.position.z);
                if (Vector3.Distance(a, b) <= stopDistanceFromPlayer + 0.05f)
                {
                    _reachedPlayer = true;
                    agent.isStopped = true;
                    PlayAnim(animIdle);
                    if (enableSocketWhenReachPlayer && tagSocketObject != null)
                        tagSocketObject.SetActive(true);
                }
            }
            yield return null;
        }
    }

    // ---------------- เคสหูหนวก ----------------
    void Start()
    {
        if (isDeaf)
        {
            SetupUIDeaf(true);
            if (uiAskSpeakBtn) uiAskSpeakBtn.onClick.AddListener(OnAskSpeak);
            if (uiAskMoveBtn)  uiAskMoveBtn.onClick.AddListener(OnAskMove);
        }
    }

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
            if (tagSocketObject != null) tagSocketObject.SetActive(true);
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

    // ---------------- รับบัตร (อีเวนต์) ----------------
    private void OnTagAttachedEvent(SelectEnterEventArgs args)
    {
        TryHandleTag(args.interactableObject as IXRSelectInteractable);
    }

    // ---------------- รับบัตร (ตรวจซ้ำใน Update) ----------------
    void Update()
    {
        if (!_tagAttached && !_finished && tagSocket != null && tagSocket.hasSelection)
        {
            var sel = tagSocket.interactablesSelected.FirstOrDefault();
            TryHandleTag(sel);
        }
    }

    private void TryHandleTag(IXRSelectInteractable sel)
    {
        if (sel == null || _tagAttached || _finished) return;

        var tr = (sel as Component)?.transform;
        if (tr == null) return;

        // ===== 3 ชั้นในการตรวจสอบ =====
        bool ok = false;

        // ชั้น 1: Tag (ถ้ามี Tag ในโปรเจกต์)
        if (!string.IsNullOrEmpty(requiredTagName))
            ok |= SafeCompareTag(tr.gameObject, requiredTagName);

        // ชั้น 2: ชื่อวัตถุ (เช่น Green_Tag-Triage)
        if (!ok && !string.IsNullOrEmpty(requiredNameContains))
            ok |= tr.name.Contains(requiredNameContains);

        // ชั้น 3: ตรงกับ Prefab ที่ลากไว้ (เทียบชื่อ prefab กับชื่ออินสแตนซ์ตัด "(Clone)")
        if (!ok && _allowedPrefabNames != null && _allowedPrefabNames.Count > 0)
        {
            string instName = StripCloneSuffix(tr.name);
            ok |= _allowedPrefabNames.Contains(instName);
        }

        if (!ok)
        {
            // ไม่ผ่าน → คายบัตรออก
            tagSocket.interactionManager.SelectExit(tagSocket, sel);
            Debug.LogWarning($"{name}: Wrong triage item. Need Tag='{requiredTagName}' OR Name contains '{requiredNameContains}' OR in allowedPrefabs.");
            return;
        }

        // ===== ผ่านแล้ว เริ่มเดินไปปลายทาง =====
        _tagAttached = true;
        EnsureAgentOnNavMesh();
        agent.isStopped = false;

        if (finalDestination != null)
        {
            MoveTo(finalDestination.position);
            PlayAnim(animWalk);
            StartCoroutine(Co_WaitUntilArriveThenFinish());
        }
        else
        {
            Debug.LogWarning($"{name}: finalDestination is NOT assigned. Patient will stay idle.");
            PlayAnim(animIdle);
            _finished = true;
        }
    }

    // ปลอดภัยกับกรณี Tag ไม่ได้ถูก Create ใน Project (จะไม่ throw)
    private bool SafeCompareTag(GameObject go, string tagText)
    {
        if (go == null || string.IsNullOrEmpty(tagText)) return false;
        try { return go.CompareTag(tagText); }
        catch { return go.tag == tagText; }
    }

    private string StripCloneSuffix(string n)
    {
        if (string.IsNullOrEmpty(n)) return n;
        const string clone = "(Clone)";
        return n.EndsWith(clone) ? n.Substring(0, n.Length - clone.Length) : n;
    }

    private IEnumerator Co_WaitUntilArriveThenFinish()
    {
        while (finalDestination != null && !_finished)
        {
            float dist = Vector3.Distance(transform.position, finalDestination.position);
            if (dist <= Mathf.Max(agent.stoppingDistance, 0.3f))
            {
                agent.isStopped = true;
                PlayAnim(animIdle);
                _finished = true;
                yield break;
            }
            yield return null;
        }
    }

    // ---------------- Utilities ----------------
    private void EnsureAgentOnNavMesh()
    {
        if (agent == null) return;
        if (agent.isOnNavMesh) return;

        if (NavMesh.SamplePosition(transform.position, out var hit, 1.0f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
        else
        {
            Debug.LogError($"{name}: not on NavMesh and cannot find nearby surface.");
        }
    }

    private void MoveTo(Vector3 worldPos)
    {
        if (agent == null) return;
        EnsureAgentOnNavMesh();
        if (!agent.isOnNavMesh) return;

        agent.isStopped = false;
        agent.SetDestination(worldPos);
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

    private void PlayAnim(string stateName)
    {
        if (_anim == null || string.IsNullOrEmpty(stateName)) return;
        _anim.CrossFadeInFixedTime(stateName, 0.1f);
    }
}
