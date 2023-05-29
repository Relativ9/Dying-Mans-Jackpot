using System.Collections;
using Unity.VisualScripting;
using UnityEngine;


public class PlayerMovement : MonoBehaviour
{

    [Header("Visible for debugging")]
    public float stepRayLenght;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float gravityStrength;
    [SerializeField] private Vector3 currentGravity;
    [SerializeField] private float groundSlopeDetected;
    public Vector3 groundPoint;
    public float stepAngle;

    [Header("Must remain publicly accessible")]
    public bool isGrounded;
    public bool isMoving;
    public bool isRunning;
    public Vector3 currentVel;
    public Vector3 stopVel;
    public float currentStaminaValue;
    public float airTime;

    [Header("Manually assigned variables")]
    [SerializeField] private Transform camFollowTrans;
    [SerializeField] private Transform dirParent;
    //[SerializeField] private Animator anim;
    [SerializeField] private GameObject bottomFoot;
    [SerializeField] private GameObject topFoot;

    //Assigned im LobbyManager
    public Transform fpCam;

    //Assigned in start
    private Rigidbody playerRb;
    //private GrappleHook grapHook;
    //private VolumeTrigger volTrig;
    //private BreathingCheck breathCheck;
    private PlayerSetup playerSetup;
    private WallRun wallRun;
    private Climbing climb;
    private CapsuleCollider playerCol;
    private LobbyManager lobbyManager;

