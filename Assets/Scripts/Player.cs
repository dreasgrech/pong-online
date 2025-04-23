using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    public float speed = 10f;

    private float lastSynchronizationTime = 0f;
    private float syncDelay = 0f;
    private float syncTime = 0f;
    private Vector3 syncStartPosition = Vector3.zero;
    private Vector3 syncEndPosition = Vector3.zero;

    private float yLimit = 2.87f;
    private NetworkManager networkManager;

    private void Awake()
    {
        networkManager = FindObjectOfType<NetworkManager>();
    }

    void Update()
    {
        if (networkView.isMine)
        {
            InputMovement();
        }
        else
        {
            SyncedMovement();
        }
    }

    private void SyncedMovement()
    {
        syncTime += Time.deltaTime;
        transform.position = Vector3.Lerp(syncStartPosition, syncEndPosition, syncTime / syncDelay);
    }
 
    void InputMovement()
    {
        int direction = 0;
        if (Input.GetKey(KeyCode.W))
        {
            direction = 1;
        } else if (Input.GetKey(KeyCode.S))
        {
            direction = -1;
        }

        if (direction != 0)
        {
            var newY = transform.position.y + direction;
            newY = Mathf.Clamp(newY, -yLimit, yLimit);
            var newPosition = transform.position.ReplaceY(newY);
            //transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime*5);
            Vector3 curVel = Vector3.zero;
            transform.position = Vector3.SmoothDamp(transform.position, newPosition, ref curVel, 0.06f);
        }
    }

    void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {

        Vector3 syncPosition = Vector3.zero;
        if (stream.isWriting)
        {
            //// We're sending data
            syncPosition = transform.position;
            stream.Serialize(ref syncPosition);
            //Debug.Log("Sent position: " + syncPosition);
        }
        else
        {
            //// We're recieving data
            if (!networkManager.Ended)
            {
                stream.Serialize(ref syncPosition);
                //Debug.Log("Recieved position: " + syncPosition);

                syncTime = 0f;
                syncDelay = Time.time - lastSynchronizationTime;
                lastSynchronizationTime = Time.time;

                syncStartPosition = transform.position;
                syncEndPosition = syncPosition;
            }
        }
    }
}