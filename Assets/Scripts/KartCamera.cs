using UnityEngine;
using System.Collections;

public class KartCamera : MonoBehaviour
{
    public KartController targetKart;

    [Header("기본 카메라 설정")]
    public float defaultDistance = 3.5f;
    public float defaultHeight = 1.5f;
    public float defaultFOV = 60f;

    [Header("부스터 카메라 설정")]
    public float boostDistance = 4.5f;
    public float boostHeight = 1.5f;
    public float boostFOV = 95f;

    [Header("시선 처리")]
    public float lookAtHeight = 1.0f;

    [Header("반응 속도")]
    public float rotationDamping =5.0f; // 회전 따라가는 속도
    public float heightDamping = 10.0f;   // 높이 따라가는 속도
    public float zoomDamping = 5.0f;      // 줌(FOV/거리) 반응 속도

    // 줌 속도 분리
    public float zoomOutSpeed = 5.0f;  // 멀어질 때 (부드럽게)
    public float zoomInSpeed = 20.0f;  // 돌아올 때 (빠르게!)

    // [핵심] 카메라의 현재 상태를 저장하는 변수 (Transform에서 가져오지 않음!)
    private float currentYAngle;
    private float currentHeight;
    private float currentDistance;

    private Vector3 shakeOffset = Vector3.zero;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (targetKart == null)
            targetKart = FindFirstObjectByType<KartController>();

        // [중요] 시작하자마자 타겟 뒤로 순간이동 (멀리서 날아오는 것 방지)
        if (targetKart != null)
        {
            currentYAngle = targetKart.transform.eulerAngles.y;
            currentHeight = targetKart.transform.position.y + defaultHeight;
            currentDistance = defaultDistance;

            // 초기 위치 강제 설정
            UpdateCameraPosition(true);
        }
    }

    void LateUpdate()
    {
        if (targetKart == null) return;
        UpdateCameraPosition(false);
    }

    void UpdateCameraPosition(bool instant)
    {
        bool isBoosting = targetKart.IsBoosting;

        // 1. 목표값 설정
        float targetDist = isBoosting ? boostDistance : defaultDistance;
        float targetHei = isBoosting ? boostHeight : defaultHeight;
        float targetFOV = isBoosting ? boostFOV : defaultFOV;

        // 2. 시간(DeltaTime) 설정
        float dt = instant ? 1000f : Time.deltaTime;

        // 부스터 중(멀어질 때) -> zoomOutSpeed (천천히 5.0)
        // 끝남(돌아올 때) -> zoomInSpeed (빠르게 20.0)
        float currentZoomSpeed = isBoosting ? zoomOutSpeed : zoomInSpeed;

        // zoomDamping 대신 currentZoomSpeed를 사용합니다.
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, dt * currentZoomSpeed);

        // 거리 계산에도 똑같이 적용
        currentDistance = Mathf.Lerp(currentDistance, targetDist, dt * currentZoomSpeed);

        // 3. 회전과 높이는 기존 Damping 사용
        float wantedRotationAngle = targetKart.transform.eulerAngles.y;
        float wantedHeight = targetKart.transform.position.y + targetHei;

        currentYAngle = Mathf.LerpAngle(currentYAngle, wantedRotationAngle, dt * rotationDamping);
        currentHeight = Mathf.Lerp(currentHeight, wantedHeight, dt * heightDamping);

        // 4. 최종 위치 계산
        Quaternion currentRotation = Quaternion.Euler(0, currentYAngle, 0);

        Vector3 finalPosition = targetKart.transform.position;
        finalPosition -= currentRotation * Vector3.forward * currentDistance;
        finalPosition.y = currentHeight;

        transform.position = finalPosition + shakeOffset;
        transform.LookAt(targetKart.transform.position + Vector3.up * lookAtHeight);
    }

    public void Shake(float duration, float magnitude)
    {
        StopAllCoroutines();
        StartCoroutine(DoShake(duration, magnitude));
    }

    IEnumerator DoShake(float duration, float magnitude)
    {
        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * (magnitude * 0.2f);
            shakeOffset = new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        shakeOffset = Vector3.zero;
    }
}