    [Header("Editable in inspector")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float airSpeed = 50f;
    [SerializeField] private float wallRunSpeed = 1f;
    [SerializeField] private float swimSpeed = 2f;
    [SerializeField] private float maxStamina = 10f;
    [SerializeField] private float jumpHeight = 3.5f;
    [SerializeField] private float multiplier = 4.5f;
    [SerializeField] private float groundRayLength = 0.6f;
    [SerializeField] private float smoothStep = 0.1f;
    [SerializeField] private float maxSlopeAngle = 45f;
    [SerializeField] private float raycastLength = 3f; // length of raycast to ground (check isGrounded sensitivity)
    [SerializeField] private float normalGravityStrength = -9.8f;
    [SerializeField] private float noGravityStrength = 0f;
    [SerializeField] private float wallRunGravityStrength = -6f;
    [SerializeField] private float grapplingGravityStrength = -30f;


    private bool canJump;
    private bool sliding;
    private bool crouching;
    private bool groundSlide;
    private Vector3 systemGravity;
    private float groundTime;
    private Vector3 crouchScale;
    private Vector3 normalScale;
    private Vector3 forward;
    private Vector3 right;
    private float secondsSinceWallRun;
    private bool recentlyWallRan;
    private bool debug;


    [Header("Input stuff")]
    [SerializeField] public float horizontal;
    [SerializeField] public float vertical;

    private RaycastHit groundRay;


    //Various context sensitive velocity directions 
    private Vector3 moveDirection;
    private Vector3 moveDirectionFlat;
    private Vector3 moveDirectionSlope;
    private Vector3 moveDirectionSliding;
    private Vector3 moveDirectionSwimming;
    private Vector3 moveDirectionLeftDiagonal;
    private Vector3 moveDirectionRightDiagonal;

    void Start()
    {
        playerSetup = this.GetComponent<PlayerSetup>();
        playerRb = this.GetComponent<Rigidbody>();
        //grapHook = FindAnyObjectByType<GrappleHook>();
        //volTrig = FindAnyObjectByType<VolumeTrigger>();
        wallRun = this.GetComponent<WallRun>();
        climb = this.GetComponent<Climbing>();
        playerCol = this.GetComponent<CapsuleCollider>();
        //breathCheck = FindAnyObjectByType<BreathingCheck>();
        //anim = FindAnyObjectByType<AnimatorStates>().GetComponent<Animator>();
        lobbyManager = FindAnyObjectByType<LobbyManager>();
        //lobbyManager.playerMovement = this;
        //playerSetup.playerMovement = this;
        fpCam = lobbyManager.camSys.GetComponentInChildren<Camera>().transform;
        this.gameObject.GetComponent<Collider>().material.staticFriction = 100f;

        currentStaminaValue = 10f;
        recentlyWallRan = false;
    }

    void Update()
    {
        InputMethod();
        CheckJump();
        CalculateForward();
        CalculateRight();
        DrawDebugLines();
        Crouch();
        DidWallRun();
        IsKinematic();
        CheckGround();

        //anim.SetBool("animGrounded", isGrounded);
        //anim.SetBool("animFalling", !isGrounded);
        //anim.SetBool("animClimbing", climb.isClimbing);
        //anim.SetBool("animClimbUp", climb.climbingUp);
        //anim.SetBool("canJump", canJump);
        //anim.SetBool("animMoving", isMoving);
        //anim.SetBool("animSurfaceSwimming", volTrig.surfaceSwimming); // will replace swimming animation with something better, disable for now
        //anim.SetBool("animUnderwaterSwimming", volTrig.underwaterSwimming); // will replace swimming animation with something better, disable for now

        Debug.DrawRay(transform.position, currentVel * 20f, Color.red);
        Debug.DrawRay(transform.position, moveDirection * 20f, Color.yellow);
    }
    private void FixedUpdate()
    {
        Walk();
        ApplyGravity();
        Jump();
        climbStep();
    }

    public void InputMethod() //Input method including context sensitive speed adjustment for different states //TODO clean this method up a bit, could probably be split into 3 or more speerate methods more narrow in focus.
    {

        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        moveDirectionSliding = dirParent.right * horizontal + dirParent.forward * vertical;
        moveDirectionSlope = Vector3.ClampMagnitude(dirParent.right * horizontal + dirParent.forward * vertical, 1f);
        moveDirectionFlat = Vector3.ClampMagnitude(dirParent.right * horizontal + dirParent.forward * vertical, 1f);
        moveDirectionSwimming = fpCam.transform.right * horizontal + fpCam.forward * vertical;
        moveDirectionLeftDiagonal = Vector3.ClampMagnitude(-dirParent.right + new Vector3(15f, 0, 0) * horizontal + dirParent.forward * vertical, 1f); //For use in the step climbing code, allows us to check for steps diagonally from the movement direction as well
        moveDirectionRightDiagonal = Vector3.ClampMagnitude(dirParent.right + new Vector3(15f, 0, 0) * horizontal + dirParent.forward * vertical, 1f); //same as above but to the right

        if (!isGrounded /*&& !volTrig.surfaceSwimming && !volTrig.underwaterSwimming*/)
        {
            moveDirection = Vector3.Slerp(moveDirection, moveDirectionFlat, 10f);
        }
        else if (isGrounded/* || grapHook.isGrappling && !volTrig.surfaceSwimming && !volTrig.underwaterSwimming*/)
        {
            moveDirection = Vector3.Slerp(moveDirection, moveDirectionSlope, 10f);
        }
        else if (isGrounded && sliding/* && !volTrig.surfaceSwimming && !volTrig.underwaterSwimming*/)
        {
            moveDirection = Vector3.Slerp(moveDirection, moveDirectionSliding, 10f);
        }

        //if (volTrig.surfaceSwimming || volTrig.underwaterSwimming)
        //{
        //    moveDirection = Vector3.Slerp(moveDirection, moveDirectionSwimming, 10f);
        //}

        if (Input.GetKey(KeyCode.LeftShift) && currentStaminaValue > 0f && isMoving && !wallRun.isWallRunning && isGrounded/* && breathCheck.canBreathe*/)
        {
            moveSpeed = runSpeed;
            isRunning = true;
            StopCoroutine("CatchBreath");
        }
        else if (isMoving/* && !volTrig.surfaceSwimming && !volTrig.underwaterSwimming*/ && !wallRun.isWallRunning)
        {
            moveSpeed = walkSpeed;
            isRunning = false;
        }
        else if (isMoving/* && volTrig.surfaceSwimming || volTrig.underwaterSwimming*/)
        {
            moveSpeed = swimSpeed;
            isRunning = false;
        }
        else if (wallRun.isWallRunning/* && !grapHook.isGrappling*/)
        {
            moveSpeed = wallRunSpeed;
            isRunning = false;
        }
        else if (wallRun.isWallRunning/* && grapHook.isGrappling*/)
        {
            moveSpeed = walkSpeed;
            isRunning = false;
        }
        else if (!isMoving)
        {
            moveSpeed = 0f;
            isRunning = false;
        }

        if (isRunning/* || !breathCheck.canBreathe*/)
        {
            currentStaminaValue -= Time.deltaTime;
        }

        if (!isRunning && isGrounded/* && breathCheck.canBreathe*/)
        {
            StartCoroutine("CatchBreath");
        }

        if (Input.GetKey(KeyCode.C))
        {
            crouching = true;

            crouching = true;
            if (isRunning)
            {
                StartCoroutine("SlidingTime");
            }
            else
            {
                moveSpeed = crouchSpeed;
            }
        }
        else
        {
            crouching = false;
        }
    }

    private void ApplyGravity() //Checks the state of the player and applies gravity wuth different modifiers, context senstive.
    {
        playerRb.useGravity = false; //Using my own gravity strenght and gravity force direction. It is not always straight down, instead if is perpendicular to the ground to avoid the player sliding (so long as the slope angle isn't too steep).
        if (!isGrounded && !wallRun.isWallRunning/* && !grapHook.isGrappling*/ && !climb.isClimbing/* && !volTrig.surfaceSwimming && !volTrig.underwaterSwimming*/)
        {
            gravityStrength = normalGravityStrength;
            currentGravity = new Vector3(0f, gravityStrength * multiplier, 0f);
            playerRb.AddForce(currentGravity, ForceMode.Acceleration);

        }

        if (isGrounded)
        {
            gravityStrength = normalGravityStrength;
            currentGravity = new Vector3(0f, gravityStrength * multiplier, 0f);
            Vector3 gravityDirection = Vector3.Slerp(currentGravity, -groundRay.normal, 0.6f);
            playerRb.AddForce(gravityDirection, ForceMode.Acceleration);
        }

        if (wallRun.isWallRunning)
        {
            gravityStrength = wallRunGravityStrength;
            currentGravity = new Vector3(0f, gravityStrength * multiplier, 0f);
            playerRb.AddForce(currentGravity, ForceMode.Acceleration);
        }


        //if (grapHook.isGrappling)
        //{
        //    gravityStrength = noGravityStrength;
        //    currentGravity = new Vector3(0f, gravityStrength * multiplier, 0f);
        //    playerRb.useGravity = true;
        //    playerRb.AddForce(Physics.gravity = systemGravity * playerRb.mass * playerRb.mass, ForceMode.Acceleration);
        //    systemGravity = Physics.gravity = new Vector3(0f, grapplingGravityStrength, 0f);
        //}

        if (climb.isClimbing)
        {
            playerCol.enabled = false;
            gravityStrength = noGravityStrength;
            currentGravity = new Vector3(0f, gravityStrength * multiplier, 0f);
        }
        else
        {
            playerCol.enabled = true;
        }

        //if (volTrig.surfaceSwimming || volTrig.underwaterSwimming)
        //{
        //    gravityStrength = noGravityStrength;
        //    currentGravity = new Vector3(0f, gravityStrength * multiplier, 0f);
        //    playerRb.drag = 1f;
        //}
        //else if (!climb.isClimbing)
        //{
        //    playerRb.drag = 0.25f;
        //}

        //if (volTrig.inGas)
        //{
        //    playerRb.drag = 10f;
        //}
        //else
        //{
        //    playerRb.drag = 0.25f;
        //}

    }

    private void IsKinematic() // turns on kinematic mode for the playerRb when the player is climbing, will also be used for other situations in the future
    {
        if (climb.isClimbing)
        {
            playerRb.isKinematic = true;
        }
        else
        {
            playerRb.isKinematic = false;
        }
    }

    private void Walk() //countains all the grounded movement code uses physical forces instead of cordinate movement, this is essential for the physics based enviromental puzzles I want to design.
    {

        if (!climb.isClimbing /*|| grapHook.isGrappling*/ || wallRun.isWallRunning || recentlyWallRan /*|| volTrig.surfaceSwimming || volTrig.underwaterSwimming*/)
        {
            if (!isGrounded /*&& !volTrig.surfaceSwimming && !volTrig.underwaterSwimming*/)
            {
                playerRb.AddForce(moveDirection * moveSpeed * airSpeed * Time.fixedDeltaTime, ForceMode.Acceleration);
            }

            //if (volTrig.surfaceSwimming || volTrig.underwaterSwimming)
            //{
            //    Vector3 swimLine = Vector3.Lerp(playerRb.velocity, moveDirection * moveSpeed, Time.fixedDeltaTime * 10f);
            //    currentVel = swimLine;
            //    playerRb.velocity = currentVel;
            //}

            if (isGrounded && !groundSlide  && stepAngle <= maxSlopeAngle)
            {
                Vector3 moveLine = Vector3.Lerp(playerRb.velocity, moveDirection * moveSpeed, Time.fixedDeltaTime * 10f);
                moveLine.y = playerRb.velocity.y;

                currentVel = new Vector3(moveLine.x, moveLine.y, moveLine.z);
                playerRb.velocity = currentVel;
            }
        }

        if (horizontal <= 0.1 && horizontal > -0.1 && vertical <= 0.1 && vertical > -0.1)
        {
            isMoving = false;
        }
        else
        {
            isMoving = true;
        }

        if (!isMoving && isGrounded)
        {
            stopVel = new Vector3(0, playerRb.velocity.y, 0);
            playerRb.velocity = stopVel;
            this.gameObject.GetComponent<Collider>().material.staticFriction = 100f;
        }
        else
        {
            this.gameObject.GetComponent<Collider>().material.staticFriction = 0.1f;
        }

        var vel = playerRb.velocity;

        var localVel = dirParent.transform.InverseTransformDirection(vel);

        //anim.SetFloat("VerticalVel", localVel.z);
        //anim.SetFloat("HorizontalVel", localVel.x);
    }

    private void Crouch() //crouch, right now it sets the transform scale of the whole player, instead in the future this will use crouch animations and set height of collider to top of head bone.
    {
        normalScale = new Vector3(1f, 1f, 1f);
        crouchScale = new Vector3(1f, 1f * 0.5f, 1f);

        if (crouching)
        {
            this.transform.localScale = crouchScale;
        }
        else
        {
            this.transform.localScale = normalScale;
        }
    }


    private void CheckJump() //sets canjump to true if the player has been on the ground long enough, isn't on a "sticky" enviroment, and of course presses jump.
    {

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && groundTime >= 0.1f/* && !volTrig.inGas*/)
        {
            //anim.SetTrigger("jumpPressed");
            isGrounded = false; //needs to be triggered here instantly as well because we have a Ienumerator giving a slight delay when just moving off the ground without jumping (too stop jitter when moving over surfaces with holes in them (plank bridges ect).
            canJump = true;

        }
        else if (!isGrounded)
        {
            canJump = false;
            //anim.SetBool("jumpPressed", false);
        }

    }

