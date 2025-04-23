using UnityEngine;
using System.Collections;

public class Ball2 : MonoBehaviour
{
    private float speed = 6f;

    private SpriteRenderer spriteRenderer;
    private ScoreManager scoreManager;
    private NetworkManager networkManager;
    private Vector3 velocity;

    private float HeightLimit { get; set; }
    private float WidthLimit { get; set; }
    private bool Moving { get; set; }

	void Start ()
	{
	    spriteRenderer = GetComponent<SpriteRenderer>();
	    scoreManager = (ScoreManager) FindObjectOfType(typeof (ScoreManager));
	    networkManager = (NetworkManager) FindObjectOfType(typeof (NetworkManager));
	}

    /// <summary>
    /// This method is executed only on the SERVER
    /// </summary>
    /// <param name="heightLimit"></param>
    /// <param name="widthLimit"></param>
    public void ServerStart(float heightLimit, float widthLimit)
    {
        HeightLimit = heightLimit;
        WidthLimit = widthLimit;

        StartCoroutine(HomelessMethods.InvokeInSeconds(2f, () =>
        {
            var startingVelocity = GetRandomVelocity();
            networkView.RPC("InitializeAndStartMoving", RPCMode.All, startingVelocity);
        }));
    }

    /// <summary>
    /// This method runs on both peers at the same time.
    /// </summary>
    /// <param name="startingVelocity"></param>
    [RPC]
    private void InitializeAndStartMoving(Vector3 startingVelocity)
    {
        gravityDropCooldown.UpdateActionTime();

        // Set the initial position to the center of the screen
        transform.position = Vector3.zero;

        // Start moving the ball
        StartMovingBall(startingVelocity);
    }

    /// <summary>
    /// Starts moving the ball with the given velocity
    /// </summary>
    /// <param name="startVelocity"></param>
    private void StartMovingBall(Vector3 startVelocity)
    {
        Moving = true;

        SetVelocity(startVelocity);
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        var currentVelocity = velocity;
        if (rigidbody2D.gravityScale > 0)
        {
            rigidbody2D.gravityScale = 0;
            //currentVelocity = other.relativeVelocity;
            //rigidbody2D.gravityScale = 0f;
            
            /*
            if (Mathf.Abs(currentVelocity.x) < 0.5)
            {
                //currentVelocity = currentVelocity.ReplaceX(0.5f * Mathf.Sign(currentVelocity.x));
            }
            */
        }

        if (other.collider.name == "TopEdge" || other.collider.name == "BottomEdge")
        {
            var newVelocity = new Vector2(currentVelocity.x, currentVelocity.y*-1f);

            SetVelocity(newVelocity);
        }

        if (other.collider.name.Contains("Paddle") || other.collider.name == "LeftEdge" || other.collider.name == "RightEdge")
        {
            var newVelocity = new Vector2(currentVelocity.x*-1f, currentVelocity.y);

            SetVelocity(newVelocity);
        }
    }

	void FixedUpdate ()
	{
	    rigidbody2D.velocity = rigidbody2D.velocity.normalized*speed;

        // From the server, check if the ball went out of bounds to increase the score
        if (Network.isServer && Moving)
        {
            PlayerType? scoringPlayer = null;
            if (transform.position.x < -WidthLimit)
            {
                // Client scored
                scoringPlayer = PlayerType.Client;
            }
            else if (transform.position.x > WidthLimit)
            {
                // Server scored
                scoringPlayer = PlayerType.Server;
            }

            if (scoringPlayer.HasValue)
            {
                var nextRoundVelocity = GetRandomVelocity();
                networkView.RPC("IncrementScoreAndResetBall", RPCMode.All, (int)scoringPlayer.Value, nextRoundVelocity);
            }

            /*
            if (rigidbody2D.gravityScale == 0f && UnityEngine.Random.Range(0f,1f) < 0.1f)
            {
                if (gravityDropCooldown.CanWeDoAction())
                {
                    gravityDropCooldown.UpdateActionTime();
                    rigidbody2D.gravityScale = 1f;
                    StartCoroutine(HomelessMethods.InvokeInSeconds(0.5f, () =>
                    {
                        //rigidbody2D.gravityScale = 0f;
                    }));
                }
            }*/
        }

        //Debug.Log(rigidbody2D.velocity);
    }

    [RPC]
    private void IncrementScoreAndResetBall(int playerType, Vector3 nextVelocity)
    {
        var type = (PlayerType) playerType;

        var pitchDelta = 0.03f;
        var pitchDeltaSigned = type == PlayerType.Client ? pitchDelta : -pitchDelta;
        StartCoroutine(HomelessMethods.Interpolate(networkManager.musicAudioSource.pitch, networkManager.musicAudioSource.pitch + pitchDeltaSigned, 0.5f, InterpolationMethods.Lerp, d =>
        {
            networkManager.musicAudioSource.pitch = d;
        }));

        Moving = false;
        SetVelocity(Vector3.zero);
        rigidbody2D.gravityScale = 0f;

        StartCoroutine(HomelessMethods.InvokeInSeconds(1f, () =>
        {
            transform.position = Vector3.zero;
        }));

        var newScore = scoreManager.IncrementScore((int)type);

        if (newScore < networkManager.MaxScore)
        {
            // Keep on playing since no one won yet
            StartCoroutine(HomelessMethods.InvokeInSeconds(2f, () => StartMovingBall(nextVelocity)));
        } else
        {
            //// Someone won!

            // Hide the ball
            spriteRenderer.enabled = false;

            if (Network.isServer)
            {
                // Someone won
                networkManager.Server_PlayerWon(type);
            }
        }
    }

    readonly CooldownTimer clientSyncServerTimer = new CooldownTimer(1f);
    readonly CooldownTimer gravityDropCooldown = new CooldownTimer(5f);

    void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        var syncPosition = Vector3.zero;
        var syncVelocity = Vector3.zero;
        var syncGravityScale = 0f;

        if (stream.isWriting)
        {
            // SERVER.
            syncPosition = transform.position;
            syncVelocity = rigidbody2D.gravityScale > 0f ? new Vector3(rigidbody2D.velocity.x, rigidbody2D.velocity.y) : velocity;
            syncGravityScale = rigidbody2D.gravityScale;

            stream.Serialize(ref syncPosition);
            stream.Serialize(ref syncVelocity);
            stream.Serialize(ref syncGravityScale);
        } else
        {
            // CLIENT.
            stream.Serialize(ref syncPosition);
            stream.Serialize(ref syncVelocity);
            stream.Serialize(ref syncGravityScale);

            if (clientSyncServerTimer.CanWeDoAction())
            {
                //Debug.Log("Matching server");
                clientSyncServerTimer.UpdateActionTime();

                var distanceToServerCorrectPosition = Vector3.Distance(transform.position, syncPosition);
                if (distanceToServerCorrectPosition >= 0.2f)
                {
                    Debug.Log(string.Format("Correction position from {0} to {1}.  Distance: {2}", transform.position, syncPosition, distanceToServerCorrectPosition));
                    transform.position = syncPosition;
                    SetVelocity(syncVelocity);
                }
            }

            rigidbody2D.gravityScale = syncGravityScale;
        }
    }

    private void SetVelocity(Vector3 vel)
    {
        velocity = vel;
        rigidbody2D.velocity = vel;
    }

    private Vector3 GetRandomVelocity()
    {
        //return Vector3.right*speed;
        var randomAngle = Random.Range(0, Mathf.PI/4);
        return new Vector3(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * speed;
    }
}
