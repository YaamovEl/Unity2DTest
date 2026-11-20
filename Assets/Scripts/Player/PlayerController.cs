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

    private void UpdateTimers()
    {
        // Coyote time: grace period for jumping after leaving ground

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private void HandleJumping()
    {
        // Can jump if: grounded, in coyote time, or has jump buffer
        bool canJump = (isGrounded || coyoteTimeCounter > 0f) && jumpBufferCounter > 0f;

        if (canJump && !isJumping)
        {
            PerformJump();
        }

        // Variable jump height: shorter jumps when button released early
        if (!jumpInputHeld && rb.velocity.y > 0f && isJumping)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * variableJumpHeight);
        }
    }

    private void PerformJump()
    {
        // Reset vertical velocity for consistent jump height
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);

        isJumping = true;
        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;

        // Trigger jump animation and sound
        animator.SetTrigger("Jump");
        // AudioManager.Instance.PlaySFX("Jump");

        // Optional: Jump particle effect
        // ParticleSystem jumpEffect = Instantiate(jumpParticlePrefab, transform.position, Quaternion.identity);
    }

    private void ApplyMovement()
    {
        // Calculate target velocity with acceleration/deceleration
        float targetVelocityX = horizontalInput * moveSpeed;
        float smoothTime = (horizontalInput != 0) ? accelerationTime : decelerationTime;

        // Smooth velocity changes for natural movement feel
        float newVelocityX = Mathf.SmoothDamp(
            rb.velocity.x,
            targetVelocityX,
            ref velocityXSmoothing,
            smoothTime
        );

        rb.velocity = new Vector2(newVelocityX, rb.velocity.y);

        // Handle sprite flipping
        if (horizontalInput > 0 && !facingRight)
        {
            Flip();
        }
        else if (horizontalInput < 0 && facingRight)
        {
            Flip();
        }
    }

    private void ApplyJumpPhysics()
    {
        // Enhanced gravity for better platformer feel
        if (rb.velocity.y < 0)
        {
            // Falling: apply extra downward force
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.velocity.y > 0 && !jumpInputHeld)
        {
            // Rising but not holding jump: reduce upward velocity
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    private void ClampFallSpeed()
    {
        // Prevent excessive fall speeds that feel uncontrollable
        if (rb.velocity.y < maxFallSpeed)
        {
            rb.velocity = new Vector2(rb.velocity.x, maxFallSpeed);
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        spriteRenderer.flipX = !facingRight;
    }

    private void OnLanding()
    {
        // Called when player lands on ground
        // Add landing effects, sound, screen shake, etc.
        animator.SetTrigger("Land");
        // CameraController.Instance.Shake(0.1f, 0.1f);
    }

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            Gizmos.DrawLine(groundCheck.position,
                           groundCheck.position + Vector3.down * groundCheckDistance);
        }
    }

    private void UpdateAnimations()
    {
        // Movement animation parameters
        animator.SetFloat("Speed", Mathf.Abs(horizontalInput));
        animator.SetFloat("VelocityY", rb.velocity.y);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsMoving", Mathf.Abs(rb.velocity.x) > 0.1f);

        // State-based animation triggers
        if (wasGrounded && !isGrounded && rb.velocity.y < 0)
        {
            animator.SetTrigger("Fall");
        }
    }

    // Public methods for external systems
    public bool IsGrounded() => isGrounded;
    public bool IsMoving() => Mathf.Abs(rb.velocity.x) > 0.1f;
    public Vector2 GetVelocity() => rb.velocity;
    public bool IsFacingRight() => facingRight;

    // State modification methods for power-ups or special abilities
    public void SetMoveSpeed(float newSpeed) => moveSpeed = newSpeed;
    public void SetJumpForce(float newJumpForce) => jumpForce = newJumpForce;

    public void AddForce(Vector2 force, ForceMode2D mode = ForceMode2D.Impulse)
    {
        rb.AddForce(force, mode);
    }

    // Disable/enable movement (for cutscenes, menus, etc.)
    public void SetMovementEnabled(bool enabled)
    {
        this.enabled = enabled;
        if (!enabled)
        {
            rb.velocity = Vector2.zero;
        }
    }


}
