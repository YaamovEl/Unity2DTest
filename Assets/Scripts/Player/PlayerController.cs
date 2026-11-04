using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 16f;
    [SerializeField] private float maxFallSpeed = -25f;
    [SerializeField] private float accelerationTime = 0.1f;
    [SerializeField] private float decelerationTime = 0.1f;

    [Header("Jump Settings")]
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.2f;
    [SerializeField] private float variableJumpHeight = 0.5f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayerMask;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private float groundCheckDistance = 0.1f;

    // Components
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // Movement state
    private float horizontalInput;
    private bool isGrounded;
    private bool wasGrounded;
    private float velocityXSmoothing;

    // Jump state
    private bool jumpInputDown;
    private bool jumpInputHeld;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool isJumping;

    // Animation state
    private bool facingRight = true;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Optimize physics for platformer gameplay
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    // Update is called once per frame
    void Update()
    {
        HandleInput();
        CheckGroundStatus();
        UpdateTimers();
        HandleJumping();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
        ApplyJumpPhysics();
        ClampFallSpeed();
    }

    private void HandleInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        jumpInputDown = Input.GetButtonDown("Jump");
        jumpInputHeld = Input.GetButton("Jump");

        // Handle jump buffering
        if (jumpInputDown)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    private void CheckGroundStatus()
    {
        wasGrounded = isGrounded;

        // Use multiple ground detection methods for reliability
        Vector2 groundCheckPos = (Vector2)groundCheck.position;
        isGrounded = Physics2D.OverlapCircle(groundCheckPos, groundCheckRadius, groundLayerMask);

        // Additional raycast for precise ground detection
        if (!isGrounded)
        {
            RaycastHit2D hit = Physics2D.Raycast(
                groundCheckPos,
                Vector2.down,
                groundCheckDistance,
                groundLayerMask
            );
            isGrounded = hit.collider != null;
        }

        // Reset jumping state when landing
        if (isGrounded && !wasGrounded)
        {
            isJumping = false;
            // Optional: Landing particle effect or sound
            OnLanding();
        }
    }
