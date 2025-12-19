using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
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

    [Header("Animation Settings")]
    public Animator animator;
    public bool useAnimation = true;

    // HIER GEÄNDERT: Statt Parameter, gibst du hier die Namen der States an
    public string idleStateName = "Idle"; // Name des orangenen Kastens im Animator für Stehen
    public string walkStateName = "Run";  // Name des orangenen Kastens im Animator für Laufen

    [Header("Stack Settings")]
    public Transform stackPoint;
    public float stackOffsetY = 0.9f;
    public float stackOffsetZ = 0.6f;
    public int maxStackSize = 20;

    private CharacterController controller;
    private List<GameObject> currentStack = new List<GameObject>();
    private float currentSpeed;

    // Damit wir die Animation nicht jeden Frame neu starten
    private bool wasMoving = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        UpdateSpeedAndUI();
    }

    void Update()
    {
        // Wenn Spiel nicht läuft -> Idle erzwingen
        if (GameManager.Instance != null && !GameManager.Instance.IsGameRunning())
        {
            if (useAnimation && animator != null)
            {
                animator.Play(idleStateName);
            }
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

        // --- HIER NEU: Animation direkt abspielen ---
        if (useAnimation && animator != null)
        {
            // Wir prüfen, ob sich der Zustand geändert hat, damit die Animation nicht stottert
            if (isMoving != wasMoving)
            {
                if (isMoving)
                {
                    animator.Play(walkStateName);
                }
                else
                {
                    animator.Play(idleStateName);
                }
                wasMoving = isMoving;
            }
        }

        if (isMoving)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, targetAngle, 0f), Time.deltaTime * rotateSpeed);
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
            if (currentStack.Count < maxStackSize) CollectGift(other.gameObject);
        }
        else if (other.CompareTag("Snowman"))
        {
            DropAllGifts();
        }
    }

    void CollectGift(GameObject groundGift)
    {
        Collider col = groundGift.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Renderer[] renderers = groundGift.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers) rend.enabled = true;

        GiftDespawner despawner = groundGift.GetComponent<GiftDespawner>();
        if (despawner != null) Destroy(despawner);

        groundGift.transform.SetParent(stackPoint);
        float newY = currentStack.Count * stackOffsetY;
        groundGift.transform.localPosition = new Vector3(0f, newY, stackOffsetZ);
        groundGift.transform.localRotation = Quaternion.identity;
        currentStack.Add(groundGift);
        UpdateSpeedAndUI();
    }

    void DropAllGifts()
    {
        if (currentStack.Count == 0) return;
        foreach (var gift in currentStack)
        {
            gift.transform.SetParent(null);
            Vector2 randomCircle = Random.insideUnitCircle * 3f;
            Vector3 dropPos = transform.position + new Vector3(randomCircle.x, 0.5f, randomCircle.y);
            dropPos.x = Mathf.Clamp(dropPos.x, minX, maxX);
            dropPos.z = Mathf.Clamp(dropPos.z, minZ, maxZ);

            gift.transform.position = dropPos;
            gift.transform.rotation = Quaternion.identity;

            Collider col = gift.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            Renderer[] renderers = gift.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers) r.enabled = true;

            if (gift.GetComponent<GiftDespawner>() == null)
                gift.AddComponent<GiftDespawner>();
        }
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

    public int GiveGifts(int neededAmount)
    {
        int amountToGive = Mathf.Min(currentStack.Count, neededAmount);
        for (int i = 0; i < amountToGive; i++)
        {
            int lastIndex = currentStack.Count - 1;
            GameObject gift = currentStack[lastIndex];
            currentStack.RemoveAt(lastIndex);
            Destroy(gift);
        }
        UpdateSpeedAndUI();
        return amountToGive;
    }
}