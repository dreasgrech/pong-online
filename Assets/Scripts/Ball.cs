using UnityEngine;
using System.Collections;

public class Ball : MonoBehaviour {

    private float lastSynchronizationTime = 0f;
    private float syncDelay = 0f;
    private float syncTime = 0f;
    private Vector3 syncStartPosition = Vector3.zero;
    private Vector3 syncEndPosition = Vector3.zero;


    private Vector3 velocity;

    private float HeightLimit { get; set; }
    private float WidthLimit { get; set; }
    private bool BallMoving { get; set; }

    private float speed = 6f;

    private ScoreManager scoreManager;

	void Start ()
	{
	    scoreManager = (ScoreManager) FindObjectOfType(typeof (ScoreManager));
	}

    public void ServerStart(float heightLimit, float widthLimit)
    {
        HeightLimit = heightLimit;
        WidthLimit = widthLimit;

        StartCoroutine(HomelessMethods.InvokeInSeconds(2f, StartMovingBall));
    }
    
    private void StartMovingBall()
    {
        Debug.Log("Starting moving ball");
        transform.position = Vector3.zero;

        //var randomStartingAngle = Random.Range(0, 2 * Mathf.PI);
        var randomStartingAngle = Random.Range(0, Mathf.PI/4);
        //var randomStartingAngle = Random.Range(0, 359) * Mathf.Deg2Rad;
        velocity = new Vector3(Mathf.Cos(randomStartingAngle), Mathf.Sin(randomStartingAngle)) * speed;

        if (Network.isServer)
        {
            ToggleBallMoving(true);
        }

        rigidbody2D.isKinematic = false;
        rigidbody2D.velocity = velocity;

        /*
        StartCoroutine(HomelessMethods.Interpolate(speed, 1f, 300f, InterpolationMethods.Lerp, f =>
        {
            speed = f;
        }));
         */
    }

    [RPC]
    void ToggleBallMoving(bool state)
    {
        BallMoving = state;

        if (Network.isServer)
        {
            networkView.RPC("ToggleBallMoving", RPCMode.Others, state);
        }
    }

    void Update()
    {
        /*
        if (Network.isServer && moving)
        {
            if (!gravityEnabled)
            {
                // Decide where to move the ball and move it
                if (UnityEngine.Random.Range(0, 100) < 1)
                {
                    Debug.Log("GRAVITY");
                    gravityEnabled = true;
                    rigidbody2D.isKinematic = false;
                    rigidbody2D.gravityScale = 1f;
                    //rigidbody2D.AddForce(velocity * 300);
                    rigidbody2D.AddForce(velocity * 200);
                    StartCoroutine(HomelessMethods.InvokeInSeconds(3.3f, () =>
                    {
                        if (gravityEnabled)
                        {
                            TurnOffGravity();
                        }
                    }));
                }
                else
                {
                    UpdateServerPosition();
                }
            }

            var vel = gravityEnabled ? rigidbody2D.velocity.normalized : new Vector2(velocity.x, velocity.y).normalized;
            if (Mathf.Abs((transform.position.y) + vel.y) > HeightLimit)
            {
                TurnOffGravity();
            }

            transform.position = transform.position.ReplaceY(Mathf.Clamp(transform.position.y, -HeightLimit, HeightLimit));
        }
        else
        {
            syncTime += Time.deltaTime;
            transform.position = Vector3.Lerp(syncStartPosition, syncEndPosition, syncTime/syncDelay);
        }
        */

        if (Network.isServer)
        {
            if (BallMoving)
            {

                rigidbody2D.velocity = rigidbody2D.velocity.normalized*speed;

                if (transform.position.x < -WidthLimit)
                {
                    // Client scored
                    IncrementScoreAndResetBall((int) PlayerType.Client);
                }
                else if (transform.position.x > WidthLimit)
                {
                    // Server scored
                    IncrementScoreAndResetBall((int) PlayerType.Server);
                }
            }
        }
        else
        {
            /* TURN THESE TWO LINES BACK ON, OTHERWISE  THE CLIENT WONT WORK */
            syncTime += Time.deltaTime;

            transform.position = syncEndPosition == Vector3.zero ? Vector3.zero : Vector3.Lerp(syncStartPosition, syncEndPosition, syncTime/syncDelay);
        }
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        /*
        Debug.Log("normal:" + other.contacts[0].normal);
        Debug.Log("V:" + rigidbody2D.velocity);
        Debug.Log("Rel: " + other.relativeVelocity);
        */
        if (Network.isServer)
        {

            var currentVelocity = velocity;
            if (rigidbody2D.gravityScale > 0)
            {
                //rigidbody2D.gravityScale = 0;
                currentVelocity = other.relativeVelocity;
            }

            if (other.collider.name == "TopEdge" || other.collider.name == "BottomEdge")
            {
                var newVelocity = new Vector2(currentVelocity.x, currentVelocity.y*-1f);
                rigidbody2D.velocity = newVelocity;

                velocity = newVelocity;
            }

            if (other.collider.name.Contains("Paddle") || other.collider.name == "LeftEdge" || other.collider.name == "RightEdge")
            {
                var newVelocity = new Vector2(currentVelocity.x*-1f, currentVelocity.y);
                rigidbody2D.velocity = newVelocity;

                velocity = newVelocity;
            }
        }
    }

    [RPC]
    private void IncrementScoreAndResetBall(int playerType)
    {
        if (Network.isServer)
        {
            networkView.RPC("IncrementScoreAndResetBall", RPCMode.Others, playerType);
        }

        var type = (PlayerType) playerType;

        Debug.Log("Incrementing Score");
        scoreManager.IncrementScore((int)type);

        Debug.Log("Resetting ball");

        velocity = Vector3.zero;
        rigidbody2D.velocity = Vector2.zero;

        StartCoroutine(HomelessMethods.InvokeInSeconds(1f, () =>
        {
            transform.position = Vector3.zero;
        }));

        if (Network.isServer)
        {
            ToggleBallMoving(false);

            StartCoroutine(HomelessMethods.InvokeInSeconds(2f, StartMovingBall));
        }
    }

    void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        Vector3 syncPosition = Vector3.zero;
        Vector3 syncVelocity = Vector3.zero;
        if (stream.isWriting)
        {
            //// We're sending data
            syncPosition = transform.position;
            stream.Serialize(ref syncPosition);

            syncVelocity = rigidbody2D.velocity;
            stream.Serialize(ref syncVelocity);
        }
        else
        {
            //// We're recieving data
            stream.Serialize(ref syncPosition);
            stream.Serialize(ref syncVelocity);

            syncTime = 0f;
            syncDelay = Time.time - lastSynchronizationTime;
            lastSynchronizationTime = Time.time;

            syncEndPosition = syncPosition + syncVelocity * syncDelay;
            syncStartPosition = transform.position;
        }
    }
}
