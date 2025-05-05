using System.Collections;
using UnityEngine;

public class MonsterAgent : MonoBehaviour
{
    public float moveSpeed = 3f;            // Bewegungsgeschwindigkeit in Units/Sekunde
    public float stepDuration = 0.3f;       // Dauer pro Schritt (wird intern berechnet)
    public float rotationSpeed = 360f;      // Grad/Sekunde für Drehung

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

        // (Rotation bleibt wie gehabt)

        Vector3 direction = (targetPosition - transform.position).normalized;

        if (direction != Vector3.zero)
        {
            Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);

            // Sanfte Rotation (solange nicht fast schon richtig ausgerichtet)
            while (Quaternion.Angle(transform.rotation, toRotation) > 1f)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    toRotation,
                    rotationSpeed * Time.deltaTime
                );
                yield return null;
            }

            // Am Ende: Rotation exakt setzen
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

        // 🚨 Bewegung beendet – Callback!
        onMoveComplete?.Invoke();
    }


    //private IEnumerator MoveStep(Vector3 targetPosition)
    //{
    //    isMoving = true;
    //    animator.SetFloat("Speed", moveSpeed);

    //    // Drehe dich in Zielrichtung
    //    Vector3 dir = (targetPosition - transform.position).normalized;
    //    if (dir != Vector3.zero)
    //    {
    //        Quaternion toRotation = Quaternion.LookRotation(dir, Vector3.up);
    //        while (Quaternion.Angle(transform.rotation, toRotation) > 0.5f)
    //        {
    //            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
    //            yield return null;
    //        }
    //    }

    //    // Bewege dich zum Ziel
    //    float elapsed = 0f;
    //    Vector3 startPos = transform.position;

    //    while (elapsed < stepDuration)
    //    {
    //        transform.position = Vector3.Lerp(startPos, targetPosition, elapsed / stepDuration);
    //        elapsed += Time.deltaTime;
    //        yield return null;
    //    }

    //    // Korrigiere auf exakt Zielposition
    //    transform.position = targetPosition;

    //    animator.SetFloat("Speed", 0f);
    //    isMoving = false;
    //}
}

//using UnityEngine;

//public class MonsterAgent : MonoBehaviour
//{
//    public float moveSpeed = 2f;
//    private Animator animator;
//    private Rigidbody rb;

//    void Start()
//    {
//        animator = GetComponent<Animator>();
//        rb = GetComponent<Rigidbody>();
//    }

//    public void MoveTo(Vector2Int direction)
//    {
//        Vector3 dir3D = new Vector3(direction.x, 0f, direction.y).normalized;
//        Vector3 velocity = dir3D * moveSpeed;
//        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);

//        if (dir3D != Vector3.zero)
//        {
//            Quaternion toRotation = Quaternion.LookRotation(dir3D, Vector3.up);
//            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, 360 * Time.deltaTime);
//        }

//        animator.SetFloat("Speed", moveSpeed);
//        animator.SetBool("Attack", false); // Optional – falls Attack noch gesetzt war
//    }
//}
