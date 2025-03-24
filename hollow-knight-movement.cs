using UnityEngine;

public class HollowKnightController : MonoBehaviour
{
    [Header("Basic Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    
    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    
    [Header("Wall Detection")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float wallCheckDistance = 1f;
    [SerializeField] private LayerMask wallLayer;
    
    [Header("Double Jump")]
    private bool hasDoubleJump = false;
    
    [Header("Dash")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.6f;
    private bool canDash = true;
    private bool isDashing = false;
    private float dashTimeLeft;
    private float dashCooldownTimer;
    
    [Header("Wall Climb")]
    [SerializeField] private float wallSlideSpeed = 2f;
    [SerializeField] private float wallJumpXForce = 12f;
    [SerializeField] private float wallJumpYForce = 12f;
    private bool isWallSliding = false;
    
    // Components
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // Inputs
    private float horizontalInput;
    [SerializeField] public bool jumpPressed;
    [SerializeField] public bool dashPressed;

    [SerializeField] public bool isGrounded;
    [SerializeField] public bool isTouchingWall;
    private int facingDirection = 1; // 1 = right, -1 = left
    private float time1;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }
    
    void Update()
    {
        time1 = time1 + Time.deltaTime;
        if (isDashing) return;
        
        // Inputs
        horizontalInput = Input.GetAxisRaw("Horizontal");
        
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Z))
        {
            jumpPressed = true;
        }
        
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.K))
        {
            dashPressed = true;
        }
        
        // Checks
        CheckGrounded();
        CheckWall();
        
        // Handles
        if (time1 > 0.1f) HandleJump();
        HandleWallSlide();
        HandleDash();
        
        // jump physics
        JumpPhysics();
        
        // Flip character on movement dir
        if (horizontalInput > 0 && facingDirection < 0)
        {
            Flip();
        }
        else if (horizontalInput < 0 && facingDirection > 0)
        {
            Flip();
        }
        
        // Reset input flags
        jumpPressed = false;
        dashPressed = false;
    }
    
    void FixedUpdate()
    {
        if (isDashing)
        {
            HandleDashMovement();
            return;
        }
        
        // Normal horizontal movement
        if (!isWallSliding)
        {
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        }
    }
    
    void CheckGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        
        // Reset abilities when landing
        if (isGrounded)
        {
            hasDoubleJump = true;
        }
    }
    
    void CheckWall()
    {
        isTouchingWall = Physics2D.Raycast(wallCheck.position, new Vector2(facingDirection, 0), wallCheckDistance, wallLayer);
        if (isTouchingWall) hasDoubleJump = true;
    }
    
    void HandleJump()
    {
        // Regular jump
        if (jumpPressed && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            // Play jump sound effect here
        }
        // Wall jump
        else if (jumpPressed && isWallSliding)
        {
            // Jump in the opposite direction of the wall
            rb.linearVelocity = new Vector2(-facingDirection * wallJumpYForce, wallJumpXForce);
            Flip(); // Flip to face away from wall
            // Play wall jump sound effect here
        }
        // Double jump
        else if (jumpPressed && !isGrounded && !isWallSliding && hasDoubleJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.8f); // Slightly lower force for double jump
            hasDoubleJump = false;

            // Play double jump effect and sound here
        }
    }
    
    void HandleWallSlide()
    {
        isWallSliding = false;
        
        // Check if we should be wall sliding
        if (isTouchingWall && !isGrounded && horizontalInput == facingDirection && rb.linearVelocity.y < 0)
        {
            isWallSliding = true;
            
            // Cap falling speed while wall sliding
            if (rb.linearVelocity.y < -wallSlideSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
            }
            
            // Wall dust particle effect would be here
        }
    }
    
    void HandleDash()
    {
        // Dash cooldown timer
        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }
        else if (!canDash)
        {
            canDash = true;
        }
        
        // Start dash
        if (dashPressed && canDash && !isDashing)
        {
            isDashing = true;
            canDash = false;
            dashTimeLeft = dashDuration;
            dashCooldownTimer = dashCooldown;
            
            // Reset Y velocity for consistent dash height
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            
            // Play dash effect and sound here
        }
    }
    
    void HandleDashMovement()
    {
        // Count down dash duration
        if (dashTimeLeft > 0)
        {
            dashTimeLeft -= Time.deltaTime;
            
            // Move in facing direction at dash speed
            rb.linearVelocity = new Vector2(facingDirection * dashSpeed, 0);
            
            // Dash trail effect would go here
        }
        else
        {
            // End dash
            isDashing = false;
            
            // Cap horizontal speed after dash ends
            if (Mathf.Abs(rb.linearVelocity.x) > moveSpeed)
            {
                rb.linearVelocity = new Vector2(facingDirection * moveSpeed, rb.linearVelocity.y);
            }
        }
    }
    
    void JumpPhysics()
    {
        // Enhanced fall - makes the character fall faster (like in Hollow Knight)
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        // Low jump multiplier - for short hops when the jump button is released early
        else if (rb.linearVelocity.y > 0 && !Input.GetKey(KeyCode.Space) && !Input.GetKey(KeyCode.Z))
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
        }
    }
    
    void Flip()
    {
        facingDirection *= -1;
        spriteRenderer.flipY = facingDirection < 0;
    }
    
    // Debug visualization
    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        
        if (wallCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + new Vector3(wallCheckDistance * (spriteRenderer != null && spriteRenderer.flipX ? -1 : 1), 0, 0));
        }
    }
}
