using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador en primera persona de la Paloma.
/// Usa el mapa "Player" del asset InputSystem_Actions (Move, Look, Jump, Sprint, Interact).
/// - Mover: Move (WASD / stick izquierdo).
/// - Camara: Look (mouse / stick derecho).
/// - Saltar: Jump (Espacio / boton sur).
/// - Correr: Sprint (Shift / gatillo).
/// - Interactuar: Interact (E / boton oeste) -> raycast + IInteractable / evento OnInteract.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class inputsPaloma : MonoBehaviour
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

    /// <summary>Se dispara al presionar Interact mirando un collider de interactLayers.</summary>
    public event Action<RaycastHit> OnInteract;

    /// <summary>Collider que se esta mirando ahora mismo (null si no hay nada). Util para UI.</summary>
    public Collider CurrentTarget { get; private set; }

    /// <summary>Objeto interactuable que se esta mirando (null si no implementa IInteractable).</summary>
    public IInteractable CurrentInteractable { get; private set; }

    /// <summary>Texto del prompt del objeto mirado, o null. Util para UI.</summary>
    public string CurrentPrompt { get; private set; }

    /// <summary>Velocidad actual combinada (horizontal + vertical). La usa el Chef para predecir el movimiento.</summary>
    public Vector3 Velocity => horizontalVelocity + Vector3.up * verticalVelocity;

    /// <summary>Posicion de los ojos (camara), o la altura de pecho si no hay camara.</summary>
    public Vector3 EyePosition => cameraTransform != null
        ? cameraTransform.position
        : transform.position + Vector3.up * 1.2f;

    /// <summary>True mientras este pisando un charco.</summary>
    public bool Ralentizada => Time.time < finRalentizacion;

    /// <summary>
    /// Aplica una ralentizacion temporal (factor 0.45 = 45 % de la velocidad normal).
    /// Si ya estaba ralentizada se queda con el factor mas fuerte.
    /// </summary>
    public void Ralentizar(float factor, float duracion)
    {
        if (duracion <= 0f || factor >= 1f)
            return;

        factorRalentizacion = Mathf.Min(factorRalentizacion, Mathf.Clamp(factor, 0.05f, 1f));
        finRalentizacion = Mathf.Max(finRalentizacion, Time.time + duracion);
    }

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

    // Ralentizacion por charcos
    private float factorRalentizacion = 1f;
    private float finRalentizacion = -1f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null)
        {
            // Preferimos una camara hija antes que Camera.main, por si hay mas de una en escena
            Camera childCamera = GetComponentInChildren<Camera>(true);
            if (childCamera != null) cameraTransform = childCamera.transform;
            else if (Camera.main != null) cameraTransform = Camera.main.transform;
        }

        if (inputActions == null)
            inputActions = InputSystem.actions;

        if (inputActions == null)
        {
            Debug.LogError("inputsPaloma: falta asignar el InputActionAsset (InputSystem_Actions).", this);
            enabled = false;
            return;
        }

        playerMap = inputActions.FindActionMap("Player", false);
        if (playerMap == null)
        {
            Debug.LogError("inputsPaloma: el asset de inputs no tiene un mapa llamado 'Player'.", this);
            enabled = false;
            return;
        }

        moveAction = playerMap.FindAction("Move", false);
        lookAction = playerMap.FindAction("Look", false);
        jumpAction = playerMap.FindAction("Jump", false);
        sprintAction = playerMap.FindAction("Sprint", false);
        interactAction = playerMap.FindAction("Interact", false);

        // Las acciones que no existan simplemente se ignoran (no rompen el controlador)
        if (moveAction == null || lookAction == null)
            Debug.LogWarning("inputsPaloma: faltan las acciones 'Move' o 'Look' en el mapa 'Player'.", this);

        // ---------- LAMBDAS ----------
        onMove = ctx => moveInput = ctx.ReadValue<Vector2>();
        onMoveCanceled = _ => moveInput = Vector2.zero;

        onLook = ctx =>
        {
            lookInput = ctx.ReadValue<Vector2>();
            lookFromStick = !(ctx.control != null && ctx.control.device is Pointer); // mouse = delta por frame, resto = stick
        };
        onLookCanceled = _ => lookInput = Vector2.zero;

        onJump = _ => jumpRequested = true;

        onSprint = _ => isSprinting = true;
        onSprintCanceled = _ => isSprinting = false;

        // "started" se dispara al presionar, incluso si Interact tuviera una interaccion tipo Hold
        onInteract = _ => TryInteract();
    }

    private void OnEnable()
    {
        if (playerMap == null) return;

        if (moveAction != null)
        {
            moveAction.performed += onMove;
            moveAction.canceled += onMoveCanceled;
        }

        if (lookAction != null)
        {
            lookAction.performed += onLook;
            lookAction.canceled += onLookCanceled;
        }

        if (jumpAction != null) jumpAction.performed += onJump;

        if (sprintAction != null)
        {
            sprintAction.performed += onSprint;
            sprintAction.canceled += onSprintCanceled;
        }

        if (interactAction != null) interactAction.started += onInteract;

        playerMap.Enable();
    }

    private void OnDisable()
    {
        if (playerMap == null) return;

        if (moveAction != null)
        {
            moveAction.performed -= onMove;
            moveAction.canceled -= onMoveCanceled;
        }

        if (lookAction != null)
        {
            lookAction.performed -= onLook;
            lookAction.canceled -= onLookCanceled;
        }

        if (jumpAction != null) jumpAction.performed -= onJump;

        if (sprintAction != null)
        {
            sprintAction.performed -= onSprint;
            sprintAction.canceled -= onSprintCanceled;
        }

        if (interactAction != null) interactAction.started -= onInteract;

        playerMap.Disable();

        // Evita que queden valores "pegados" al desactivar
        moveInput = Vector2.zero;
        lookInput = Vector2.zero;
        isSprinting = false;
        jumpRequested = false;
        horizontalVelocity = Vector3.zero;
        factorRalentizacion = 1f;
        finRalentizacion = -1f;
        CurrentTarget = null;
        CurrentInteractable = null;
        CurrentPrompt = null;
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

        if (Time.time >= finRalentizacion)
            factorRalentizacion = 1f;

        float targetSpeed = (isSprinting ? sprintSpeed : walkSpeed) * (Ralentizada ? factorRalentizacion : 1f);
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

    /// <summary>Busca un IInteractable en el collider y en sus padres (por si el script esta en la raiz).</summary>
    private static IInteractable BuscarInteractuable(Collider collider)
    {
        return collider != null ? collider.GetComponentInParent<IInteractable>() : null;
    }

    /// <summary>Actualiza CurrentTarget / CurrentInteractable / CurrentPrompt cada frame (para UI).</summary>
    private void UpdateTarget()
    {
        if (!TryRaycast(out RaycastHit hit))
        {
            CurrentTarget = null;
            CurrentInteractable = null;
            CurrentPrompt = null;
            return;
        }

        ActualizarObjetivo(hit.collider);
    }

    private void ActualizarObjetivo(Collider collider)
    {
        CurrentTarget = collider;
        CurrentInteractable = BuscarInteractuable(collider);
        CurrentPrompt = CurrentInteractable != null ? CurrentInteractable.Prompt : null;
    }

    private void TryInteract()
    {
        if (!TryRaycast(out RaycastHit hit)) return;

        ActualizarObjetivo(hit.collider);

        // 1) Interaccion por interfaz (puertas, palancas, objetos del mundo...)
        if (CurrentInteractable != null)
            CurrentInteractable.Interact(this);

        // 2) Evento generico para quien prefiera suscribirse desde fuera
        if (OnInteract != null)
            OnInteract.Invoke(hit);
        else if (CurrentInteractable == null)
            Debug.Log($"Interactuo con '{hit.collider.name}' (nadie escucha OnInteract).");
    }
}
