using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Basic Setting")]
    [SerializeField] private float baseMultiplier = 1f;
    [SerializeField] private float sensitivityX = 2f;
    [SerializeField] private float sensitivityY = 2f;
    [SerializeField] private bool useMouseAcceleration = false;
    [SerializeField] private bool invertY = false;

    [Header("AnimationCurve")]
    [SerializeField] private AnimationCurve sensitivityCurve = AnimationCurve.Linear(0, 1, 1, 1);
    [SerializeField] private AnimationCurve adsCurve = AnimationCurve.Linear(0, 1, 1, 0.5f);
    [SerializeField] private AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0, 1, 1, 2);

    [SerializeField] Transform playerBody;

    #region private
    private Vector2 currentRotation;
    private PlayerInput playerInput;
    private InputAction lookAction;
    private readonly bool isAiming = false;
    #endregion

    private void Awake()
    {
        playerInput = new PlayerInput();
        lookAction = playerInput.Player.Look;
    }

    private void OnEnable()
    {
        SetCameraControlEnabled(true);
    }

    private void OnDisable()
    {
        SetCameraControlEnabled(false);
    }

    private void Update()
    {
        HandleMouseLook();
    }

    private void HandleMouseLook()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        float inputMagnitude = lookInput.magnitude;

        // Áp dụng sensitivity curve
        float sensitivityMultiplier = sensitivityCurve.Evaluate(inputMagnitude);

        // Xử lý ADS
        float adsMultiplier = 1f;
        if (isAiming)
        {
            adsMultiplier = adsCurve.Evaluate(inputMagnitude);
        }

        // Xử lý acceleration
        float accelerationMultiplier = 1f;
        if (useMouseAcceleration)
        {
            accelerationMultiplier = accelerationCurve.Evaluate(inputMagnitude);
        }

        // Tính toán độ nhạy cuối cùng
        float finalMultiplier = baseMultiplier * sensitivityMultiplier * adsMultiplier * accelerationMultiplier;

        // Áp dụng độ nhạy riêng cho từng trục
        Vector2 processedInput = new Vector2(
            lookInput.x * sensitivityX * finalMultiplier,
            lookInput.y * sensitivityY * finalMultiplier
        ) * Time.deltaTime;

        if (invertY)
            processedInput.y = -processedInput.y;

        // Cập nhật rotation
        currentRotation.x = Mathf.Clamp(currentRotation.x - processedInput.y, -90f, 90f);
        currentRotation.y += processedInput.x;

        // Áp dụng rotation
        transform.localRotation = Quaternion.Euler(currentRotation.x, 0f, 0f);
        playerBody.localRotation = Quaternion.Euler(0f, currentRotation.y, 0f);
    }

    public void SetCameraControlEnabled(bool enabled)
    {
        if (enabled)
        {
            lookAction.Enable();
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            lookAction.Disable();
            Cursor.lockState = CursorLockMode.None;
        }
    }
}