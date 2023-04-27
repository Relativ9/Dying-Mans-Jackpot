using UnityEngine;

public class PlayerSetup : MonoBehaviour
{
    public PlayerMovement playerMovement; 
    public CameraFollow camFollow;
    public PlayerLook playerLook;
    public Climbing climbing;
    public WallRun wallRun;

    public void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        camFollow = GetComponent<CameraFollow>();
        playerLook = GetComponent<PlayerLook>();
        climbing = GetComponent<Climbing>();
        wallRun = GetComponent<WallRun>();
    }

    public void IsLocalPlayer() 
    {
        playerMovement.enabled = true;
        playerLook.enabled = true;
        camFollow.enabled = true;
        climbing.enabled = true;
        wallRun.enabled = true;
    }
}
