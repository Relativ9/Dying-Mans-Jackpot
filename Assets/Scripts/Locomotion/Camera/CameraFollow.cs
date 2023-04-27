using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Manually assigned variables")]
    [SerializeField] private Transform camFollowTrans;

    //[SerializeField] private Transform deathCamPos;
    //[SerializeField] private Transform fpCamTrans;


    private Transform cameraHolder;


[Header("Editable in inspector")]
    [SerializeField] float multiplier = 10f;
    [SerializeField] float deathCamSmooth = 0.1f;

    //private PlayerHealth playHealth;

    // Start is called before the first frame update
    void Start()
    {
        //playHealth = FindFirstObjectByType<PlayerHealth>();
        //fpCamTrans = Camera.main.transform;
        cameraHolder = Camera.main.GetComponentInParent<Transform>();
    }

    void Update()
    {
        //if(!playHealth.isAlive)
        //{           
        //    transform.position = Vector3.Slerp(transform.position, deathCamPos.position, deathCamSmooth * Time.deltaTime); //slowly moves the camera out from the player on death, also slows down time for a cool effect.
        //    //Time.timeScale = 0.5f;
        //} else
        //{
        //Time.timeScale = 1f;
        cameraHolder.position = Vector3.Slerp(transform.position, camFollowTrans.position, multiplier); //camera smoothly follows the player from set position, usually within childed to the head bone (when in first person).
        //}
    }
}
