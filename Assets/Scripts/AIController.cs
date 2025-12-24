using UnityEngine;
using System.Collections.Generic;

public class AIController : MonoBehaviour
{
    [Header("경로 설정")]
    public Transform pathRoot; // 녹화했거나 배치한 점들의 부모

    [Header("AI 성격 설정")]
    public float steeringSensitivity = 1.0f; // 핸들링 민감도
    public float laneOffset = 0f;            // 0이면 중앙, -면 왼쪽, +면 오른쪽으로 달림
    public float speedFactor = 1.0f;         // 기본 실력 (0.8 = 느림, 1.2 = 빠름)

    [Header("지능 설정")]
    public float driftThreshold = 0.6f; // 핸들을 이만큼(0.6) 이상 꺾으면 드리프트 함
    public float itemUseDelayMin = 1.0f; // 아이템 먹고 최소 1초 뒤 사용
    public float itemUseDelayMax = 5.0f; // 최대 5초 안에 사용

    [Header("회피 센서")]
    public float sensorLength = 10.0f;
    public float sensorAngle = 30.0f;
    public float avoidMultiplier = 2.0f;

    private KartController kart;
    private InventoryManager inventory; // 아이템 확인용
    private List<Transform> nodes = new List<Transform>();
    private int currentNode = 0;

    private float nodeStuckTimer = 0f;
    private float itemCooldown = 0f; // 아이템 사용 타이머
   
    public float rearSensorLength = 15.0f; // 후방 센서 설정

    private bool aiIsDriftingState = false; // AI가 현재 드리프트 중인지 기억하는 변수
    private float currentSteer = 0f; // 부드러운 핸들링을 위한 변수

    void Start()
    {
        kart = GetComponent<KartController>();
        inventory = GetComponent<InventoryManager>();
        kart.isAI = true;

        // 1. 경로 데이터 가져오기
        if (pathRoot != null)
        {
            foreach (Transform child in pathRoot) nodes.Add(child);
        }

        // 2. 랜덤 차선 선택 (도로폭이 10m라고 가정할 때 -3 ~ +3 사이로 랜덤)
        // 이렇게 하면 AI마다 달리는 라인이 달라져서 "사람"처럼 보임
        laneOffset = Random.Range(-3.0f, 3.0f);

        // 3. 약간의 실력 차이 부여
        speedFactor = Random.Range(0.9f, 1.1f);
        
        itemCooldown = Random.Range(itemUseDelayMin, itemUseDelayMax); // 아이템 사용 시간 랜덤 설정
    }