    public void Jump() //because CheckJump is running in update, every time there is a physics step it will check if canjump is true and apply the force, must run in fixed update
    {
        if (canJump)
        {
            playerRb.velocity = new Vector3(playerRb.velocity.x, 0, playerRb.velocity.z);
            playerRb.AddRelativeForce(Vector3.up * jumpHeight * multiplier, ForceMode.Impulse);
            canJump = false;
        }
    }

    private void CheckGround() //checks if player is grounded and also returns the angle of the ground you are standing on (which must not exceed maxSlopeAngle in order to remain grounded).
    {

        float groundColliderOffset = 0.5f;
        RaycastHit groundHit;
        Vector3 rayOrigin = transform.position;
        rayOrigin.y = rayOrigin.y + groundColliderOffset;

        if(Physics.SphereCast(playerRb.position, playerCol.radius, Vector3.down, out groundHit, groundRayLength) && !groundSlide)
        {
            isGrounded = true;
            airTime = 0f;
            groundTime += Time.deltaTime;
            //anim.SetBool("jumpPressed", false);
        } else
        {
            airTime += Time.deltaTime;
            if (wallRun.isLeft || wallRun.isRight || wallRun.isFront/* || groundSlide*/)
            {
                isGrounded = false;
                groundTime = 0f;
                currentVel = playerRb.velocity;
            }
            else if (airTime >= 0.3f)
            {
                isGrounded = false;
                groundTime = 0f;
                currentVel = playerRb.velocity;
            }
        } 
        //else if ()
        //{
        //    airTime = 0f;
        //}

        groundSlopeDetected = Vector3.Angle(groundHit.normal, Vector3.up);
        if(groundSlopeDetected >= maxSlopeAngle)
        {
            groundSlide = true;
        } else
        {
            groundSlide = false;
        }
    }


