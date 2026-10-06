// PlayerRunner.cs
// Drives the player via CharacterController.
// Handles lane switching, jumping, input (touch swipe + keyboard + mouse drag),
// fly state, and reports distance to GameManager.
// No per-frame allocations.
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerRunner : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector: movement
    // ─────────────────────────────────────────────
    [Header("Speed")]
    [Tooltip("Starting forward speed (units/sec).")]
    public float startSpeed     = 8f;
    [Tooltip("Maximum forward speed (units/sec).")]
    public float maxSpeed       = 20f;
    [Tooltip("How many units/sec² the speed increases.")]
    public float acceleration   = 0.5f;

    [Header("Lanes")]
    [Tooltip("Lateral distance between lane centres.")]
    public float laneWidth      = 2.0f;
    [Tooltip("Seconds to slide between lanes.")]
    public float laneChangeTime = 0.18f;

    [Header("Jump")]
    [Tooltip("Initial upward velocity when jumping.")]
    public float jumpVelocity   = 10f;
    [Tooltip("Gravity applied while airborne.")]
    public float gravity        = -22f;
    [Tooltip("Jump buffer window in seconds.")]
    public float jumpBuffer     = 0.12f;

    [Header("Swipe Input")]
    [Tooltip("Fraction of screen height required to register a swipe.")]
    [Range(0.02f, 0.2f)]
    public float swipeThreshold = 0.06f;

    [Header("Animation")]
    [Tooltip("Optional Animator on the character model child.")]
    public Animator characterAnimator;

    // ─────────────────────────────────────────────
    //  Internal
    // ─────────────────────────────────────────────
    private CharacterController _cc;
    private float _currentSpeed;
    private int   _currentLane;          // -1, 0, +1
    private float _targetX;
    private float _laneVelocity;         // SmoothDamp ref
    private float _vertVelocity;
    private float _jumpBufferTimer;
    private bool  _wasGrounded;

    // Fly state
    private float _groundY;             // Y of ground at game start
    private bool  _flyGrounded = true;  // true once landed after fly

    // Touch / mouse input (no alloc: single finger tracking)
    private bool  _touchActive;
    private Vector2 _touchStart;
    private bool  _swipeFired;

    // Animator parameter detection
    private bool _hasAnimator;
    private bool _jumpParamIsBool;
    private static readonly int _fwdHash    = Animator.StringToHash("Forward");
    private static readonly int _groundHash = Animator.StringToHash("OnGround");
    private static readonly int _jumpHash   = Animator.StringToHash("Jump");

    // Distance dirty tracking
    private float _distanceThisFrame;

    // ─────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────
    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    private void Start()
    {
        _currentSpeed = startSpeed;
        _currentLane  = 0;
        _targetX      = 0f;
        _groundY      = transform.position.y;

        // Animator detection – determine Jump parameter type once
        _hasAnimator = characterAnimator != null;
        if (_hasAnimator)
        {
            characterAnimator.applyRootMotion = false;
            _jumpParamIsBool = false;
            foreach (var param in characterAnimator.parameters)
            {
                if (param.nameHash == _jumpHash)
                {
                    _jumpParamIsBool = param.type == AnimatorControllerParameterType.Bool;
                    break;
                }
            }
        }

        // Ensure tag
        if (!gameObject.CompareTag("Player"))
            Debug.LogWarning("[PlayerRunner] GameObject is not tagged 'Player'. Tag it for trigger detection.");
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsRunning) return;

        float dt = Time.deltaTime;

        // 1. Accelerate
        _currentSpeed = Mathf.Min(_currentSpeed + acceleration * dt, maxSpeed);

        // Apply speed boost from power-up
        float boostedSpeed = _currentSpeed;
        if (PowerUpManager.Instance != null)
        {
            float bonus = PowerUpManager.Instance.SpeedBonusFraction;
            boostedSpeed *= (1f + bonus);
            PowerUpManager.Instance.SetPlayerSpeed(boostedSpeed);
        }

        // 2. Input
        ReadInput();

        // 3. Jump buffer
        _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - dt);

        // 4. Grounded check
        bool grounded = _cc.isGrounded;

        // 5. Vertical velocity
        bool flyActive = PowerUpManager.Instance != null && PowerUpManager.Instance.FlyActive;

        if (flyActive)
        {
            HandleFlyVertical(dt);
        }
        else
        {
            if (!_flyGrounded)
            {
                // Just finished fly – land cleanly
                _flyGrounded = true;
            }

            if (grounded)
            {
                if (_vertVelocity < 0f) _vertVelocity = -2f; // small push to keep grounded
                if (_jumpBufferTimer > 0f)
                {
                    _vertVelocity    = jumpVelocity;
                    _jumpBufferTimer = 0f;
                }
            }
            else
            {
                _vertVelocity += gravity * dt;
            }
        }

        // 6. Lateral movement (smooth)
        float posX = Mathf.SmoothDamp(transform.position.x, _targetX,
                                      ref _laneVelocity, laneChangeTime);

        // 7. Move
        Vector3 move = new Vector3(
            posX - transform.position.x,
            _vertVelocity * dt,
            boostedSpeed  * dt
        );
        _cc.Move(move);

        // 8. Distance score
        _distanceThisFrame = boostedSpeed * dt;
        if (GameManager.Instance != null)
            GameManager.Instance.AddDistance(_distanceThisFrame);

        // 9. Animator
        if (_hasAnimator)
        {
            characterAnimator.SetFloat(_fwdHash, 1f);
            characterAnimator.SetBool(_groundHash, grounded || flyActive);
            SetJumpParam(grounded ? 0f : Mathf.Max(0f, _vertVelocity));
        }

        _wasGrounded = grounded;

        // Update fly grounded tracking
        if (flyActive) _flyGrounded = false;
    }

    // ─────────────────────────────────────────────
    //  Fly vertical logic
    // ─────────────────────────────────────────────
    private void HandleFlyVertical(float dt)
    {
        float targetY = _groundY + PowerUpManager.Instance.FlyHeightOffset;
        float currentY = transform.position.y;
        // Smoothly move toward target height
        float newY  = Mathf.MoveTowards(currentY, targetY, flyVSpeed * dt);
        _vertVelocity = (newY - currentY) / dt;
    }
    [Tooltip("Vertical speed during fly lerp (units/sec).")]
    [SerializeField] private float flyVSpeed = 2.5f;

    // ─────────────────────────────────────────────
    //  Input
    // ─────────────────────────────────────────────
    private void ReadInput()
    {
        // Touch
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                _touchStart  = t.position;
                _touchActive = true;
                _swipeFired  = false;
            }
            else if (_touchActive && !_swipeFired && t.phase == TouchPhase.Moved)
            {
                EvaluateSwipe(t.position - _touchStart);
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                _touchActive = false;
            }
        }
        // Mouse drag (editor)
        else if (Input.GetMouseButtonDown(0))
        {
            _touchStart  = Input.mousePosition;
            _touchActive = true;
            _swipeFired  = false;
        }
        else if (_touchActive && !_swipeFired && Input.GetMouseButton(0))
        {
            EvaluateSwipe((Vector2)Input.mousePosition - _touchStart);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            _touchActive = false;
        }

        // Keyboard fallback
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))  SwitchLane(-1);
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) SwitchLane(+1);
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) ||
            Input.GetKeyDown(KeyCode.UpArrow)) QueueJump();
    }

    private void EvaluateSwipe(Vector2 delta)
    {
        float thresh = Screen.height * swipeThreshold;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            if (Mathf.Abs(delta.x) >= thresh)
            {
                SwitchLane(delta.x > 0 ? +1 : -1);
                _swipeFired = true;
            }
        }
        else
        {
            if (delta.y >= thresh)
            {
                QueueJump();
                _swipeFired = true;
            }
        }
    }

    // ─────────────────────────────────────────────
    //  Lane / jump helpers
    // ─────────────────────────────────────────────
    private void SwitchLane(int dir)
    {
        bool flyActive = PowerUpManager.Instance != null && PowerUpManager.Instance.FlyActive;
        if (flyActive) return; // no lane-change during fly? optional; allow it for fun
        _currentLane = Mathf.Clamp(_currentLane + dir, -1, 1);
        _targetX     = _currentLane * laneWidth;
    }

    private void QueueJump()
    {
        bool flyActive = PowerUpManager.Instance != null && PowerUpManager.Instance.FlyActive;
        if (flyActive) return; // cannot jump while flying
        _jumpBufferTimer = jumpBuffer;
    }

    // ─────────────────────────────────────────────
    //  Obstacle / pickup trigger
    // ─────────────────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        // Collectibles
        Collectible col = other.GetComponent<Collectible>();
        if (col != null)
        {
            col.Collect();
            return;
        }

        // Power-up pickups
        PowerUpPickup pup = other.GetComponent<PowerUpPickup>();
        if (pup != null)
        {
            pup.Collect();
            return;
        }

        // Obstacles
        Obstacle obs = other.GetComponent<Obstacle>();
        if (obs != null)
        {
            // Fly = invulnerable
            bool flyActive = PowerUpManager.Instance != null
                             && PowerUpManager.Instance.FlyActive;
            if (flyActive) return;

            // Shield absorb
            if (PowerUpManager.Instance != null
                && PowerUpManager.Instance.TryConsumeShield())
            {
                obs.TriggerDebris();
                obs.Hide();
                return;
            }

            // Game over
            if (GameManager.Instance != null)
                GameManager.Instance.TriggerGameOver();
        }
    }

    // ─────────────────────────────────────────────
    //  Animator helper
    // ─────────────────────────────────────────────
    private void SetJumpParam(float value)
    {
        if (!_hasAnimator) return;
        if (_jumpParamIsBool)
            characterAnimator.SetBool(_jumpHash, value > 0.1f);
        else
            characterAnimator.SetFloat(_jumpHash, value);
    }
}
