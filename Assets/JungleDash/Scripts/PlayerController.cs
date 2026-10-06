// PlayerController.cs – Auto-runs right, swipe/tap to jump, 2D physics.
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Auto-run speed (units/sec).")] public float runSpeed = 6f;
    [Tooltip("Maximum run speed cap.")]       public float maxSpeed = 14f;
    [Tooltip("Speed increase per second.")]   public float acceleration = 0.3f;
    [Tooltip("Jump force.")]                  public float jumpForce = 14f;

    [Header("Ground Check")]
    [Tooltip("Point to check ground contact.")] public Transform groundCheck;
    [Tooltip("Radius of ground check circle.")] public float groundRadius = 0.2f;
    [Tooltip("Layer mask for ground.")]          public LayerMask groundLayer;

    [Header("Jump Buffer & Coyote")]
    public float jumpBufferTime = 0.15f;
    public float coyoteTime     = 0.12f;

    [Header("Swipe Input")]
    [Range(0.02f, 0.2f)]
    public float swipeThreshold = 0.05f;

    // Animator hashes
    static readonly int HashSpeed    = Animator.StringToHash("Speed");
    static readonly int HashGrounded = Animator.StringToHash("Grounded");
    static readonly int HashJump     = Animator.StringToHash("Jump");
    static readonly int HashDead     = Animator.StringToHash("Dead");

    Rigidbody2D _rb;
    Animator    _anim;
    bool        _grounded;
    float       _currentSpeed;
    float       _jumpBufferTimer;
    float       _coyoteTimer;
    bool        _isDead;

    // Swipe tracking (no allocation)
    bool    _touchDown;
    Vector2 _touchStart;
    bool    _swipeFired;

    void Awake()
    {
        _rb   = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();
        _currentSpeed = runSpeed;
    }

    void Update()
    {
        if (_isDead) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsRunning) return;

        // Accelerate
        _currentSpeed = Mathf.Min(_currentSpeed + acceleration * Time.deltaTime, maxSpeed);

        // Ground check
        bool wasGrounded = _grounded;
        _grounded = groundCheck != null &&
                    Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);

        // Coyote time
        if (wasGrounded && !_grounded) _coyoteTimer = coyoteTime;
        else if (_grounded)            _coyoteTimer = 0f;
        else                           _coyoteTimer -= Time.deltaTime;

        // Jump buffer timer
        _jumpBufferTimer -= Time.deltaTime;

        // Read input
        ReadInput();

        // Execute jump
        if (_jumpBufferTimer > 0f && (_grounded || _coyoteTimer > 0f))
        {
            DoJump();
        }

        // Animator
        _anim.SetFloat(HashSpeed, _currentSpeed);
        _anim.SetBool(HashGrounded, _grounded);
    }

    void FixedUpdate()
    {
        if (_isDead) return;
        // Keep horizontal velocity = run speed, preserve vertical
        Vector2 v = _rb.linearVelocity;
        v.x = _currentSpeed;
        _rb.linearVelocity = v;
    }

    void ReadInput()
    {
        // Touch input
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) { _touchStart = t.position; _touchDown = true; _swipeFired = false; }
            else if (_touchDown && !_swipeFired && t.phase == TouchPhase.Moved)
            {
                Vector2 delta = t.position - _touchStart;
                if (delta.y > Screen.height * swipeThreshold) { QueueJump(); _swipeFired = true; }
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) _touchDown = false;
        }
        // Mouse drag (Editor)
        if (Input.GetMouseButtonDown(0)) { _touchStart = Input.mousePosition; _touchDown = true; _swipeFired = false; }
        else if (_touchDown && !_swipeFired && Input.GetMouseButton(0))
        {
            Vector2 delta = (Vector2)Input.mousePosition - _touchStart;
            if (delta.y > Screen.height * swipeThreshold) { QueueJump(); _swipeFired = true; }
        }
        if (Input.GetMouseButtonUp(0)) _touchDown = false;

        // Keyboard
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            QueueJump();
    }

    void QueueJump() => _jumpBufferTimer = jumpBufferTime;

    void DoJump()
    {
        _jumpBufferTimer = 0f;
        _coyoteTimer     = 0f;
        Vector2 v = _rb.linearVelocity;
        v.y = jumpForce;
        _rb.linearVelocity = v;
        _anim.SetTrigger(HashJump);
    }

    public void Die()
    {
        if (_isDead) return;
        _isDead = true;
        _rb.linearVelocity = Vector2.zero;
        _anim.SetTrigger(HashDead);
        if (GameManager.Instance != null) GameManager.Instance.TriggerGameOver();
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Enemy")) Die();
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Coin"))
        {
            if (GameManager.Instance != null) GameManager.Instance.AddCoin(1);
            col.gameObject.SetActive(false);
        }
        if (col.CompareTag("Enemy")) Die();
        if (col.CompareTag("DeathZone")) Die();
    }
}
