using Photon.Pun;
using UnityEngine;

public class PlayerSetup : MonoBehaviour
{
    public PlayerMovement playerMovement; 
    public CameraFollow camFollow;
    public PlayerLook playerLook;
    public Climbing climbing;
    public WallRun wallRun;
    public GameObject fpCam;
    public GameObject directionParent;
    public LobbyManager lobbyManager;
    private PhotonView photonView;


    public void Awake()
    {
        lobbyManager = FindAnyObjectByType<LobbyManager>();
        photonView = GetComponent<PhotonView>();
        //lobbyManager.playerSetup = this;
        //fpCam = lobbyManager.camSys.gameObject;
        //camFollow = GetComponent<CameraFollow>();
        //playerLook = GetComponent<PlayerLook>();
        //climbing = GetComponent<Climbing>();
        //wallRun = GetComponent<WallRun>();
    }

    public void IsLocalPlayer() 
    {
        //if (photonView.IsMine)
        //{
        //    fpCam.SetActive(true);
        //    playerMovement.enabled = true;
        //    playerLook.enabled = true;
        //    camFollow.enabled = true;
        //    climbing.enabled = true;
        //    wallRun.enabled = true;
        //}
        //else
        //{
        //    fpCam.SetActive(false);
        //    playerMovement.enabled = false;
        //    playerLook.enabled = false;
        //    camFollow.enabled = false;
        //    climbing.enabled = false;
        //    wallRun.enabled = false;
        //}
        fpCam.SetActive(true);
        playerMovement.enabled = true;
        playerLook.enabled = true;
        camFollow.enabled = true;
        climbing.enabled = true;
        wallRun.enabled = true;

    }

}
