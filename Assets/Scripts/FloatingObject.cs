using UnityEngine;

public class FloatingObject : MonoBehaviour
{
    public float rotateSpeed = 50f;
    public float floatSpeed = 2f;
    public float floatHeight = 0.2f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // ºù±Ûºù±Û È¸Àü
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);

        // À§¾Æ·¡ µÕµÕ
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}