using UnityEngine;
using System.Collections;

public class KartController : MonoBehaviour
{
    [Header("기본 설정")]
    public bool isAI = false; // 이게 체크되면 키보드 입력 무시
    public bool isControlled = true; // 카트 조작 가능 여부 (기본값은 false로 해서 시작하자마자 못 움직이게)

    [Header("순위 계산용 (자동 설정)")]
    public Transform[] trackNodes; // 트랙의 모든 점들
    public int currentNodeIndex = 0; // 내가 지금 몇 번째 점 근처인가

    [Header("상태 이상")]
    public bool isFlattened = false; // 납작해졌는가?
    private Vector3 defaultScale;    // 원래 크기 기억용

    [Header("이펙트 (파티클)")]
    public ParticleSystem[] driftParticles; // 왼쪽, 오른쪽 2개니까 배열로
    public ParticleSystem boostParticle;    // 부스터 불꽃

    [Header("카트 스탯 (개성 설정)")]
    public float acceleration = 40f;     // 가속력 (클수록 빨리 출발)
    public float maxSpeed = 20f;         // 최고 속도 (이 이상 안 빨라짐)
    public float turnSpeed = 100f;       // 핸들링
    public float weight = 1000f;         // 무게 (무거울수록 몸싸움 승리)

    [Header("아케이드 물리 설정 (중요)")]
    public float gravityMultiplier = 80.0f; // 기본 중력의 3배! (빠른 착지용)
    public float drag = 0.2f;
    public LayerMask groundLayer; // 바닥만 감지하기 위한 레이어 설정

    [Header("드리프트")]
    public float driftBaseTurnSpeed = 100f; // 드리프트 중 기본 회전 속도
    public float driftSharpFactor = 1.5f;   // 안쪽으로 꺾을 때 (더 날카롭게)
    public float driftWideFactor = 0.5f;    // 바깥쪽으로 꺾을 때 (더 넓게)
    public float jumpForce = 2.5f;
    public float minDriftTime = 0.8f;
    private float jumpCooldown = 0f;  // 점프 연타 방지용 쿨타임

    [Header("부스터")]
    public float boostForce = 100f;      // 순간 가속 힘
    public float boostMaxSpeed = 40f;    // 부스터 시 허용되는 최고 속도
    public float absoluteMaxSpeed = 45f; // 절대 한계 속도 (아무리 빨라도 이 이상은 절대 안 됨! 300km/h 방지용)
    public float boostWindow = 0.5f;     // 입력 대기 시간
    public float boostDeceleration = 25f;    //부스터 끝난 후 감속 속도(클수록 빨리 원래 속도로 돌아옴) 
    public float instantBoostDuration = 0.5f; // 드리프트 순간 부스터 (짧게!)
    public float padBoostDuration = 2.5f;     // 발판 부스터 (길게!)
    public float driftDecel = 1.0f;  // 드리프트: 빨리 원래 속도로 복귀 (탁! 끊기는 맛)
    public float padDecel = 0.5f;     // 발판: 아주 천천히 줄어듦 (여운)

    private float currentBoostDecel; // 현재 적용 중인 감속 수치
    public float groundCheckRadius = 0.5f; // 바닥 인식용 반지름

    [Header("충돌")]
    public float bounceForce = 0.6f;
    public float minBounceForce = 5f;

    [Header("레이스 상태 (자동 관리)")]
    public int currentLap = 1;
    public int nextCheckpointIndex = 0;

    [Header("피격 설정 (미사일)")]
    public float hitJumpHeight = 1.5f; // 점프 높이
    public float hitDuration = 0.8f;   // 구르는 시간 (짧을수록 빠름)
    public int flipCount = 1;          // 몇 바퀴 구를지 (보통 1)

    // 드리프트/아이템 입력 신호 (AI가 이걸 true로 만들면 작동)
    public bool isDriftInput = false;
    public bool isItemUseInput = false;

    private Rigidbody rb;
    private Renderer kartRenderer;
    private Color originalColor;

    private float moveInput;
    public float turnInput;

    private bool isDrifting = false;
    private float driftTimer = 0f;

    private float driftDirection = 0f;  // 드리프트 방향 기억 (-1: 왼쪽, 1: 오른쪽, 0: 없음)

    private bool canInstantBoost = false;
    private float storedBoostPower = 0f;

