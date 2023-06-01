using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Unity.VisualScripting;

public class LobbyManager : MonoBehaviourPunCallbacks
{

    public GameObject player;
    public GameObject cameraSystem;
    public Transform spawnPoint;
    public WallRun wallrun;
    private PhotonView photonView;

    public GameObject camSys;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Connecting....");

        PhotonNetwork.ConnectUsingSettings();
        //cameraSystem.gameObject.SetActive(false);
    }


    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();

        Debug.Log("Connected to Server");

        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();
        PhotonNetwork.JoinOrCreateRoom("MainLobby", null, null);

        Debug.Log("we're in the Lobby!");
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        Debug.Log("Connected and in a room now!");
        camSys = PhotonNetwork.Instantiate(cameraSystem.name, spawnPoint.position, Quaternion.identity);
        GameObject _player = PhotonNetwork.Instantiate(player.name, spawnPoint.position, Quaternion.identity);
        photonView = _player.GetComponent<PhotonView>();
        _player.GetComponent<PlayerSetup>().fpCam = camSys.GetComponentInChildren<Camera>().gameObject;
        _player.GetComponent<PlayerMovement>().fpCam = camSys.GetComponentInChildren<Camera>().transform;
        _player.GetComponent<PlayerLook>().fpCamTrans = camSys.GetComponentInChildren<Camera>().transform;
        _player.GetComponent<CameraFollow>().camFollowTrans = _player.transform.Find("DirectionParent/Head");
        _player.GetComponent<CameraFollow>().cameraHolder = camSys.transform;
        //playerLook.camParent = camSys.transform.parent.transform;
        //camSys.gameObject.SetActive(true);

        if(photonView.IsMine)
        {
            _player.GetComponent<PlayerSetup>().IsLocalPlayer();
        }
    }
}



