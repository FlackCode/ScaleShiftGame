using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    private float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public float shrinkedWalkSpeed;
    public float shrinkedSprintSpeed;
    public float drag;

    [Header("Scale")]
    private bool isSmall = false;
    private bool isScaling = false;
    public float scaleDuration = 1f;
    Vector3 bigScale = new Vector3(1, 1, 1);
    Vector3 smallScale = new Vector3(0.4f, 0.4f, 0.4f);

    [Header("Crouch")]
    private bool isCrouching = false;
    public float crouchSpeed;
    private float crouchYScale = 0.7f;
    private float startYScale;
    public float crouchTransitionDuration = 0.2f;
    private Coroutine crouchCoroutine;

    [Header("Slide")]
    public float slideDuration;
    private bool isSliding = false;

    [Header("Jump")]
    public float jumpForce;
    public float smallJumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    private bool readyToJump = true;
    public float gravityScale = 3f;

    [Header("Keybinds")]
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode scaleKey = KeyCode.E;
    public KeyCode crouchKey = KeyCode.C;
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Ground Check")]
    private float playerHeight;
    public LayerMask whatIsGround;
    private bool grounded;

    [Header("Extra")]
    public Transform orientation;
    public Transform cameraPos;
    float horizontalInput;
    float verticalInput;
    Vector3 moveDirection;
    Rigidbody rigidBody;
    private float defaultSprintSpeed;

    public State state;

    public enum State {
        walking,
        sprinting,
        crouching,
        scaling,
        jumping,
        sliding
    }
    // Start is called before the first frame update
    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        rigidBody.freezeRotation = true;

        playerHeight = transform.localScale.y;
        startYScale = playerHeight;
        moveSpeed = walkSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        grounded = CheckGrounded();
        StateHandler();
    }

    private void StateHandler() {
        if (isScaling || isSliding) return; // If scaling, ignore other transitions

        if (Input.GetKeyDown(scaleKey) && !isScaling && !isCrouching && !CheckBelow() && CheckGrounded()) {
            StartCoroutine(ScalePlayer());
            state = State.scaling;
            return;
        }

        if (Input.GetKeyDown(crouchKey) && !isSmall) {
            if (Input.GetKey(sprintKey) && grounded) {
                state = State.sliding;
                if (crouchCoroutine != null) {
                    StopCoroutine(crouchCoroutine);
                }
                StartCoroutine(Slide());
            } else {
                state = State.crouching;
                if (crouchCoroutine != null) {
                    StopCoroutine(crouchCoroutine);
                }
                crouchCoroutine = StartCoroutine(Crouch());
            }
        }

        if (Input.GetKeyUp(crouchKey) && isCrouching && !isSmall && !CheckBelow()) {
            state = State.walking;
            if (crouchCoroutine != null) {
                StopCoroutine(crouchCoroutine);
            }
            crouchCoroutine = StartCoroutine(Uncrouch());
        }

        if (isCrouching && !Input.GetKey(crouchKey) && !CheckBelow()) {
            if (crouchCoroutine != null) {
                StopCoroutine(crouchCoroutine);
            }
            crouchCoroutine = StartCoroutine(Uncrouch());
        }

        if (Input.GetKeyDown(jumpKey)) {
            state = State.jumping;
            StartCoroutine(Jump());
            
        }

        if (grounded && Input.GetKey(sprintKey) && !isCrouching) {
            state = State.sprinting;
            if (isSmall) {
                moveSpeed = shrinkedSprintSpeed;
            } else {
                moveSpeed = sprintSpeed;
            }
        } else if (grounded && !isCrouching) {
            state = State.walking;
            if (isSmall) {
                moveSpeed = shrinkedWalkSpeed;
            } else {
                moveSpeed = walkSpeed;
            }
        }
    }
    
    private void FixedUpdate()
    {
        if (grounded && state != State.scaling && state != State.sliding)
        {
            MovePlayer();
            rigidBody.drag = drag;
        }
        else
        {
            rigidBody.drag = 0f;
        }

        if (!grounded) {
            rigidBody.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);
        }
    }

    private void MovePlayer()
    {
        Vector3 moveDirection = orientation.forward * Input.GetAxisRaw("Vertical") + orientation.right * Input.GetAxisRaw("Horizontal");
        Debug.Log($"Move Direction: {moveDirection}, Speed: {moveSpeed}");
    
        if (grounded || state == State.scaling) {
            if (moveDirection != Vector3.zero) {
                rigidBody.AddForce(10f * moveSpeed * moveDirection.normalized, ForceMode.Force);
            }
        } else {
            if (moveDirection != Vector3.zero) {
                rigidBody.AddForce(10f * airMultiplier * moveSpeed * moveDirection.normalized, ForceMode.Force);
            }
        }
    }

    private IEnumerator ScalePlayer() {
        isScaling = true;
        float elapsedTime = 0f;

        Vector3 startScale = transform.localScale;
        Vector3 targetScale = isSmall ? bigScale : smallScale;
        Vector3 cameraStartPos = cameraPos.localPosition;
        float originalHeight = 1f;
        float targetHeight = isSmall ? originalHeight : originalHeight * smallScale.y;
        Vector3 cameraTargetPos = new Vector3(cameraStartPos.x, targetHeight, cameraStartPos.z);
        
        while (elapsedTime < scaleDuration) {
            transform.localScale = Vector3.Lerp(startScale, targetScale, elapsedTime / scaleDuration);
            cameraPos.localPosition = Vector3.Lerp(cameraStartPos, cameraTargetPos, elapsedTime / scaleDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = targetScale;
        cameraPos.localPosition = cameraTargetPos;
        isSmall = !isSmall;
        isScaling = false;
    }

    private IEnumerator Crouch() {
        isCrouching = true;
        float elapsedTime = 0f;
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = new Vector3(originalScale.x, startYScale * crouchYScale, originalScale.z);

        moveSpeed = crouchSpeed;

        Vector3 cameraStartPos = cameraPos.localPosition;
        float originalHeight = 1f;
        float targetHeight = originalHeight * crouchYScale;
        Vector3 cameraTargetPos = new Vector3(cameraStartPos.x, targetHeight, cameraStartPos.z);
        
        while (elapsedTime < crouchTransitionDuration) {
            transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsedTime / crouchTransitionDuration);
            cameraPos.localPosition = Vector3.Lerp(cameraStartPos, cameraTargetPos, elapsedTime / crouchTransitionDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.localScale = targetScale;
        cameraPos.localPosition = cameraTargetPos;
    }

    private IEnumerator Uncrouch() {
        isCrouching = false;
        float elapsedTime = 0f;
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = new Vector3(originalScale.x, startYScale, originalScale.z);

        moveSpeed = walkSpeed;

        Vector3 cameraStartPos = cameraPos.localPosition;
        float targetHeight = 1f;
        Vector3 cameraTargetPos = new Vector3(cameraStartPos.x, targetHeight, cameraStartPos.z);

        while (elapsedTime < crouchTransitionDuration) {
            transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsedTime / crouchTransitionDuration);
            cameraPos.localPosition = Vector3.Lerp(cameraStartPos, cameraTargetPos, elapsedTime / crouchTransitionDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.localScale = targetScale;
        cameraPos.localPosition = cameraTargetPos;
    }

    private IEnumerator Jump() {
        if (readyToJump && grounded) {
            float adjustedJumpForce = isSmall ? smallJumpForce : jumpForce;
            rigidBody.AddForce(Vector3.up * adjustedJumpForce, ForceMode.Impulse);
            grounded = false;
            readyToJump = false;
            yield return new WaitForSeconds(jumpCooldown);
            readyToJump = true;
        }
    }

    private IEnumerator Slide() {
        isSliding = true;

        if (!isCrouching) {
            if (crouchCoroutine != null) {
                StopCoroutine(crouchCoroutine);
            }
            crouchCoroutine = StartCoroutine(Crouch());
        }

        float slideMultiplier = 1.1f;
        moveSpeed = (isSmall ? shrinkedSprintSpeed : sprintSpeed ) * slideMultiplier;
        Vector3 slideDirection = orientation.forward;

        rigidBody.AddForce(slideDirection * moveSpeed, ForceMode.VelocityChange);

        yield return new WaitForSeconds(slideDuration);

        moveSpeed = isSmall ? shrinkedSprintSpeed : sprintSpeed;
        isSliding = false;

    }

    private bool CheckGrounded() {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        Vector3 rayDirection = Vector3.down;
        float rayDistance = (playerHeight * 0.5f) + 0.2f;

        bool isGrounded = Physics.Raycast(rayOrigin, rayDirection, rayDistance);

        Debug.DrawRay(rayOrigin, rayDirection * rayDistance, isGrounded ? Color.green : Color.red);

        return isGrounded;
    }

    private bool CheckBelow() {
        Vector3 rayOrigin = transform.position + Vector3.up;
        Vector3 rayDirection = Vector3.up;
        float rayDistance = playerHeight * 1.1f;
        
        bool isBelow = Physics.Raycast(rayOrigin, rayDirection, rayDistance);

        Debug.DrawRay(rayOrigin, rayDirection * rayDistance, isBelow ? Color.green : Color.red);

        return isBelow;
    }
}
