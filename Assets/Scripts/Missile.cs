using UnityEngine;
using System.Linq; // 리스트 정렬을 위해 필요

public class Missile : MonoBehaviour
{
    [Header("기본 설정")]
    public float speed = 50f;
    public float lifeTime = 5.0f;

    [Header("유도 설정 (빨간 등껍질)")]
    public bool isHoming = true;      // 유도 기능을 켤지 말지
    public float turnSpeed = 20.0f;    // 회전 속도 (클수록 잘 꺾음)
    public float searchAngle = 45.0f; // 전방 탐색 각도 (너무 옆에 있는 건 무시)

    [HideInInspector] public KartController ownerScript; // 주인 스크립트
    private Transform target; // 추적할 목표

    void Start()
    {
        Destroy(gameObject, lifeTime);

        // 1. 타겟 설정 (내 앞 등수)
        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm != null && ownerScript != null)
        {
            KartController targetKart = gm.GetTargetFor(ownerScript);
            if (targetKart != null)
            {
                target = targetKart.transform;
                Debug.Log("미사일 타겟: " + target.name);
            }
            else
            {
                Debug.Log("타겟 없음 (1등이거나 오류)");
            }
        }
    }

    void Update()
    {
        // 1. 유도 로직
        if (target != null)
        {
            // 목표 방향 계산
            Vector3 dir = target.position - transform.position;
            dir.Normalize();

            // 캡슐 앞방향 벡터(transform.up)를 목표 방향으로 회전
            Quaternion targetRot = Quaternion.FromToRotation(transform.up, dir) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        // 2. 전진
        transform.Translate(Vector3.up * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (ownerScript != null && other.gameObject == ownerScript.gameObject) return;
        if (other.isTrigger) return;

        KartController hitKart = other.GetComponent<KartController>();
        if (hitKart != null)
        {
            hitKart.HitByMissile();
            Explode();
        }
        else if (other.CompareTag("Wall") || other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Explode();
        }
    }

    void Explode()
    {
        // 폭발 이펙트 생성 (있으면)
        // Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}