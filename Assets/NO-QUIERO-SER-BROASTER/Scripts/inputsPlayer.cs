using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador en primera persona que usa el mapa "Player" del asset InputSystem_Actions.
/// Todas las acciones se leen con expresiones lambda.
/// - Movimiento (Move), camara (Look), salto (Jump), correr (Sprint).
/// - Raycast desde el centro de la camara: al presionar Interact (E) interactua con IInteractable.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class InputsPlayer : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Arrastra aqui el asset InputSystem_Actions.")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Referencias")]
    [Tooltip("Camara hija del jugador (a la altura de los ojos).")]
    [SerializeField] private Transform cameraTransform;

    [Header("Movimiento")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float acceleration = 12f;

    [Header("Camara")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float gamepadSensitivity = 150f;
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;
    [SerializeField] private bool lockCursor = true;

    [Header("Interaccion")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactLayers = ~0;
    [SerializeField] private bool drawDebugRay = true;

    // Mapa y acciones del asset
    private InputActionMap playerMap;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction interactAction;

    // Lambdas guardadas en variables para poder desuscribirlas (el asset es compartido)
    private Action<InputAction.CallbackContext> onMove;
    private Action<InputAction.CallbackContext> onMoveCanceled;
    private Action<InputAction.CallbackContext> onLook;
    private Action<InputAction.CallbackContext> onLookCanceled;
    private Action<InputAction.CallbackContext> onJump;
    private Action<InputAction.CallbackContext> onSprint;
    private Action<InputAction.CallbackContext> onSprintCanceled;
    private Action<InputAction.CallbackContext> onInteract;

    // Valores que actualizan las lambdas
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool lookFromStick;
    private bool isSprinting;
    private bool jumpRequested;

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float pitch;

    /// <summary>Objeto interactuable que se esta mirando ahora mismo (puede ser null). Util para UI.</summary>
    public IInteractable CurrentTarget { get; private set; }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (inputActions == null)
        {
            Debug.LogError("InputsPlayer: falta asignar el InputActionAsset (InputSystem_Actions).", this);
            enabled = false;
            return;
        }

        // Mapa "Player" y sus acciones
        playerMap = inputActions.FindActionMap("Player", true);
        moveAction = playerMap.FindAction("Move", true);
        lookAction = playerMap.FindAction("Look", true);
        jumpAction = playerMap.FindAction("Jump", true);
        sprintAction = playerMap.FindAction("Sprint", true);
        interactAction = playerMap.FindAction("Interact", true);

        // ---------- LAMBDAS ----------
        onMove = ctx => moveInput = ctx.ReadValue<Vector2>();
        onMoveCanceled = _ => moveInput = Vector2.zero;

        onLook = ctx =>
        {
            lookInput = ctx.ReadValue<Vector2>();
            lookFromStick = !(ctx.control.device is Pointer); // mouse = delta por frame, resto = stick
        };
        onLookCanceled = _ => lookInput = Vector2.zero;

        onJump = _ => jumpRequested = true;

        onSprint = _ => isSprinting = true;
        onSprintCanceled = _ => isSprinting = false;

        // "started" se dispara al presionar, incluso si Interact tiene una interaccion Hold
        onInteract = _ => TryInteract();
    }

    private void OnEnable()
    {
        if (playerMap == null) return;

        moveAction.performed += onMove;
        moveAction.canceled += onMoveCanceled;
        lookAction.performed += onLook;
        lookAction.canceled += onLookCanceled;
        jumpAction.performed += onJump;
        sprintAction.performed += onSprint;
        sprintAction.canceled += onSprintCanceled;
        interactAction.started += onInteract;

        playerMap.Enable();

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDisable()
    {
        if (playerMap == null) return;

        moveAction.performed -= onMove;
        moveAction.canceled -= onMoveCanceled;
        lookAction.performed -= onLook;
        lookAction.canceled -= onLookCanceled;
        jumpAction.performed -= onJump;
        sprintAction.performed -= onSprint;
        sprintAction.canceled -= onSprintCanceled;
        interactAction.started -= onInteract;

        playerMap.Disable();

        // Evita que queden valores "pegados" al desactivar
        moveInput = Vector2.zero;
        lookInput = Vector2.zero;
        isSprinting = false;
        jumpRequested = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        HandleLook();
        HandleMovement();
        UpdateTarget();
    }

    // ---------------- CAMARA ----------------
    private void HandleLook()
    {
        // El delta del mouse ya es por frame; el stick necesita sensibilidad * deltaTime
        Vector2 delta = lookFromStick
            ? lookInput * gamepadSensitivity * Time.deltaTime
            : lookInput * mouseSensitivity;

        // Yaw: rota el cuerpo del jugador
        transform.Rotate(Vector3.up, delta.x);

        // Pitch: rota solo la camara, con limite
        pitch = Mathf.Clamp(pitch - delta.y, minPitch, maxPitch);
        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    // ---------------- MOVIMIENTO ----------------
    private void HandleMovement()
    {
        Vector2 input = Vector2.ClampMagnitude(moveInput, 1f);

        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
        Vector3 targetVelocity = (transform.right * input.x + transform.forward * input.y) * targetSpeed;

        // Aceleracion suave
        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity, targetVelocity, acceleration * targetSpeed * Time.deltaTime);

        // Gravedad y salto
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // mantiene al personaje pegado al suelo

        if (jumpRequested && controller.isGrounded)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        jumpRequested = false;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    // ---------------- RAYCAST / INTERACCION ----------------
    private bool TryRaycast(out RaycastHit hit)
    {
        hit = default;
        if (cameraTransform == null) return false;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (drawDebugRay)
            Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.green);

        return Physics.Raycast(ray, out hit, interactDistance, interactLayers,
                               QueryTriggerInteraction.Ignore);
    }

    /// <summary>Actualiza CurrentTarget cada frame (para mostrar un prompt "Presiona E", etc.).</summary>
    private void UpdateTarget()
    {
        CurrentTarget = null;

        if (TryRaycast(out RaycastHit hit))
            CurrentTarget = hit.collider.GetComponentInParent<IInteractable>();
    }

    private void TryInteract()
    {
        if (!TryRaycast(out RaycastHit hit)) return;

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
        if (interactable != null)
            interactable.Interact(this);
        else
            Debug.Log($"Raycast golpeo '{hit.collider.name}' pero no es interactuable.");
    }
}