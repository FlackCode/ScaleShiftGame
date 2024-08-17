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

    [Header("Keybinds")]
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode scaleKey = KeyCode.E;
    public KeyCode crouchKey = KeyCode.C;

    [Header("Ground Check")]
    private float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    [Header("Extra")]
    public Transform orientation;
    public Transform cameraPos; //the actual camera, not the camera pos...
    float horizontalInput;
    float verticalInput;
    Vector3 moveDirection;
    Rigidbody rigidBody;

    public MovementState state;

    public enum MovementState {
        walking,
        sprinting,
        crouching
    }
    // Start is called before the first frame update
    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        rigidBody.freezeRotation = true;

        playerHeight = transform.localScale.y;
        startYScale = playerHeight;
    }

    // Update is called once per frame
    void Update()
    {
        float raycastDistance = playerHeight * 0.5f + 0.2f;

        grounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, raycastDistance, whatIsGround);
        Debug.DrawRay(transform.position + Vector3.up * 0.1f, Vector3.down * raycastDistance, Color.red);

        Debug.Log("Grounded: " + grounded);

        MyInput();
        StateHandler();
        SpeedControl();
    }
    
    void FixedUpdate() {
        if (grounded)
        {
            MovePlayer();
            rigidBody.drag = drag;
        }
        else
        {
            rigidBody.drag = 0f;
        }
    }

    private void MyInput() {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(scaleKey) && !isScaling && !isCrouching) {
            StartCoroutine(ScalePlayer());
        }

        if (Input.GetKeyDown(crouchKey) && !isSmall) {
            if (crouchCoroutine != null) {
                StopCoroutine(crouchCoroutine);
            }
            crouchCoroutine = StartCoroutine(Crouch());
        }

        if (Input.GetKeyUp(crouchKey) && !isSmall) {
            if (crouchCoroutine != null) {
                StopCoroutine(crouchCoroutine);
            }
            crouchCoroutine = StartCoroutine(Uncrouch());
        }
    }

    private void StateHandler() {
        if (Input.GetKey(crouchKey) && !isSmall) {
            state = MovementState.crouching;
            moveSpeed = crouchSpeed;
        } else if (grounded && Input.GetKey(sprintKey)) {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
        } else if (grounded) {
            state = MovementState.walking;
            moveSpeed = walkSpeed;
        }
    }
    
    private void MovePlayer() {
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if (grounded) {
            rigidBody.AddForce(10f * moveSpeed * moveDirection.normalized, ForceMode.Force);
        }
    }

    private void SpeedControl() {
        Vector3 flatVel = new Vector3(rigidBody.velocity.x, 0f, rigidBody.velocity.z);;

        if (flatVel.magnitude > moveSpeed) {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rigidBody.velocity = new Vector3(limitedVel.x, rigidBody.velocity.y, limitedVel.z);
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
}
