using UnityEngine;
using System.Collections;

public class Crusher : MonoBehaviour
{
    [Header("움직임 설정")]
    public float downSpeed = 15.0f; // 더 빠르게 찍음
    public float upSpeed = 5.0f;    // 올라가는 것도 좀 빠르게 (속박 탈출)
    public float waitTimeBottom = 0.2f; // 바닥에서 아주 잠깐만 머뭄!
    public float waitTimeTop = 2.0f;
    public float moveDistance = 5.0f;

    [Header("공격 설정")]
    public float squashDuration = 5.0f; // 납작해지는 시간 길게 (5초)

    private Vector3 startPos;
    private Vector3 bottomPos;
    private bool isGoingDown = false; // 지금 내려찍는 중인가?

    void Start()
    {
        startPos = transform.position;
        bottomPos = startPos - new Vector3(0, moveDistance, 0);
        StartCoroutine(CrushRoutine());
    }

    IEnumerator CrushRoutine()
    {
        while (true)
        {
            // 1. 내려찍기
            isGoingDown = true; // 공격 시작
            while (Vector3.Distance(transform.position, bottomPos) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, bottomPos, downSpeed * Time.deltaTime);
                yield return null;
            }
            isGoingDown = false; // 바닥 도착하면 공격 판정 끝 (이미 깔린 상태)

            // 2. 바닥 대기 (아주 짧게)
            yield return new WaitForSeconds(waitTimeBottom);

            // 3. 올라가기
            while (Vector3.Distance(transform.position, startPos) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, startPos, upSpeed * Time.deltaTime);
                yield return null;
            }

            // 4. 위에서 대기
            yield return new WaitForSeconds(waitTimeTop);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        KartController kart = collision.gameObject.GetComponent<KartController>();

        if (kart != null)
        {
            // "옆에서 닿았냐, 밑에서 깔렸냐?" 판별

            // 1. 프레스기가 카트보다 위에 있어야 함 (Y축 비교)
            bool isAboveKart = transform.position.y > kart.transform.position.y;

            // 2. 혹은 프레스기가 지금 '내려찍는 중(isGoingDown)'이어야 함

            if (isAboveKart && isGoingDown)
            {
                Debug.Log("압사!");
                // 길게 납작해져라! (5초)
                kart.Squash(squashDuration);
            }
            else
            {
                Debug.Log("옆에 부딪힘 (납작해지지 않음)");
                // 옆에 부딪히면 그냥 물리적으로 튕겨나감
            }
        }
    }
}