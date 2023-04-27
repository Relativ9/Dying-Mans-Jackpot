using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class LobbyManager : MonoBehaviourPunCallbacks
{

    public GameObject player;
    public GameObject CameraSystem;
    public Transform spawnPoint;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Connecting....");

        PhotonNetwork.ConnectUsingSettings();
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

        GameObject camSys = PhotonNetwork.Instantiate(CameraSystem.name, spawnPoint.position, Quaternion.identity);
        int camSysID = camSys.GetInstanceID();

        GameObject _player = PhotonNetwork.Instantiate(player.name, spawnPoint.position, Quaternion.identity);

        //camSys.GetComponent<PlayerSetup>().IsLocalCamera();
        _player.GetComponent<PlayerSetup>().IsLocalPlayer();
    }
}



