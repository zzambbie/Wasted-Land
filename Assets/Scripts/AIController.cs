using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class AIController : MonoBehaviour
{
    [Header("경로 설정")]
    public Transform pathRoot;

    [Header("AI 주행 설정")]
    public float steeringSensitivity = 5.0f;
    public float speedFactor = 0.82f;  // 기본 속도 비율 (1.0이면 플레이어와 동급, 낮출수록 쉬워짐)

    [Header("아이템 사용")]
    public float itemUseDelayMin = 1.0f;
    public float itemUseDelayMax = 4.0f;

    [Header("센서 설정")]
    public float sensorLength = 12.0f;
    public float frontSensorAngle = 15.0f;
    public float sideSensorAngle = 45.0f;
    public float avoidMultiplier = 1.5f;

    public LayerMask obstacleLayer;

    private KartController kart;
    private InventoryManager inventory;
    public List<Transform> nodes = new List<Transform>();
    private int currentNode = 0;

    // === 레이싱 라인 분배 시스템 ===
    // static 카운터: AI가 생성될 때마다 자동으로 다른 라인 배정
    private static int aiSpawnCount = 0;
    private int myLaneIndex;                // 0, 1, 2... (이 AI의 고유 번호)
    private float personalLanePreference;   // 고유 차선 위치
    private float personalSpeedVariation;   // 고유 속도 편차
    private float laneOffset = 0f;
    private float laneChangeTimer = 0f;

    // === 끼임 감지 ===
    private float stuckTimer = 0f;          // 정지 상태 감지 (속도 < 2)
    private float wallProximityTimer = 0f;  // 벽 근처 시간 (센서로 감지)
    private float itemTimer = 0f;
    private float currentSteer = 0f;
    private bool isRescuing = false;

    void Start()
    {
        kart = GetComponent<KartController>();
        inventory = GetComponent<InventoryManager>();
        kart.isAI = true;

        if (pathRoot != null) { foreach (Transform child in pathRoot) nodes.Add(child); }

        // === 레이싱 라인 고유 배정 (마리오 카트 방식) ===
        // AI #0 = 왼쪽(-1.0), AI #1 = 중앙(0), AI #2 = 오른쪽(+1.0)
        // 벽에 안 닿을 정도의 적절한 간격
        myLaneIndex = aiSpawnCount;
        aiSpawnCount++;

        // 3대 기준: -1.0, 0.0, +1.0 (벽에 안 닿는 안전한 범위)
        int totalLanes = Mathf.Max(aiSpawnCount, 3);
        personalLanePreference = Mathf.Lerp(-1.0f, 1.0f, (float)myLaneIndex / (totalLanes - 1));
        laneOffset = personalLanePreference;

        // 속도 편차: 각 AI마다 살짝 다른 속도 (0.80~0.95)
        personalSpeedVariation = 0.80f + (myLaneIndex * 0.05f);
        personalSpeedVariation = Mathf.Clamp(personalSpeedVariation, 0.80f, 0.95f);

        laneChangeTimer = Random.Range(8.0f, 15.0f);
        itemTimer = Random.Range(itemUseDelayMin, itemUseDelayMax);
    }

    void OnDestroy()
    {
        // 씬 전환 시 카운터 리셋
        aiSpawnCount = 0;
    }

    void Update()
    {
        if (nodes.Count == 0) return;
        if (isRescuing) return;

        // ★ 카운트다운 중(isControlled == false)이면 AI도 정지
        if (!kart.isControlled)
        {
            kart.SetInput(0f, 0f, false, false);
            return;
        }

        // --- 1. 웨이포인트 추적 ---
        Vector3 nodePos = nodes[currentNode].position;
        int nextIndex = (currentNode + 1) % nodes.Count;
        Vector3 nextNodePos = nodes[nextIndex].position;

        Vector3 roadDirection = (nextNodePos - nodePos).normalized;
        Vector3 roadRight = Vector3.Cross(Vector3.up, roadDirection).normalized;
        Vector3 myTargetPos = nodePos + (roadRight * laneOffset);

        float distToTarget = Vector3.Distance(transform.position, myTargetPos);
        float distToNext = Vector3.Distance(transform.position, nextNodePos);

        if (distToNext < distToTarget || distToTarget < 12.0f)
        {
            currentNode = nextIndex;
            stuckTimer = 0f;
        }

        // --- 2. 차선 변경 (가끔, 고유 선호 라인 근처에서만) ---
        laneChangeTimer -= Time.deltaTime;
        if (laneChangeTimer <= 0f)
        {
            // 고유 라인 중심에서 ±0.3 범위에서만 미세 변경
            float newOffset = personalLanePreference + Random.Range(-0.3f, 0.3f);
            laneOffset = Mathf.Clamp(newOffset, -1.2f, 1.2f);
            laneChangeTimer = Random.Range(8.0f, 15.0f);
        }

        // --- 3. 끼임 감지 (2가지 조건) ---
        if (kart.isControlled)
        {
            // A) 속도가 거의 0 → 완전 정지 상태 (4초 후 구조)
            if (kart.CurrentSpeed < 2.0f)
            {
                stuckTimer += Time.deltaTime;
            }
            else
            {
                stuckTimer = 0f;
            }

            // B) 정면 센서가 벽을 계속 감지 → 벽에 박혀서 진동 중 (3초 후 구조)
            // 속도와 무관! 벽에서 튕기면서 속도 > 2일 수 있지만 센서가 계속 벽을 봄
            Vector3 frontCheck = transform.position + Vector3.up * 1.0f;
            bool frontWall = CastRay(frontCheck, 0, 4.0f, out _); // 짧은 거리(4m)로 "코앞의 벽" 감지
            if (frontWall)
            {
                wallProximityTimer += Time.deltaTime;
            }
            else
            {
                wallProximityTimer = 0f;
            }

            // 어느 조건이든 충족하면 구조
            if (stuckTimer > 4.0f || wallProximityTimer > 3.0f)
            {
                StartCoroutine(LakituRescue());
                return;
            }
        }

        // --- 4. 조향 ---
        Vector3 localTarget = transform.InverseTransformPoint(myTargetPos);
        float targetTurn = localTarget.x / localTarget.magnitude;

        // --- 5. 센서 회피 ---
        float avoidTurn = 0f;
        float avoidanceFactor = 0f;

        Vector3 sensorPos = transform.position + Vector3.up * 1.0f;

        if (CastRay(sensorPos, 0, sensorLength, out _)) { avoidTurn += (targetTurn > 0 ? 1.0f : -1.0f); avoidanceFactor = 1.0f; }
        if (CastRay(sensorPos, frontSensorAngle, sensorLength, out _)) { avoidTurn -= 0.5f; if (avoidanceFactor == 0) avoidanceFactor = 0.5f; }
        if (CastRay(sensorPos, -frontSensorAngle, sensorLength, out _)) { avoidTurn += 0.5f; if (avoidanceFactor == 0) avoidanceFactor = 0.5f; }
        if (CastRay(sensorPos, sideSensorAngle, sensorLength * 0.5f, out _)) { avoidTurn -= 1.0f; }
        if (CastRay(sensorPos, -sideSensorAngle, sensorLength * 0.5f, out _)) { avoidTurn += 1.0f; }

        float finalTurn = (avoidTurn != 0) ? avoidTurn * avoidMultiplier : targetTurn;
        currentSteer = Mathf.Lerp(currentSteer, finalTurn, Time.deltaTime * 5.0f);

        // --- 6. 아이템 ---
        bool wantUseItem = false;
        if (inventory != null && inventory.hasItem && !inventory.isRolling)
        {
            itemTimer -= Time.deltaTime;
            if (itemTimer <= 0 && avoidanceFactor == 0)
            {
                wantUseItem = true;
                itemTimer = Random.Range(itemUseDelayMin, itemUseDelayMax);
            }
        }

        // --- 7. 속도 ---
        float finalSpeed = speedFactor * personalSpeedVariation;

        if (avoidanceFactor > 0) finalSpeed *= 0.5f;  // 벽 근처: 더 많이 감속 (0.6→0.5)
        else if (Mathf.Abs(currentSteer) > 0.6f) finalSpeed *= 0.9f;

        finalSpeed = Mathf.Max(finalSpeed, 0.3f);

        kart.SetInput(finalSpeed, currentSteer, false, wantUseItem);
    }

    bool CastRay(Vector3 pos, float angle, float dist, out float hitDistance)
    {
        Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;

        if (Physics.Raycast(pos, dir, out RaycastHit hit, dist, obstacleLayer))
        {
            Debug.DrawLine(pos, hit.point, Color.red);
            hitDistance = hit.distance;
            return true;
        }
        else
        {
            Debug.DrawRay(pos, dir * dist, Color.green);
            hitDistance = dist;
            return false;
        }
    }

    // =============================================
    // 라쿠투 구조 시스템
    // =============================================
    System.Collections.IEnumerator LakituRescue()
    {
        isRescuing = true;

        int rescueNode = currentNode;
        int nextNode = (rescueNode + 1) % nodes.Count;
        Vector3 rescuePos = nodes[rescueNode].position + Vector3.up * 1.5f;
        Vector3 lookDir = (nodes[nextNode].position - nodes[rescueNode].position).normalized;

        kart.ResetStatus();

        transform.position = rescuePos;
        if (lookDir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(lookDir);
        }

        currentNode = nextNode;
        laneOffset = personalLanePreference;
        stuckTimer = 0f;
        wallProximityTimer = 0f;

        yield return new WaitForSeconds(0.5f);

        isRescuing = false;
    }
}