    private bool isGrounded;
    private float currentMaxSpeed; // 부스터 중에는 최고 속도 제한을 늘려주기 위한 변수

    private bool isBoosting = false; // 부스터 사용 중인지 체크
    private bool isUncontrollable = false; // 조작 불능 상태 체크
   
    private bool hasPassedStartLine = false; // 출발선을 통과한 적이 있는가?
    public float CurrentSpeed { get { return rb.linearVelocity.magnitude; } }
    public bool IsBoosting { get { return isBoosting; } }

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // 1. 무게 적용 (Rigidbody의 Mass를 스탯에 맞춤)
        rb.mass = weight;
        rb.linearDamping = drag;
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
        rb.useGravity = true;

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        defaultScale = transform.localScale; // 시작할 때 원래 크기를 기억해둠
        kartRenderer = GetComponent<Renderer>();
        if (kartRenderer != null) originalColor = kartRenderer.material.color;

        currentBoostDecel = driftDecel;
    }

    void Update()
    {
        // 1. 플레이어라면 키보드 입력 받음 
        if (isControlled && !isUncontrollable)
        {
            if (!isAI)
            {
                moveInput = Input.GetAxis("Vertical");
                turnInput = Input.GetAxis("Horizontal");
            }
        }
        // 2. 카운트다운 등으로 조작이 잠겨있으면(isControlled == false) 입력 0
        else
        {
            moveInput = 0;
            turnInput = 0;
        }
        // 3. AI라면 여기서 아무것도 안 함 (AIController가 넣어줄 값을 기다림)

        if (jumpCooldown > 0) jumpCooldown -= Time.deltaTime; // 쿨타임 감소

        CheckGrounded();
        HandleDrift();
        HandleInstantBoost();
        UpdateEffects();
        CalculateProgress();

        // 공중에서 강제로 자세 바로잡기
        if (!isGrounded)
        {
            // 천천히 원래 각도(X=0, Z=0)로 돌아오게 함
            float yRot = transform.rotation.eulerAngles.y;
            Quaternion upright = Quaternion.Euler(0, yRot, 0);
            transform.rotation = Quaternion.Lerp(transform.rotation, upright, Time.deltaTime * 2.0f);
        }
    }
    void FixedUpdate()
    {
        Move();
        ApplyCustomGravity();
    }
    // AI가 입력 넣는 함수
    public void SetInput(float move, float turn, bool drift, bool itemUse)
    {
        if (!isAI) return;
        moveInput = move;
        turnInput = turn;
        isDriftInput = drift;
        isItemUseInput = itemUse;
    }
    //내가 경로상 어디쯤인지 계산
    void CalculateProgress()
    {
        if (trackNodes == null || trackNodes.Length == 0) return;

        // 현재 알고 있는 내 위치(currentNodeIndex)에서부터
        // 앞뒤로 5개 정도만 검사해서 가장 가까운 점을 찾음 (최적화)
        // (처음엔 전체 탐색, 이후엔 주변 탐색)

        float minDst = Mathf.Infinity;
        int bestIndex = currentNodeIndex;

        // 전체를 다 뒤지면 무거우니까, 현재 인덱스 기준으로 앞뒤 5개씩만 검사
        for (int i = 0; i < trackNodes.Length; i++)
        {
            float dst = Vector3.Distance(transform.position, trackNodes[i].position);
            if (dst < minDst)
            {
                minDst = dst;
                bestIndex = i;
            }
        }

        // 역주행 방지 로직 (갑자기 0번에서 50번으로 점프하면 안됨)
        // 정상 주행이라면 인덱스가 조금씩 늘어나야 함.
        // 하지만 여기서는 단순하게 "가장 가까운 점"을 내 위치로 잡음.
        currentNodeIndex = bestIndex;
    }
    void CheckGrounded()
    {
        // groundLayer에 포함된 것만 바닥으로 인식! (벽 타고 오르는 거 방지)
        RaycastHit hit;
        if (Physics.SphereCast(transform.position + Vector3.up * 0.5f, 0.5f, -transform.up, out hit, 1.2f, groundLayer))
        {
            isGrounded = true;

            // 바닥 경사에 맞춰 회전 (단, 벽이 아닐 때만)
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
        }
        else
        {
            isGrounded = false;
        }
    }
    void ApplyCustomGravity()
    {
        if (!isGrounded)
        {
            rb.linearDamping = 0f;
            rb.AddForce(Vector3.down * gravityMultiplier * rb.mass, ForceMode.Acceleration);
        }
        else
        {
            rb.linearDamping = drag;
        }
    }
    public void PassCheckpoint(int index, int totalCheckpoints)
    {
        // 순서가 맞는지 확인
        if (index == nextCheckpointIndex)
        {
            Debug.Log(gameObject.name + " 체크포인트 " + index + " 통과!");

            nextCheckpointIndex++;
            if (nextCheckpointIndex >= totalCheckpoints)
            {
                nextCheckpointIndex = 0;
            }

            // 0번(출발선) 통과 시 처리
            if (index == 0)
            {
                // 게임 시작 후 어느 정도 시간이 지났을 때만 랩 증가 (초반 버그 방지)
                // Time.timeSinceLevelLoad 등을 써도 되지만 간단하게 체크
                if (!hasPassedStartLine)
                {
                    hasPassedStartLine = true;
                }
                else
                {
                    // 두 번째 통과부터 랩 증가
                    currentLap++;
                }
            }

            // [중요] 내가 플레이어라면? UI 업데이트 요청!
            if (!isAI)
            {
                GameManager gm = FindFirstObjectByType<GameManager>();
                if (gm != null) gm.UpdateLapUI(currentLap);
            }
        }
    }
    void UpdateEffects()
    {
        // 1. 드리프트 파티클 제어
        // 땅에 붙어있고 + 드리프트 중이고 + 속도가 좀 있을 때만 연기 남
        if (isGrounded && isDrifting && rb.linearVelocity.magnitude > 5f)
        {
            foreach (var p in driftParticles)
            {
                if (!p.isPlaying) p.Play();
            }
        }
        else
        {
            foreach (var p in driftParticles)
            {
                if (p.isPlaying) p.Stop();
            }
        }

        // 2. 부스터 파티클 제어
        if (isBoosting)
        {
            if (!boostParticle.isPlaying) boostParticle.Play();
        }
        else
        {
            if (boostParticle.isPlaying) boostParticle.Stop();
        }
    }
    public void SlipAndSpin(float duration)
    {
        if (isUncontrollable) return; // 이미 돌고 있으면 무시
        StartCoroutine(SpinRoutine(duration));
    }

    System.Collections.IEnumerator SpinRoutine(float duration)
    {
        isUncontrollable = true; // 입력 차단

        // 1. 드리프트/부스터 강제 종료
        isDrifting = false;
        isBoosting = false;
        canInstantBoost = false;
        StopCoroutine("BoostRoutine");
        if (kartRenderer != null) kartRenderer.material.color = originalColor;

        // 2. 회전 변수 설정
        float elapsed = 0f;

        // 회전 시작 전, 현재 카트가 나아가던 물리적인 방향을 기억함
        // (속도가 거의 없으면 그냥 현재 앞방향을 유지)
        Vector3 moveDirection = rb.linearVelocity.magnitude > 1f ? rb.linearVelocity.normalized : transform.forward;
        moveDirection.y = 0; // 위아래 기울기는 무시

        // 시작 각도
        Quaternion startRotation = transform.rotation;

        // 목표 각도: 진행 방향을 바라보는 각도 + 360도 (한 바퀴 뺑 돌기)
        float totalSpinAmount = 720f;

        // 3. 부드럽게 회전 시키기 (애니메이션처럼)
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // 0 ~ 1 사이 진행률 (비율)
            float percent = elapsed / duration;

            // EaseOut 곡선 적용 (처음엔 빠르고 끝엔 부드럽게 멈춤) - 선택사항이지만 넣으면 고급짐
            // 그냥 percent를 쓰면 일정한 속도로 돕니다.
            float curvePercent = Mathf.Sin(percent * Mathf.PI * 0.5f);

            // 현재 회전 각도 계산 (기본 방향에서 curvePercent만큼 360도 회전)
            // AngleAxis를 사용해 Y축 기준으로 회전
            Quaternion spin = Quaternion.AngleAxis(totalSpinAmount * curvePercent, Vector3.up);

            // 진행 방향(moveDirection)을 기준으로 회전을 더해줌
            // 이렇게 하면 카트가 미끄러지는 방향을 보면서 뱅글 돕니다.
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection) * spin;

            transform.rotation = targetRotation;

            yield return null;
        }

        // 4. 끝난 후 확실하게 정면(진행방향) 보게 하기
        if (rb.linearVelocity.magnitude > 1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity.normalized);
        }

        // 5. 복귀
        isUncontrollable = false;
        rb.angularVelocity = Vector3.zero; // 물리 회전값 초기화
    }
    void HandleDrift()
    {
        if (isUncontrollable) return; // AI는 드리프트 로직 패스

        // Player는 키보드, AI는 변수(isDriftInput)로 판단
        bool driftInput = isAI ? isDriftInput : Input.GetKey(KeyCode.LeftShift);

        // 1. 드리프트 시작 (조건: 드리프트 키 + 땅 + 전진 중 + 핸들 꺾음)
        // AI의 경우 isDriftInput이 true로 바뀌는 순간이 GetKeyDown과 유사하게 처리되도록 로직 보완이 필요하지만
        // 간단하게는 '현재 드리프트 중이 아닌데 드리프트 키가 들어오면' 시작으로 봄.
        if (!isDrifting && driftInput && isGrounded && moveInput > 0 && Mathf.Abs(turnInput) > 0.3f && jumpCooldown <= 0)
        {
            isDrifting = true;
            driftTimer = 0f;
            jumpCooldown = 0.5f;
            driftDirection = Mathf.Sign(turnInput);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }

        // 2. Shift 누르고 있는 중: 게이지 충전 (색깔 변화)
        if (isDrifting && driftInput && isGrounded)
        {
            driftTimer += Time.deltaTime; // 시간 누적
            
            if (kartRenderer != null) // 시각 효과: 오래 누를수록 빨갛게 변하게 (충전 느낌)
            {
                // 0초~maxDriftTime 사이의 값을 0~1로 변환해서 색깔 섞기
                float lerpVal = Mathf.Clamp01(driftTimer / 2.0f);
                kartRenderer.material.color = Color.Lerp(originalColor, Color.red, lerpVal);
            }
        }
        else if (isDrifting && moveInput <= 0) 
        {
            StopDrift(); // 속도 줄거나 멈추면 취소
        }

        // 3. Shift 뗀 순간: 부스터 발사!
        if (isDrifting && !driftInput)
        {
            StopDrift();

            // 드리프트 시간이 최소 시간(1초)을 넘겼다면 부스터 발동!
            if (driftTimer >= minDriftTime)
            {
                OpenBoostWindow();

                if (isAI) HandleInstantBoost();  // AI는 기회가 생기자마자 즉시 발동
            }
        }
    }
    void StopDrift()
    {
        isDrifting = false;
        driftDirection = 0f; // 방향 초기화
        if (kartRenderer != null) kartRenderer.material.color = originalColor;
    }

    // 부스터 타이밍을 열어주는 함수
    void OpenBoostWindow()
    {
        canInstantBoost = true;        
        storedBoostPower = boostForce * Mathf.Clamp(driftTimer, 1f, 2f); // 드리프트 시간에 비례해 힘을 저장해둠 (최대 2배까지만)
        Invoke("CloseBoostWindow", boostWindow); // 정해진 시간(boostWindow)이 지나면 기회 박탈
    }
    // 부스터 기회 종료
    void CloseBoostWindow()
    {
            canInstantBoost = false;
    }
    void HandleInstantBoost()
    {
        if (isUncontrollable) return;

        // 플레이어: 키를 눌러야 발동
        // AI: 기회가 생기면(canInstantBoost) 무조건 즉시 발동
        bool triggerBoost = isAI ? canInstantBoost : (canInstantBoost && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)));

        // 기회가 있고(canInstantBoost), '위쪽 화살표'를 딱! 눌렀을 때(GetKeyDown)
        if (triggerBoost)
        {
            ActivateBoost(storedBoostPower, instantBoostDuration, driftDecel);
            canInstantBoost = false;
            CancelInvoke("CloseBoostWindow");
        }
    }
    void FireBoost()
    {
        // 부스터 쓸 때는 최고 속도 제한을 잠시 풀어줌
        StartCoroutine(BoostSpeedUpRoutine(2f)); // 2초간 속도 제한 해제

        // 저장해둔 힘만큼 앞으로 발사!
        rb.AddForce(transform.forward * storedBoostPower, ForceMode.Impulse);

        // 부스터 사용했으니 상태 초기화
        canInstantBoost = false;
        CancelInvoke("CloseBoostWindow"); // 예약된 종료 취소
        Debug.Log("순간 부스터 발동!!!");
    }

    void Move()
    {
        float speedPenalty = isFlattened ? 0.4f : 1.0f;
        float currentAccel = moveInput * acceleration * speedPenalty;
        if (moveInput < 0) currentAccel *= 0.5f;

        // 힘 적용
        rb.AddForce(transform.forward * currentAccel * rb.mass, ForceMode.Force);

        // 1. 현재 상황에 맞는 목표 한계 속도 정하기
        float currentSpeed = rb.linearVelocity.magnitude;
        float targetLimit = isBoosting ? boostMaxSpeed : maxSpeed;
        if (isFlattened && !isBoosting) targetLimit *= speedPenalty;

        // 2. 현재 속도가 목표 한계보다 빠른 경우 (예: 부스터 끝난 직후)
        if (currentSpeed > targetLimit)
        {
            // 강제로 깎지 않고, boostDeceleration만큼 서서히(하지만 빠르게) 줄여나감
            // Mathf.MoveTowards는 현재값에서 목표값으로 일정량만큼 이동시킴
            float newSpeed = Mathf.Lerp(currentSpeed, targetLimit, currentBoostDecel * Time.fixedDeltaTime);

            // 방향은 유지한 채 속도 크기만 줄임
            rb.linearVelocity = rb.linearVelocity.normalized * newSpeed;
        }
        // 3. 절대 한계 속도 (Hard Cap)
        // 부스터+발판 중첩으로 속도가 300이 되더라도, 여기서 강제로 absoluteMaxSpeed로 고정함
        if (rb.linearVelocity.magnitude > absoluteMaxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * absoluteMaxSpeed;
        }

        float finalTurnFactor = 0f;

        if (isDrifting)
        {
            // 드리프트 중일 때: 입력 키가 드리프트 방향과 같은지 다른지 확인
            // Mathf.Sign을 써서 방향 부호(-1, 1)를 비교
            bool isSteeringSameDir = Mathf.Sign(turnInput) == driftDirection && Mathf.Abs(turnInput) > 0.1f;

            if (isSteeringSameDir)
            {
                // 같은 방향으로 꺾으면 -> 더 날카롭게 회전 (Sharp)
                finalTurnFactor = driftDirection * driftBaseTurnSpeed * driftSharpFactor;
            }
            else
            {
                // 반대 방향으로 꺾거나 안 꺾으면 -> 넓게 회전 (Wide)
                // (driftDirection은 유지하되 속도를 줄임)
                finalTurnFactor = driftDirection * driftBaseTurnSpeed * driftWideFactor;
            }
        }
        else
        {
            // 평소 주행: 입력값 그대로 회전
            finalTurnFactor = turnInput * turnSpeed;
        }

        // 공중에서는 회전 제약
        if (!isGrounded) finalTurnFactor *= 0.5f;

        // 최종 회전 적용
        transform.Rotate(0f, finalTurnFactor * Time.fixedDeltaTime, 0f);
    }
    public void AddExternalBoost(float power)
    {
        ActivateBoost(power, padBoostDuration, padDecel);
    }
    void ActivateBoost(float power, float duration, float decelerationRate)
    {
        StopCoroutine("BoostRoutine"); // 이미 실행 중이면 끄고 다시 시작
        currentBoostDecel = decelerationRate; // 이번 부스터가 끝난 뒤 적용할 감속 속도를 저장
        StartCoroutine(BoostRoutine(power, duration));
    }
    IEnumerator BoostRoutine(float power, float duration)
    {
        isBoosting = true;

        // 1. 순간적인 힘을 팍! 줌 (가속)
        rb.AddForce(transform.forward * power * rb.mass, ForceMode.Impulse);
        // 2. 부스터 지속 시간 동안 최고 속도 제한을 늘림
        yield return new WaitForSeconds(duration);
        // 3. 끝남
        isBoosting = false;
    }
    // 일정 시간 동안 혹은 목표 속도까지 MaxSpeed를 늘려주는 코루틴
    System.Collections.IEnumerator BoostSpeedUpRoutine(float targetLimit)
    {
        // 현재 한계보다 높을 때만 적용
        if (targetLimit > currentMaxSpeed)
        {
            currentMaxSpeed = targetLimit;
        }

        // 1초 뒤부터 서서히 원래 속도로 줄어드는 건 Move()의 Lerp가 처리함
        yield return null;
    }
    // 외부(프레스기)에서 호출할 함수
    public void Squash(float duration)
    {
        if (isFlattened) return; // 이미 납작하면 무시 (시간 초기화하고 싶으면 로직 변경 가능)

        StartCoroutine(SquashRoutine(duration));
    }

    IEnumerator SquashRoutine(float duration)
    {
        isFlattened = true;
        Debug.Log("으악! 납작해졌다!");

        // 1. 납작하게 만들기 (Y축만 0.2배로 줄임)
        transform.localScale = new Vector3(defaultScale.x, defaultScale.y * 0.2f, defaultScale.z);

        // 2. 지속 시간 대기
        yield return new WaitForSeconds(duration);

        // 3. 원래대로 복구 (뿅!)
        transform.localScale = defaultScale;
        isFlattened = false;
    }
    public void ResetStatus()
    {
        // 1. 물리 속도 제거
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 2. 드리프트 정보 초기화
        isDrifting = false;
        driftTimer = 0f;

        // 3. 부스터 정보 초기화
        isBoosting = false;
        canInstantBoost = false;
        storedBoostPower = 0f;
        StopCoroutine("BoostRoutine");
        CancelInvoke("CloseBoostWindow");

        // 4. 시각 효과 초기화
        if (kartRenderer != null) kartRenderer.material.color = originalColor;
        foreach (var p in driftParticles) p.Stop();
        if (boostParticle != null) boostParticle.Stop();

        // 납작 상태 해제
        isFlattened = false;
        transform.localScale = defaultScale;
        StopCoroutine("SquashRoutine");

        Debug.Log("카트 상태 리셋 완료!");
    }
    // 미사일 맞았을 때 호출되는 함수
    public void HitByMissile()
    {
        // 이미 당하고 있거나 무적 상태면 무시
        if (isUncontrollable) return;

        StartCoroutine(HitRoutine());
    }

    IEnumerator HitRoutine()
    {
        isUncontrollable = true;
        ResetStatus();

        rb.isKinematic = true; // 물리 끄기

        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        // 회전의 축이 될 높이 (카트의 절반 높이)
        // 이 값이 클수록 공중제비를 크게 돌고, 작을수록 제자리에서 돕니다.
        float centerOffset = 0.5f;

        float jumpBump = 0.5f;

        while (elapsed < hitDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / hitDuration; // 0 ~ 1

            // 1. 회전 각도
            float currentAngle = t * 360f * flipCount;
            Quaternion rot = Quaternion.Euler(currentAngle, 0, 0);

            // 2.피벗 보정
            Vector3 pivotOffset = new Vector3(0, centerOffset, 0);
            Vector3 rotatedOffset = rot * -pivotOffset;
            Vector3 finalPosOffset = pivotOffset + rotatedOffset;

            // 3. 살짝 튀어오르는 느낌 추가
            float bump = Mathf.Sin(t * Mathf.PI) * jumpBump;

            // 최종 적용
            transform.rotation = startRot * rot;
            transform.position = startPos + finalPosOffset + (Vector3.up * bump);

            yield return null;
        }

        // 복구 및 착지
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 5.0f, groundLayer))
        {
            transform.position = hit.point;
        }
        else
        {
            transform.position = startPos;
        }

        transform.rotation = startRot;
        rb.isKinematic = false;
        isUncontrollable = false;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    // 충돌이 일어났을 때 자동으로 실행되는 함수
    void OnCollisionEnter(Collision collision)
    {
        // 그냥 태그가 Wall이면 튕김
        if (collision.gameObject.CompareTag("Wall"))
        {   
            float impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed > 5f)
            {
                canInstantBoost = false;
                StopDrift();
                StopCoroutine("BoostRoutine");
                isBoosting = false;

                // 심플하게 반대 방향으로 튕기기
                Vector3 pushDir = collision.contacts[0].normal;
                rb.AddForce(pushDir * bounceForce * impactSpeed, ForceMode.Impulse);

                rb.angularVelocity = Vector3.zero; // 회전 멈춤

                if (!isAI)
                {
                    KartCamera cam = Camera.main.GetComponent<KartCamera>();
                    if (cam != null) cam.Shake(0.2f, Mathf.Clamp(impactSpeed * 0.02f, 0.2f, 0.8f));
                }
            }
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeathZone"))
        {
            // 게임 매니저를 찾아서 리스폰 요청
            GameManager gm = FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                gm.RespawnPlayer(this);
            }
        }
    }
}