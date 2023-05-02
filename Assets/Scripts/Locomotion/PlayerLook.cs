using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [Header("Manually assigned variables")]

    [SerializeField] private Transform camFollowTrans;
    [SerializeField] private Transform dirParent;
    //[SerializeField] private Transform hipBone;

    [Header("Editable in inspector")]
    [SerializeField] public float mouseSens = 100f;
    [SerializeField] public float snapSpeed = 10f;

    [Header("Visible for debugging")]
    [SerializeField] private float mouseX;
    [SerializeField] private float mouseY;

    [Header("Input stuff")]
    public float xRotation;
    public float yRotation;
    private float ClampedyRotation;
    private float ClampedxRotation;
    //private PlayerHealth playHealth;

    //assigned at start
    private PlayerSetup playerSetup;
    private Climbing climbing;
    private WallRun wallrun;
    private LobbyManager lobbyManager;

    [Header("Assigned from LobbyManager")]
    public Transform camParent;
    public Transform fpCamTrans;


    void Start()
    {

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        playerSetup = this.GetComponent<PlayerSetup>();
        //playHealth = FindAnyObjectByType<PlayerHealth>();
        climbing = this.GetComponent<Climbing>();
        wallrun = this.GetComponent<WallRun>();
        lobbyManager = FindAnyObjectByType<LobbyManager>();
        lobbyManager.playerLook = this;
        //playerSetup.playerLook = this;
        fpCamTrans = lobbyManager.camSys.GetComponentInChildren<Camera>().transform;
        camParent = lobbyManager.camSys.transform;
    }

    void Update()
    {
        GetInputs();

        //if (playHealth.isAlive) 
        //{
            if (climbing.isClimbing) // restricts camera moving when in climbing mode and snaps it to face the ledge on climbing entry.
            {
                var ledgeDir = climbing.hitpointLedge.normal;
                ledgeDir.y = 0;
                dirParent.transform.rotation = Quaternion.Slerp(dirParent.transform.rotation, Quaternion.LookRotation(-ledgeDir), Time.deltaTime * snapSpeed); //change snapSpeed to adjust smoothness

                fpCamTrans.transform.localRotation = Quaternion.Euler(ClampedxRotation, ClampedyRotation, 0);
            }
            else
            {
                Quaternion defaultCameraTilt = Quaternion.Euler(ClampedxRotation, 0, 0);

                if (!wallrun.isRight && !wallrun.isLeft || !wallrun.isWallRunning) 
                {
                    Vector3 tiltedCamera = fpCamTrans.transform.eulerAngles;
                    tiltedCamera = new Vector3(ClampedxRotation, 0, tiltedCamera.z);
                    Quaternion tiltedCameraQuat = Quaternion.Euler(tiltedCamera.x, tiltedCamera.y, tiltedCamera.z); //controls the camera tilt when not wallrunning to ensure a horizontal alignment

                    fpCamTrans.transform.localRotation = Quaternion.Slerp(tiltedCameraQuat, defaultCameraTilt, Time.deltaTime * 2f); //controls how the player looks up and down on the z-axis.

                }
                camParent.transform.Rotate(mouseX * Vector3.up, Space.World); // rotate camera left right
                dirParent.transform.Rotate(Vector3.up * mouseX); //rotates the character (directionParent) with the camera on the y axis.
            }
        //} else //makes the camera look at the the hip bone (which won't have same transform as parent due to ragdoll displacement on death).
        //{
            //Vector3 deathDir = hipBone.transform.position - fpCamTrans.position;
            //Quaternion deathRot = Quaternion.LookRotation(deathDir);
            //fpCamTrans.rotation = Quaternion.Slerp(fpCamTrans.rotation, deathRot, 0.5f * Time.deltaTime);
        //}
    }

    public void GetInputs() //function tracks the inputs.
    {
        mouseX = Input.GetAxisRaw("Mouse X") * mouseSens * Time.fixedDeltaTime;
        mouseY = Input.GetAxisRaw("Mouse Y") * mouseSens * Time.fixedDeltaTime;

        yRotation += mouseX;
        xRotation -= mouseY;


        ClampedxRotation = Mathf.Clamp(xRotation, -80f, 70f);
        ClampedyRotation = Mathf.Clamp(yRotation, -80f, 70f);
    }
}

