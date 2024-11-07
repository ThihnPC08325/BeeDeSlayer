using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    #region SerializeField
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 12f; // Tăng tốc độ di chuyển
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private float acceleration = 150f;
    [SerializeField] private float deceleration = 200f;
    [SerializeField] private float airControl = 0.8f; // Kiểm soát khi ở trên không

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private float gravity = -25f; // Tăng trọng lực để nhảy nhanh hơn

    [Header("Dashing")]
    [SerializeField] private float dashSpeed = 30f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private int maxDashes = 2;
    #endregion

    #region private
    private CharacterController controller;
    private Vector2 moveInput;
    private Vector3 velocity;
    private bool isGrounded;
    private int jumpsRemaining;
    private bool isDashing;
    private int remainingDashes;
    private float dashEndTime;

    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    #endregion

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = new PlayerInput();
    }

    private void OnEnable()
    {
        moveAction = playerInput.Player.Move;
        moveAction.Enable();

        jumpAction = playerInput.Player.Jump;
        jumpAction.Enable();

        dashAction = playerInput.Player.Dash;
        dashAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        dashAction.Disable();
    }

    private void FixedUpdate()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            jumpsRemaining = maxJumps;
            remainingDashes = maxDashes;
        }
    }

    private void Update()
    {
        HandleMovement();
        ApplyGravity();
        if (dashAction.triggered && remainingDashes > 0)
        {
            StartCoroutine(Dash());
            remainingDashes--;
        }
    }

    private void HandleMovement()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        if (isDashing)
        {
            if (Time.time >= dashEndTime)
            {
                isDashing = false;
            }
            else
            {
                controller.Move(move.normalized * dashSpeed * Time.deltaTime);
                return;
            }
        }

        //---MOVE---
        float targetSpeed = move.magnitude * moveSpeed;
        float currentSpeed = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;

        if (targetSpeed > currentSpeed)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, deceleration * Time.deltaTime);
        }

        currentSpeed = Mathf.Clamp(currentSpeed, 0, maxSpeed);

        if (move.magnitude > 0.1f)
        {
            controller.Move(move.normalized * currentSpeed * Time.deltaTime);
        }

        if (isGrounded)
        {
            controller.Move(move * moveSpeed * Time.deltaTime);
        }
        else
        {
            //---AIR CONTROL---
            controller.Move(move * moveSpeed * airControl * Time.deltaTime);
        }

        //---JUMP---
        if (jumpAction.triggered && jumpsRemaining > 0)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            jumpsRemaining--;
        }
    }

    private void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private IEnumerator Dash()
    {
        isDashing = true;
        dashEndTime = Time.time + dashDuration;

        // Đọc input hiện tại để xác định hướng dash
        Vector2 input = moveAction.ReadValue<Vector2>();

        // Dash theo hướng input
        Vector3 dashDirection = transform.TransformDirection(new Vector3(input.x, 0, input.y)).normalized;

        // Tính toán vận tốc dash
        velocity = dashDirection * dashSpeed;

        // Thực hiện dash
        while (Time.time < dashEndTime)
        {
            controller.Move(velocity * Time.deltaTime);
            yield return null;
        }

        // Kết thúc dash
        isDashing = false;
        velocity = Vector3.zero;
    }
}