    void Update()
    {
        if (nodes.Count == 0) return;

        // 1. 목표 지점 계산 (나만의 라인 만들기)
        // 그냥 점(position)으로 가는 게 아니라, 점의 오른쪽/왼쪽(Right * laneOffset)을 목표로 삼음
        Vector3 targetNodePos = nodes[currentNode].position;
        Vector3 myTargetPos = targetNodePos + (nodes[currentNode].right * laneOffset);

        // 2. 웨이포인트 갱신 로직
        float distToTarget = Vector3.Distance(transform.position, myTargetPos);
        int nextIndex = (currentNode + 1) % nodes.Count;
        Vector3 nextNodePos = nodes[nextIndex].position + (nodes[nextIndex].right * laneOffset);
        float distToNext = Vector3.Distance(transform.position, nextNodePos);

        nodeStuckTimer += Time.deltaTime; // 타이머 흐름

        // 현재 목표보다 다음 목표가 더 가까우면 지나친 걸로 간주
        if (distToNext < distToTarget || distToTarget < 10.0f || nodeStuckTimer > 2.5f)
        {
            currentNode = nextIndex;
            nodeStuckTimer = 0f; // 타이머 리셋

            // 코너 돌 때마다 랜덤하게 차선을 아주 살짝 바꿈 (더 자연스럽게)
            laneOffset += Random.Range(-0.5f, 0.5f);
            laneOffset = Mathf.Clamp(laneOffset, -3.0f, 3.0f); // 도로 밖으로 안 나가게 제한
        }

        // 3. 핸들링 계산
        Vector3 localTarget = transform.InverseTransformPoint(myTargetPos);
        float targetTurn = localTarget.x / localTarget.magnitude;

        // 4. 장애물 회피 (센서)
        float avoidTurn = 0f;
        bool obstacleDetected = false;
        Vector3 sensorPos = transform.position + Vector3.up * 0.5f;

        // 센서 로직 (기존과 동일)
        if (Physics.Raycast(sensorPos, Quaternion.Euler(0, sensorAngle, 0) * transform.forward, sensorLength))
        {
            avoidTurn -= 1.0f; obstacleDetected = true;
        }
        if (Physics.Raycast(sensorPos, Quaternion.Euler(0, -sensorAngle, 0) * transform.forward, sensorLength))
        {
            avoidTurn += 1.0f; obstacleDetected = true;
        }
        if (Physics.Raycast(sensorPos, transform.forward, sensorLength))
        {
            avoidTurn += (targetTurn > 0 ? 1.0f : -1.0f); obstacleDetected = true;
        }

        float finalTurn = obstacleDetected ? avoidTurn * avoidMultiplier : targetTurn;

        // 5. 드리프트 판단
        // 회전값(finalTurn)이 임계치(driftThreshold)보다 크면 드리프트 시도!
        // 단, 장애물 회피 중일 때는 드리프트 끄기 (안정성 확보)
        if (!obstacleDetected)
        {
            float absTurn = Mathf.Abs(currentSteer);

            if (aiIsDriftingState)
            {
                // [유지 조건] 이미 드리프트 중이라면?
                // 핸들이 거의 직선(0.3 미만)으로 풀릴 때까지는 계속 드리프트 해라!
                if (absTurn < 0.3f)
                {
                    aiIsDriftingState = false; // 이제 그만
                }
            }
            else
            {
                // [시작 조건] 드리프트 중이 아니라면?
                // 핸들을 깊게(0.6 이상) 꺾었을 때만 시작해라!
                if (absTurn > driftThreshold)
                {
                    aiIsDriftingState = true; // 시작!
                }
            }
        }
        else
        {
            // 장애물 피할 땐 드리프트 끔
            aiIsDriftingState = false;
        }

        // 6. 아이템 사용 판단
        bool wantUseItem = false;
        if (inventory != null && inventory.hasItem && !inventory.isRolling)
        {
            InventoryManager.ItemType item = inventory.currentItem; // 현재 가지고 있는 아이템이 뭔지 확인

            switch (item)
            {
                case InventoryManager.ItemType.Mushroom:
                    // [조건] 핸들을 거의 안 꺾었을 때(직선) + 앞에 장애물이 없을 때
                    if (Mathf.Abs(targetTurn) < 0.2f && !obstacleDetected)
                    {
                        wantUseItem = true;
                    }
                    break;

                case InventoryManager.ItemType.Banana:
                    // [조건] 내 뒤에 누군가(Player 또는 다른 AI) 있을 때
                    // 뒤쪽으로 레이저 발사
                    if (Physics.Raycast(transform.position, -transform.forward, rearSensorLength))
                    {
                        wantUseItem = true;
                    }
                    break;
            }
        }

        // 7. 속도 조절
        float finalSpeed = speedFactor;
        // 드리프트 중이거나 코너면 감속 안 함 (오히려 밟음), 장애물 있으면 감속
        if (obstacleDetected) finalSpeed *= 0.6f;

        // 입력 전달 (드리프트와 아이템 사용 여부도 같이 보냄)
        kart.SetInput(finalSpeed, finalTurn, aiIsDriftingState, wantUseItem);
    }
    void OnDrawGizmos()
    {
        if (nodes.Count > 0)
        {
            Gizmos.color = Color.red;
            // 현재 목표에 빨간 공
            Vector3 target = nodes[currentNode].position + (nodes[currentNode].right * laneOffset);
            Gizmos.DrawSphere(target, 1.0f);
            // AI에서 목표까지 선 긋기
            Gizmos.DrawLine(transform.position, target);
        }
    }
}