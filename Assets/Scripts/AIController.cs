using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class AIController : MonoBehaviour
{
    [Header("경로 데이터")]
    public Transform pathRoot;

    [Header("AI 운전 성향")]
    public float steeringSensitivity = 5.0f;
    public float laneOffset = 0f;
    public float speedFactor = 1.0f; // 기본 속도 1.0 (최대)

    [Header("아이템 사용")]
    public float itemUseDelayMin = 1.0f;
    public float itemUseDelayMax = 4.0f;

    [Header("센서 설정")]
    public float sensorLength = 12.0f;
    public float frontSensorAngle = 15.0f;
    public float sideSensorAngle = 45.0f;
    public float avoidMultiplier = 3.0f;

    // 바닥은 감지하지 않기 위한 레이어 마스크
    public LayerMask obstacleLayer;

    private KartController kart;
    private InventoryManager inventory;
    public List<Transform> nodes = new List<Transform>();
    private int currentNode = 0;

    private float nodeStuckTimer = 0f;
    private float itemTimer = 0f;
    private float currentSteer = 0f;
    private bool isReversing = false;

    void Start()
    {
        kart = GetComponent<KartController>();
        inventory = GetComponent<InventoryManager>();
        kart.isAI = true;

        if (pathRoot != null) { foreach (Transform child in pathRoot) nodes.Add(child); }

        laneOffset = Random.Range(-1.0f, 1.0f);
        itemTimer = Random.Range(itemUseDelayMin, itemUseDelayMax);
    }

    void Update()
    {
        if (nodes.Count == 0) return;

        // --- 1. 길찾기 로직 ---
        Vector3 nodePos = nodes[currentNode].position;
        int nextIndex = (currentNode + 1) % nodes.Count;
        Vector3 nextNodePos = nodes[nextIndex].position;

        // 진행 방향 계산
        Vector3 roadDirection = (nextNodePos - nodePos).normalized;
        Vector3 roadRight = Vector3.Cross(Vector3.up, roadDirection).normalized;
        Vector3 myTargetPos = nodePos + (roadRight * laneOffset);

        float distToTarget = Vector3.Distance(transform.position, myTargetPos);
        float distToNext = Vector3.Distance(transform.position, nextNodePos);

        // 점 지나침 체크
        if (distToNext < distToTarget || distToTarget < 12.0f)
        {
            currentNode = nextIndex;
            nodeStuckTimer = 0f;
            laneOffset = Random.Range(-1.0f, 1.0f);
        }

        // --- 2. 후진 로직 (끼임 탈출) ---
        // 속도가 거의 없는데(1.0 미만) 전진하려고 하면 끼인 것
        if (kart.CurrentSpeed < 1.0f && !isReversing)
        {
            nodeStuckTimer += Time.deltaTime;
            if (nodeStuckTimer > 2.0f) StartCoroutine(ReverseRoutine());
        }
        else nodeStuckTimer = 0f;

        if (isReversing)
        {
            // 후진할 때는 핸들을 반대로 꺾지 말고, 그냥 랜덤하게 꺾어서 탈출 시도
            kart.SetInput(-1.0f, Random.Range(-1f, 1f), false, false);
            return;
        }

        // --- 3. 핸들링 ---
        Vector3 localTarget = transform.InverseTransformPoint(myTargetPos);
        float targetTurn = localTarget.x / localTarget.magnitude;

        // --- 4. 센서 로직 (바닥 무시 & 높이 조절) ---
        float avoidTurn = 0f;
        float avoidanceFactor = 0f;

        // 센서 위치를 카트 바닥이 아니라 '눈높이(1.0m)'로 올림
        // 너무 낮으면 오르막길을 벽으로 인식함
        Vector3 sensorPos = transform.position + Vector3.up * 1.0f;

        if (CastRay(sensorPos, 0, sensorLength, out _)) { avoidTurn += (targetTurn > 0 ? 1.0f : -1.0f); avoidanceFactor = 1.0f; }
        if (CastRay(sensorPos, frontSensorAngle, sensorLength, out _)) { avoidTurn -= 0.5f; if (avoidanceFactor == 0) avoidanceFactor = 0.5f; }
        if (CastRay(sensorPos, -frontSensorAngle, sensorLength, out _)) { avoidTurn += 0.5f; if (avoidanceFactor == 0) avoidanceFactor = 0.5f; }
        if (CastRay(sensorPos, sideSensorAngle, sensorLength * 0.5f, out _)) { avoidTurn -= 1.0f; }
        if (CastRay(sensorPos, -sideSensorAngle, sensorLength * 0.5f, out _)) { avoidTurn += 1.0f; }

        float finalTurn = (avoidTurn != 0) ? avoidTurn * avoidMultiplier : targetTurn;
        currentSteer = Mathf.Lerp(currentSteer, finalTurn, Time.deltaTime * 5.0f);

        // --- 5. 아이템 ---
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

        // --- 6. 속도 조절 (과감하게!) ---
        float finalSpeed = speedFactor;

        // 장애물이 있어도 속도를 너무 줄이지 않음 (최소 0.8 유지)
        // 속도가 있어야 핸들이 먹혀서 피할 수 있음!
        if (avoidanceFactor > 0) finalSpeed *= 0.8f;
        else if (Mathf.Abs(currentSteer) > 0.6f) finalSpeed *= 0.9f;

        kart.SetInput(finalSpeed, currentSteer, false, wantUseItem);
    }

    bool CastRay(Vector3 pos, float angle, float dist, out float hitDistance)
    {
        Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;

        // [수정] obstacleLayer 마스크를 추가해서 '바닥'은 무시하고 '벽'만 감지
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

    System.Collections.IEnumerator ReverseRoutine()
    {
        isReversing = true;
        yield return new WaitForSeconds(1.5f);
        isReversing = false;
        nodeStuckTimer = 0f;
    }
}