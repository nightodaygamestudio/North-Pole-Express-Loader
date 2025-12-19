using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class SnowmanController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f; // Wird vom Spawner überschrieben
    public float waitTime = 2f;
    public float rotateSpeed = 5f;

    [Header("Wander Area")]
    public float minX = -30f;
    public float maxX = 30f;
    public float minZ = -13f;
    public float maxZ = 9f;
    public float fixedY = 0f;

    [Header("Model Correction")]
    public float xRotationCorrection = -90f;

    private Vector3 targetPosition;
    private float timer;
    private bool isWaiting;
    private bool isSpawning = true;

    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        // Rotation korrigieren
        transform.rotation = Quaternion.Euler(xRotationCorrection, 0f, 0f);
        StartCoroutine(RiseFromGround());
    }

    // Methode zum Ändern der Geschwindigkeit
    public void SetSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }

    IEnumerator RiseFromGround()
    {
        isSpawning = true;
        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(startPos.x, fixedY, startPos.z);
        float elapsed = 0f;
        float duration = 1.5f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;
        isSpawning = false;
        SetNewRandomTarget();
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameRunning()) return;
        if (isSpawning) return;

        // Rotations-Korrektur (X-Achse festnageln)
        float currentYRot = transform.rotation.eulerAngles.y;
        transform.rotation = Quaternion.Euler(xRotationCorrection, currentYRot, 0f);

        if (isWaiting)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                isWaiting = false;
                SetNewRandomTarget();
            }
        }
        else
        {
            MoveToTarget();
        }
    }

    void MoveToTarget()
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            // Rotation berechnen
            Quaternion targetRot = Quaternion.LookRotation(direction);
            Quaternion correctedTargetRot = Quaternion.Euler(xRotationCorrection, targetRot.eulerAngles.y, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, correctedTargetRot, Time.deltaTime * rotateSpeed);

            // Bewegung
            Vector3 moveVector = direction * moveSpeed * Time.deltaTime;
            controller.Move(moveVector);
        }

        // Ziel-Check (flach)
        Vector3 flatPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 flatTarget = new Vector3(targetPosition.x, 0, targetPosition.z);

        if (Vector3.Distance(flatPos, flatTarget) < 0.2f)
        {
            isWaiting = true;
            timer = waitTime;
        }

        // Position auf Boden zwingen
        Vector3 clampedPos = transform.position;
        clampedPos.y = fixedY;
        transform.position = clampedPos;
    }

    void SetNewRandomTarget()
    {
        float x = Random.Range(minX, maxX);
        float z = Random.Range(minZ, maxZ);
        targetPosition = new Vector3(x, fixedY, z);
    }
}