    public void CalculateForward() //calculates the forward vector from our player so that it's always parallell with the ground normal (unless the slope is too steep (ie: we're not grounded)
    {
        if (!isGrounded)
        {
            forward = dirParent.forward;
            return;
        }
        if (isGrounded)
        {
            forward = Vector3.Cross(dirParent.right, groundRay.normal);
        }
    }

    private void CalculateRight() //does the same thing as forward calculation but for the right vector, used for smoothing diagnoal movement up and down slopes.
    {
        if (!isGrounded)
        {
            right = -dirParent.right;
            return;
        }

        right = Vector3.Cross(dirParent.forward, groundRay.normal);
    }

    private void DrawDebugLines()
    {
        if (!debug) { return; }
        Debug.DrawLine(transform.position, transform.position + forward * 7f, Color.blue);
        Debug.DrawLine(transform.position, transform.position + right * 7f, Color.green);
        Debug.DrawLine(transform.position, transform.position - Vector3.up * 2f, Color.green);
        Debug.DrawRay(transform.position, currentGravity, Color.blue, 10f);
    }


    private void DidWallRun() //ensures the player can't reenter wallrun mode mid-air if falling along an uneven wall
    {
        if (!wallRun.isWallRunning)
        {
            secondsSinceWallRun += Time.deltaTime;
        }
        else
        {
            secondsSinceWallRun = 0f;
        }

        if (secondsSinceWallRun < 2f)
        {
            recentlyWallRan = true;
        }
        else
        {
            recentlyWallRan = false;
        }
    }

