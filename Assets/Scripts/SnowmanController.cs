using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))] // Erzwingt den Controller
public class SnowmanController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float waitTime = 2f;
    public float rotateSpeed = 5f;

    [Header("Wander Area")]
    public float minX = -30f;
    public float maxX = 30f;
    public float minZ = -13f;
    public float maxZ = 9f;

    // HIER: Damit nageln wir ihn auf dem Boden fest, genau wie den Player
    public float fixedY = 0f;

    [Header("Model Correction")]
    // Wenn dein Modell liegt, drehen wir das Visual-Child oder das Objekt selbst.
    // Falls das ganze Objekt gedreht werden muss:
    public float xRotationCorrection = -90f;

    private Vector3 targetPosition;
    private float timer;
    private bool isWaiting;
    private bool isSpawning = true;

    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Sofortige Korrektur der Rotation beim Start
        // Wir setzen die Rotation hart, damit er steht
        transform.rotation = Quaternion.Euler(xRotationCorrection, 0f, 0f);

        StartCoroutine(RiseFromGround());
    }

    IEnumerator RiseFromGround()
    {
        isSpawning = true;

        // Startposition (unter der Erde)
        Vector3 startPos = transform.position;
        // Zielposition (auf dem Boden)
        Vector3 endPos = new Vector3(startPos.x, fixedY, startPos.z);

        float elapsed = 0f;
        float duration = 1.5f;

        while (elapsed < duration)
        {
            // Wir bewegen nur die Position, Rotation bleibt fest korrigiert
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

        // Wir erzwingen permanent die X-Rotation, falls die Physik sie verhaut
        // Aber wir erlauben Y-Rotation (drehen zum Ziel)
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
        // 1. Richtung berechnen (nur X und Z, Y ignorieren wir)
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // Wichtig: Keine Bewegung nach oben/unten berechnen

        if (direction != Vector3.zero)
        {
            // 2. Rotation zum Ziel (aber unter Beibehaltung der -90 X-Rotation)
            Quaternion targetRot = Quaternion.LookRotation(direction);
            // Wir kombinieren die Ziel-Y-Rotation mit deiner festen X-Korrektur
            Quaternion correctedTargetRot = Quaternion.Euler(xRotationCorrection, targetRot.eulerAngles.y, 0f);

            transform.rotation = Quaternion.Slerp(transform.rotation, correctedTargetRot, Time.deltaTime * rotateSpeed);

            // 3. Bewegung mit CharacterController (wie beim Player)
            Vector3 moveVector = direction * moveSpeed * Time.deltaTime;
            controller.Move(moveVector);
        }

        // 4. Ziel erreicht Check
        // Wir ignorieren hier auch die Höhe beim Distanz-Check
        Vector3 flatPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 flatTarget = new Vector3(targetPosition.x, 0, targetPosition.z);

        if (Vector3.Distance(flatPos, flatTarget) < 0.2f)
        {
            isWaiting = true;
            timer = waitTime;
        }

        // 5. Hard Fix für Position (genau wie beim Player)
        Vector3 clampedPos = transform.position;
        clampedPos.y = fixedY; // Auf Boden zwingen
        transform.position = clampedPos;
    }

    void SetNewRandomTarget()
    {
        float x = Random.Range(minX, maxX);
        float z = Random.Range(minZ, maxZ);
        targetPosition = new Vector3(x, fixedY, z);
    }
}