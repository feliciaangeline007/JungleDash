// PlayerRunner.cs – 3D endless runner player controller.
// Uses CharacterController. Touch swipe + keyboard input.
// Unity 6 compatible – no obsolete APIs.
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerRunner : MonoBehaviour
{
    [Header("Speed")]
    public float startSpeed   = 8f;
    public float maxSpeed     = 20f;
    public float acceleration = 0.4f;

    [Header("Lanes")]
    public float laneWidth      = 2f;
    public float laneChangeTime = 0.15f;

    [Header("Jump")]
    public float jumpVelocity = 10f;
    public float gravity      = -22f;
    public float jumpBuffer   = 0.12f;

    [Header("Swipe")]
    [Range(0.03f, 0.2f)] public float swipeThreshold = 0.05f;

    [Header("Animation")]
    public Animator characterAnimator;

    static readonly int HashForward  = Animator.StringToHash("Forward");
    static readonly int HashGround   = Animator.StringToHash("OnGround");
    static readonly int HashJump     = Animator.StringToHash("Jump");

    CharacterController _cc;
    float _speed;
    int   _lane;          // -1, 0, 1
    float _targetX;
    float _xVel;          // SmoothDamp ref
    float _yVel;
    float _jumpBuf;
    bool  _hasAnim;
    bool  _jumpIsBool;

    // Touch
    bool    _touchDown;
    Vector2 _touchStart;
    bool    _swipeDone;

    void Awake() => _cc = GetComponent<CharacterController>();

    void Start()
    {
        _speed = startSpeed;
        gameObject.tag = "Player";

        _hasAnim = characterAnimator != null;
        if (_hasAnim)
        {
            characterAnimator.applyRootMotion = false;
            foreach (var p in characterAnimator.parameters)
                if (p.nameHash == HashJump)
                { _jumpIsBool = p.type == AnimatorControllerParameterType.Bool; break; }
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsRunning) return;
        float dt = Time.deltaTime;

        _speed = Mathf.Min(_speed + acceleration * dt, maxSpeed);
        _jumpBuf -= dt;

        ReadInput();

        bool grounded = _cc.isGrounded;
        if (grounded && _yVel < 0) _yVel = -2f;
        if (grounded && _jumpBuf > 0) { _yVel = jumpVelocity; _jumpBuf = 0; }
        if (!grounded) _yVel += gravity * dt;

        float newX = Mathf.SmoothDamp(transform.position.x, _targetX, ref _xVel, laneChangeTime);
        Vector3 move = new Vector3(newX - transform.position.x, _yVel * dt, _speed * dt);
        _cc.Move(move);

        if (_hasAnim)
        {
            characterAnimator.SetFloat(HashForward, 1f);
            characterAnimator.SetBool(HashGround, grounded);
            if (_jumpIsBool) characterAnimator.SetBool(HashJump, !grounded && _yVel > 0);
            else             characterAnimator.SetFloat(HashJump, Mathf.Max(0, _yVel));
        }
    }

    void ReadInput()
    {
        // Touch
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)  { _touchStart = t.position; _touchDown = true; _swipeDone = false; }
            if (_touchDown && !_swipeDone && t.phase == TouchPhase.Moved) EvalSwipe(t.position - _touchStart);
            if (t.phase == TouchPhase.Ended)  _touchDown = false;
        }
        // Mouse drag
        if (Input.GetMouseButtonDown(0)) { _touchStart = Input.mousePosition; _touchDown = true; _swipeDone = false; }
        if (_touchDown && !_swipeDone && Input.GetMouseButton(0)) EvalSwipe((Vector2)Input.mousePosition - _touchStart);
        if (Input.GetMouseButtonUp(0)) _touchDown = false;

        // Keyboard
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))  ChangeLane(-1);
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) ChangeLane(+1);
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            _jumpBuf = jumpBuffer;
    }

    void EvalSwipe(Vector2 d)
    {
        float t = Screen.height * swipeThreshold;
        if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) { if (Mathf.Abs(d.x) >= t) { ChangeLane(d.x > 0 ? 1 : -1); _swipeDone = true; } }
        else                                  { if (d.y >= t)              { _jumpBuf = jumpBuffer;        _swipeDone = true; } }
    }

    void ChangeLane(int dir)
    {
        _lane    = Mathf.Clamp(_lane + dir, -1, 1);
        _targetX = _lane * laneWidth;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Coin"))     { GameManager.Instance?.AddCoin(1);  other.gameObject.SetActive(false); return; }
        if (other.CompareTag("Gem"))      { GameManager.Instance?.AddCoin(5);  other.gameObject.SetActive(false); return; }
        if (other.CompareTag("Obstacle")) { GameManager.Instance?.TriggerGameOver(); }
    }
}
