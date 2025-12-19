using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement & Speed")]
    public float baseMoveSpeed = 10f;
    public float rotateSpeed = 10f;
    public TMP_Text speedDisplayUI;

    [Header("Boundaries")]
    public float minX = -30f;
    public float maxX = 30f;
    public float minZ = -13f;
    public float maxZ = 9f;
    public float fixedY = 0f;

    [Header("Animation")]
    public Animator animator;
    public bool useAnimation = true;
    public string moveParameterName = "IsMoving";

    [Header("Stacking & Penalty")]
    public Transform stackPoint; // HIER dein "Giftsack"-Objekt reinziehen!
                                 // public GameObject giftVisualPrefab; // Brauchen wir nicht mehr!
                                 // public GameObject giftPickupPrefab; // Brauchen wir nicht mehr zwingend, wir nutzen das Original

    [Header("Stack Settings")]
    public float stackOffsetY = 0.9f; // Pro Geschenk 0.9 hoch
    public float stackOffsetZ = 0.6f; // Immer 0.6 nach hinten versetzt
    public int maxStackSize = 20;

    private CharacterController controller;
    private List<GameObject> currentStack = new List<GameObject>();
    private float currentSpeed;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        UpdateSpeedAndUI();
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameRunning())
        {
            if (useAnimation && animator != null) animator.SetBool(moveParameterName, false);
            return;
        }

        Move();
    }

    void Move()
    {
        float x = 0f;
        float z = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) z -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) z += 1f;
        }

        Vector3 direction = new Vector3(x, 0, z).normalized;
        bool isMoving = direction.magnitude >= 0.1f;

        if (useAnimation && animator != null)
        {
            animator.SetBool(moveParameterName, isMoving);
        }

        if (isMoving)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation = Quaternion.Lerp(transform.rotation, rotation, Time.deltaTime * rotateSpeed);

            Vector3 moveVector = direction * currentSpeed * Time.deltaTime;
            controller.Move(moveVector);
        }

        Vector3 finalPos = transform.position;
        finalPos.x = Mathf.Clamp(finalPos.x, minX, maxX);
        finalPos.z = Mathf.Clamp(finalPos.z, minZ, maxZ);
        finalPos.y = fixedY;
        transform.position = finalPos;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Gift"))
        {
            if (currentStack.Count < maxStackSize)
            {
                CollectGift(other.gameObject);
            }
            else
            {
                Debug.Log("Stapel voll!");
            }
        }
        else if (other.CompareTag("Snowman"))
        {
            DropAllGifts();
        }
    }

    void CollectGift(GameObject groundGift)
    {
        // 1. Physik & Logik vom Boden-Objekt entfernen
        // Collider ausschalten, damit wir es nicht nochmal einsammeln, während es auf dem Rücken ist
        Collider col = groundGift.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Despawner zerstören (damit es NICHT verschwindet)
        GiftDespawner despawner = groundGift.GetComponent<GiftDespawner>();
        if (despawner != null) Destroy(despawner);

        // 2. An den Giftsack heften (Reparenting)
        groundGift.transform.SetParent(stackPoint);

        // 3. Positionieren nach deiner Formel
        // X = 0, Z = 0.6
        // Y = Index * 0.9 (Erstes ist 0, Zweites 0.9, Drittes 1.8)
        float newY = currentStack.Count * stackOffsetY;

        groundGift.transform.localPosition = new Vector3(0f, newY, stackOffsetZ);
        groundGift.transform.localRotation = Quaternion.identity; // Rotation nullen

        // 4. Zur Liste hinzufügen
        currentStack.Add(groundGift);

        UpdateSpeedAndUI();
    }

    void DropAllGifts()
    {
        if (currentStack.Count == 0) return;

        Debug.Log("Oh nein! Schneemann getroffen!");

        // Wir gehen durch alle Geschenke auf dem Rücken
        foreach (var gift in currentStack)
        {
            // 1. Vom Spieler lösen
            gift.transform.SetParent(null); // Wieder in die Welt setzen

            // 2. Zufällige Position berechnen
            Vector2 randomCircle = Random.insideUnitCircle * 3f;
            Vector3 dropPos = transform.position + new Vector3(randomCircle.x, 0.5f, randomCircle.y);

            // Grenzen checken
            dropPos.x = Mathf.Clamp(dropPos.x, minX, maxX);
            dropPos.z = Mathf.Clamp(dropPos.z, minZ, maxZ);

            gift.transform.position = dropPos;
            gift.transform.rotation = Quaternion.identity;

            // 3. Collider wieder anmachen (damit man sie wieder sammeln kann)
            Collider col = gift.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            // 4. NEUEN Despawner hinzufügen (damit sie nach einer Weile weggehen, wenn man sie nicht holt)
            // Wir fügen das Skript neu hinzu, dadurch startet der Timer von vorne
            if (gift.GetComponent<GiftDespawner>() == null)
            {
                gift.AddComponent<GiftDespawner>();
            }
        }

        // Liste leeren
        currentStack.Clear();
        UpdateSpeedAndUI();
    }

    void UpdateSpeedAndUI()
    {
        float fullness = (float)currentStack.Count / (float)maxStackSize;
        float speedFactor = 1.0f - (0.5f * fullness);
        currentSpeed = baseMoveSpeed * speedFactor;

        if (speedDisplayUI != null)
        {
            int displayPercent = Mathf.RoundToInt(speedFactor * 100);
            speedDisplayUI.text = $"Speed: {displayPercent}%";

            if (displayPercent > 75) speedDisplayUI.color = Color.green;
            else if (displayPercent > 50) speedDisplayUI.color = Color.yellow;
            else speedDisplayUI.color = Color.red;
        }
    }

    public int ClearStack()
    {
        int amount = currentStack.Count;
        // Beim Abladen am Zug zerstören wir die Objekte wirklich
        foreach (var gift in currentStack)
        {
            Destroy(gift);
        }
        currentStack.Clear();

        UpdateSpeedAndUI();

        return amount;
    }
}