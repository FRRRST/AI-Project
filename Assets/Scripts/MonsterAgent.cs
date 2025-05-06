using System.Collections;
using UnityEngine;

public class MonsterAgent : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float stepDuration = 0.3f;
    public float rotationSpeed = 360f;      //grad/sek für drehung

    private Animator animator;
    public System.Action onMoveComplete;
    private bool isMoving = false;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void MoveTo(Vector2Int direction)
    {
        if (!isMoving)
        {
            Vector3 targetPos = transform.position + new Vector3(direction.x, 0f, direction.y);
            StartCoroutine(MoveStep(targetPos));
        }
    }

    private IEnumerator MoveStep(Vector3 targetPosition)
    {
        isMoving = true;
        animator.SetFloat("Speed", moveSpeed);

        Vector3 direction = (targetPosition - transform.position).normalized;

        if (direction != Vector3.zero)
        {
            Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);

            while (Quaternion.Angle(transform.rotation, toRotation) > 1f)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    toRotation,
                    rotationSpeed * Time.deltaTime
                );
                yield return null;
            }

            transform.rotation = toRotation;
        }

        float elapsed = 0f;
        Vector3 startPos = transform.position;

        while (elapsed < stepDuration)
        {
            transform.position = Vector3.Lerp(startPos, targetPosition, elapsed / stepDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;

        animator.SetFloat("Speed", 0f);
        isMoving = false;

        onMoveComplete?.Invoke();
    }
}