    void climbStep() //allows the player to walk up stairs and/or over small obstacles on the ground seemlessly (without needing a tag).
    {
        stepRayLenght = playerCol.radius + 0.2f;
        RaycastHit bottomHit;
        RaycastHit bottomLeftHit;
        RaycastHit bottomRightHit;

        if (Physics.Raycast(bottomFoot.transform.position, moveDirection, out bottomHit, stepRayLenght - 0.1f))
        {
                RaycastHit topHit;
            if (!Physics.Raycast(topFoot.transform.position, moveDirection, out topHit, stepRayLenght + 0.2f) && !groundSlide)
            {
                stepAngle = Vector3.Angle(bottomHit.normal, Vector3.up); //checks if the angle of the obstacle is small enough walk "up" anyway instead of stepping over

                if (stepAngle >= maxSlopeAngle)
                {
                    playerRb.velocity = new Vector3(0, smoothStep, 0);
                }
            }

        }
        else if (Physics.Raycast(bottomFoot.transform.position, moveDirectionLeftDiagonal, out bottomLeftHit, stepRayLenght))
        {
            RaycastHit topLeftHit;
            if (!Physics.Raycast(topFoot.transform.position, moveDirectionLeftDiagonal, out topLeftHit, stepRayLenght) && !groundSlide)
            {
                stepAngle = Vector3.Angle(topLeftHit.normal, Vector3.up); //checks if the angle of the obstacle is small enough walk "up" anyway instead of stepping over

                if (stepAngle >= maxSlopeAngle)
                {
                    playerRb.velocity = new Vector3(0, smoothStep, 0);
                }
            }
        }
        else if (Physics.Raycast(bottomFoot.transform.position, moveDirectionRightDiagonal, out bottomRightHit, stepRayLenght))
        {
            RaycastHit topRightHit;
            if (!Physics.Raycast(topFoot.transform.position, moveDirectionRightDiagonal, out topRightHit, stepRayLenght) && !groundSlide)
            {
                stepAngle = Vector3.Angle(topRightHit.normal, Vector3.up); //checks if the angle of the obstacle is small enough walk "up" anyway instead of stepping over

                if (stepAngle >= maxSlopeAngle)
                {
                    playerRb.velocity = new Vector3(0, smoothStep, 0);
                }
            }

        }
        else
        {
            stepAngle = 0f;
        }
    }

    IEnumerator CatchBreath() //returns stamina to the player
    {

        yield return new WaitForSeconds(0.5f);

        while (currentStaminaValue < maxStamina/* && !volTrig.inGas*/)
        {
            currentStaminaValue += 0.01f * Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator SlidingTime() // lets the player slide on the ground for 2f, sliding lowers friction between the player and the ground
    {
        sliding = true;
        yield return new WaitForSeconds(2);
        if (!groundSlide)
        {
            sliding = false;
            moveSpeed = crouchSpeed;
        }
    }
}
