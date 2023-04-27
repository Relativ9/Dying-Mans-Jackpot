using System.Collections;
using UnityEngine;

public class Climbing : MonoBehaviour
{

    [Header("Manually assigned variables")]
    [SerializeField] private LayerMask climbableLayer;
    [SerializeField] private Transform dirParent;
    [SerializeField] private Transform topOfPlayer;
    [SerializeField] private Transform ledgeTopRayOrigin;

    //Assigned in start
    private PlayerMovement playerMovement;
    //private GrappleHook grapHook;
    private Transform player;
    private Rigidbody playerRb;
    private PlayerLook playerLook;

    [Header("Editable in inspector")]
    [SerializeField] private float peakHeight = 200f;

    [Header("Must remain publicly accessible")]
    public bool isClimbing;
    public bool climbingUp;
    public Vector3 standingPoint;
    public RaycastHit hitpointLedge;
    public bool canClimb;
    public bool canVault;
    public bool isPeaking;

    [Header("Visible for debugging")]
    [SerializeField] private bool middleHit;
    [SerializeField] private bool topHit;

    private Vector3 targetHeight;

    void Start()
    {
        playerMovement = FindAnyObjectByType<PlayerMovement>();
        //grapHook = FindAnyObjectByType<GrappleHook>();
        playerLook = FindAnyObjectByType<PlayerLook>();
        playerRb = this.gameObject.GetComponent<Rigidbody>();
        player = this.gameObject.transform;

        //Setting climbing bools just in case a scene transition happens in the middle of a climb
        middleHit = false;
        topHit = false;
        isClimbing = false;
        canClimb = true;
        canVault = false;
    }

    void Update()
    {
        TopCheck();
        MiddleCheck();
        HangDist();
        LedgeCheck();
        ActivateClimb();
        VaultCheck();
        LedgeMovement(); //must be the last method called.
    }

    public void LedgeCheck() //Check for valid ledge, set target height and standing point transforms so the animations will line up with the ledge.
    {
        RaycastHit ledgeHeightHit;
        if (Physics.Raycast(ledgeTopRayOrigin.transform.position, -ledgeTopRayOrigin.transform.up, out ledgeHeightHit, 1.5f, climbableLayer)) //ray is cast from a point above and ahead of the player to ensure you can only trigger the climbing mode on ledges you are facing.
        {
            Debug.DrawRay(ledgeTopRayOrigin.transform.position, -ledgeTopRayOrigin.transform.up, Color.cyan);
            var hitHeight = ledgeHeightHit.point;
            hitHeight.y = ledgeHeightHit.point.y - 1f;
            hitHeight.x = dirParent.position.x;
            hitHeight.z = dirParent.position.z;

            targetHeight = new Vector3(hitHeight.x, hitHeight.y, hitHeight.z);

            standingPoint = ledgeHeightHit.point;
        }
        else
        {
            standingPoint = Vector3.zero;
        }
    }

    public void TopCheck() //sends a ray out from the top of the player (empty gameobject) in the forward direction, must not hit a collider in 2 units 
    {
        RaycastHit hitTop;
        if (Physics.Raycast(topOfPlayer.position, topOfPlayer.forward, out hitTop, 2f, climbableLayer))
        {
            Debug.DrawRay(topOfPlayer.position, topOfPlayer.forward, Color.yellow);
            topHit = true;
        }
        else
        {
            topHit = false;
        }
    }

    public void MiddleCheck() // sends out a ray from the middle of the player in the forward direction, a false topHit pluss a positive middle hit should result in engaging climb mode. 
    {
        RaycastHit hitMiddle;
        if (Physics.Raycast(dirParent.position, dirParent.forward, out hitMiddle, 2f, climbableLayer))
        {
            Debug.DrawRay(dirParent.position, dirParent.forward, Color.cyan);
            hitpointLedge = hitMiddle;
            middleHit = true;

            if (isClimbing && !climbingUp)
            {
                player.transform.localPosition = Vector3.MoveTowards(player.transform.position, hitMiddle.point - new Vector3(0f, 0f, 0.3f), 1f * Time.deltaTime);
            }
        }
        else
        {
            middleHit = false;
        }
    }

    public void VaultCheck()
    {
        if (topHit)
        {
            canVault = false;
        }
        else if (!topHit && !middleHit)
        {
            canVault = false;
        }

        else if (topHit && middleHit && playerMovement.isGrounded)
        {
            canVault = true;
        }
    }

    public void LedgeMovement() //input function to climb up or drop off ledges, sideways movement on the ledge with root motion will be added later
    {
        if (isClimbing)
        {
            if (Input.GetKey(KeyCode.S))
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    StartCoroutine("JustClimbed");
                }
            }
            if (Input.GetKey(KeyCode.W))
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    StartCoroutine("ClimbUp");
                }
            }
        }
    }

    void ActivateClimb() //state check of the varius bools to initiate climbing mode
    {
        if (middleHit && !topHit && !playerMovement.isGrounded && canClimb /*&& !grapHook.isGrappling*/ && !isClimbing)
        {
            playerLook.yRotation = 0;
            playerLook.camParent.rotation = dirParent.transform.rotation;
            if (standingPoint != Vector3.zero)
            {
                isClimbing = true;
            }
        }
    }


    public void HangDist() //sets the height (y position) of the player as he hangs off the ledge, allowing the player to also peak over the edge on mouse 1, also snaps the camera to face the ledge.
    {
        if (isClimbing)
        {
            if (Input.GetMouseButton(1) && !playerMovement.isGrounded)
            {
                if (!isPeaking) // might replace this meathod of peaking with a root motion animation.
                {
                    isPeaking = true;
                    if (player.transform.position.y <= 0f)
                    {
                        Vector3 ledgePeak = new Vector3(player.transform.position.x, player.transform.position.y + 0.85f, player.transform.position.z);
                        player.transform.position = Vector3.Slerp(player.transform.position, ledgePeak, peakHeight * Time.deltaTime);
                        Debug.Log("THIS ISN*T WORKING 2!");
                    }
                    else if (player.transform.position.y >= 0f)
                    {
                        Vector3 ledgePeak = new Vector3(player.transform.position.x, player.transform.position.y + 0.85f, player.transform.position.z);
                        player.transform.position = Vector3.Slerp(player.transform.position, ledgePeak, peakHeight * Time.deltaTime);
                        Debug.Log("THIS ISN*T WORKING!");
                    }
                }
            }
            else if (!climbingUp)
            {
                isPeaking = false;
                playerRb.transform.localPosition = Vector3.MoveTowards(this.transform.localPosition, targetHeight, 300f * Time.deltaTime);
            }
            playerLook.camParent.rotation = dirParent.transform.rotation;
        }
    }
    IEnumerator JustClimbed() //climbing state change set with a delay to ensure it doesn't bug when falling past multiple edges.
    {
        canClimb = false;
        isClimbing = false;
        yield return new WaitForSeconds(0.5f);
        canClimb = true;
    }

    IEnumerator ClimbUp() //same as above
    {
        canClimb = false;
        climbingUp = true;
        yield return new WaitForSeconds(1.13f);
        canClimb = true;
        isClimbing = false;
        climbingUp = false;
    }
}
