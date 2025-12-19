using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // ... (Andere Variablen wie bisher lassen) ...
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

    [Header("Stack Settings")]
    public Transform stackPoint;
    public float stackOffsetY = 0.9f;
    public float stackOffsetZ = 0.6f;
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

    // ... (Move, OnTriggerEnter, CollectGift, DropAllGifts bleiben GLEICH wie vorher) ...
    // ... (Kopiere hier deine Move, OnTriggerEnter, CollectGift, DropAllGifts Methoden rein) ...
    // Ich kürze das hier ab, damit du nur das Neue siehst:

    void Move()
    {
        // (Dein bestehender Move-Code)
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

        if (useAnimation && animator != null) animator.SetBool(moveParameterName, isMoving);

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

    // Diese Methode prüft die Kollision
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Gift"))
        {
            if (currentStack.Count < maxStackSize) CollectGift(other.gameObject);
        }
        // WICHTIG: Hast du dem Schneemann den Tag "Snowman" gegeben?
        else if (other.CompareTag("Snowman"))
        {
            DropAllGifts();
        }
        // Falls du auch eine Ablade-Zone hast (wie vorhin besprochen)
        else if (other.CompareTag("TrainZone"))
        {
            // Hier würde die Logik für GiveGifts rein, falls du sie nicht im TrainController hast
            // Aber wir haben das ja im TrainController gelöst, also passt das so.
        }
    }

    void CollectGift(GameObject groundGift)
    {
        Collider col = groundGift.GetComponent<Collider>();
        if (col != null) col.enabled = false;
        GiftDespawner despawner = groundGift.GetComponent<GiftDespawner>();
        if (despawner != null) Destroy(despawner);

        groundGift.transform.SetParent(stackPoint);
        float newY = currentStack.Count * stackOffsetY;
        groundGift.transform.localPosition = new Vector3(0f, newY, stackOffsetZ);
        groundGift.transform.localRotation = Quaternion.identity;
        currentStack.Add(groundGift);
        UpdateSpeedAndUI();
    }

    // Diese Methode verteilt die Geschenke im Kreis (hast du schon, nur zur Sicherheit)
    void DropAllGifts()
    {
        if (currentStack.Count == 0) return;

        foreach (var gift in currentStack)
        {
            gift.transform.SetParent(null);

            // ZUFALLS-KREIS LOGIK:
            Vector2 randomCircle = Random.insideUnitCircle * 3f; // Radius 3
            Vector3 dropPos = transform.position + new Vector3(randomCircle.x, 0.5f, randomCircle.y);

            // Begrenzung, damit sie nicht aus der Map fliegen
            dropPos.x = Mathf.Clamp(dropPos.x, minX, maxX);
            dropPos.z = Mathf.Clamp(dropPos.z, minZ, maxZ);

            gift.transform.position = dropPos;
            gift.transform.rotation = Quaternion.identity;

            // Collider wieder anmachen und Despawner hinzufügen
            Collider col = gift.GetComponent<Collider>();
            if (col != null) col.enabled = true;

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

    // --- HIER IST DIE NEUE FUNKTION ---
    // Gibt maximal 'neededAmount' zurück und behält den Rest
    public int GiveGifts(int neededAmount)
    {
        // Wir können maximal so viele geben, wie wir haben
        int amountToGive = Mathf.Min(currentStack.Count, neededAmount);

        // Wir entfernen die obersten Geschenke (vom Ende der Liste)
        for (int i = 0; i < amountToGive; i++)
        {
            // Immer das letzte Element nehmen
            int lastIndex = currentStack.Count - 1;
            GameObject gift = currentStack[lastIndex];

            // Aus Liste entfernen
            currentStack.RemoveAt(lastIndex);

            // Objekt zerstören (es ist ja jetzt im Zug)
            Destroy(gift);
        }

        UpdateSpeedAndUI(); // Speed wieder erhöhen!
        return amountToGive;
    }
}