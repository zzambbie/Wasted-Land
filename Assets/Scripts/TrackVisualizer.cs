using UnityEngine;

[ExecuteInEditMode] // 게임 실행 안 해도 보이게 함
public class TrackVisualizer : MonoBehaviour
{
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        // 자식 오브젝트(점)들을 순서대로 가져옴
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform current = transform.GetChild(i);

            // 점 위치에 공 그리기
            Gizmos.DrawSphere(current.position, 0.5f);

            // 다음 점으로 선 긋기
            if (i < transform.childCount - 1)
            {
                Transform next = transform.GetChild(i + 1);
                Gizmos.DrawLine(current.position, next.position);
            }
            // 마지막 점 -> 시작 점 연결 (트랙이니까)
            else
            {
                Transform first = transform.GetChild(0);
                Gizmos.DrawLine(current.position, first.position);
            }
        }
    }
}