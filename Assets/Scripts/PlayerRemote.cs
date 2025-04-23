
using UnityEngine;
using System.Collections;

public class PlayerRemote : MonoBehaviour
{
    public bool simulatePhysics = true;
    public bool updatePosition = true;
    public float physInterp = 0.1f;
    public float netInterp = 0.2f;
    public float ping;
    public float jitter;
    public GameObject localPlayer;      //The "Player" GameObject for which this game instance is authoritative. Used to determine if we should be calculating physics on the object this script is controlling, in case it could be colliding with this game instance's "player"
    public bool isResponding = false;         //Updated by the script for diagnostic feedback of the status of this <span class="posthilit">NetworkView</span>
    public string netCode = " (No Connection)";   //Updated by the script for diagnostic feedback of the status of this <span class="posthilit">NetworkView</span>
    private int m;
    private Vector3 p;
    private Quaternion r;
    private State[] states = new State[15];
    private int stateCount;

    internal struct State
    {
        internal Vector3 p;
        internal Quaternion r;
        internal float t;
    }
   void Start()
    {
        networkView.observed = this;
    }
    void FixedUpdate()
    {
        if (!updatePosition)
            return;

        simulatePhysics = (localPlayer && Vector3.Distance(localPlayer.rigidbody2D.transform.position, rigidbody2D.transform.position) < 30);
        jitter = Mathf.Lerp(jitter, Mathf.Abs(ping - ((float)Network.time - states[0].t)), Time.deltaTime * 0.3f);
        ping = Mathf.Lerp(ping, (float)Network.time - states[0].t, Time.deltaTime * 0.3f);

        rigidbody2D.isKinematic = !simulatePhysics;
        rigidbody2D.interpolation = (simulatePhysics ? RigidbodyInterpolation2D.Interpolate : RigidbodyInterpolation2D.None);

        //Interpolation
        float interpolationTime = (float)Network.time - netInterp;
        if (states[0].t > interpolationTime)
        {                        // Target playback time should be present in the buffer
            for (int i = 0; i < stateCount; i++)
            {                          // Go through buffer and find correct state to play back
                if (!states[i].Equals(null) && (states[i].t <= interpolationTime || i == stateCount - 1))
                {
                    State rhs = states[Mathf.Max(i - 1, 0)];                            // The state one slot newer than the best playback state
                    State lhs = states[i];                                            // The best playback state (closest to .1 seconds old)
                    float l = rhs.t - lhs.t;                      // Use the time between the two slots to determine if interpolation is necessary
                    float t = 0.0f;                                                    // As the time difference gets closer to 100 ms, t gets closer to 1 - in which case rhs is used
                    if (l > 0.0001) t = ((interpolationTime - lhs.t) / l);          // if t=0 => lhs is used directly
                    if (simulatePhysics)
                    {
                        transform.position = Vector3.Lerp(rigidbody2D.transform.position, Vector3.Lerp(lhs.p, rhs.p, t), physInterp);
                        transform.rotation = Quaternion.Slerp(rigidbody2D.transform.rotation, Quaternion.Slerp(lhs.r, rhs.r, t), physInterp);
                        rigidbody2D.velocity = ((rhs.p - states[i + 1].p) / (rhs.t - states[i + 1].t));
                    }
                    else
                    {
                        transform.position = Vector3.Lerp(lhs.p, rhs.p, t);
                        transform.rotation = Quaternion.Slerp(lhs.r, rhs.r, t);
                    }
                    isResponding = true;
                    netCode = "";
                    return;
                }
            }
        }
        //Extrapolation
        else
        {
            float extrapolationLength = (interpolationTime - states[0].t);
            if (extrapolationLength < 1 && !states[0].Equals(null) && !states[1].Equals(null))
            {
                if (!simulatePhysics)
                {
                    transform.position = states[0].p + (((states[0].p - states[1].p) / (states[0].t - states[1].t)) * extrapolationLength);
                    transform.rotation = states[0].r;
                }
                isResponding = true;
                if (extrapolationLength < .5) netCode = ">";
                else netCode = " (Delayed)";
            }
            else
            {
                netCode = " (Not Responding)";
                isResponding = false;
            }
        }
        if (simulatePhysics && states[0].t > states[2].t)
            rigidbody2D.velocity = ((states[0].p - states[2].p) / (states[0].t - states[2].t));
    }
    void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        //We are the server, and have to keep track of relaying messages between connected clients
        if (stream.isWriting)
        {
            if (stateCount == 0) return;
            p = states[0].p;
            r = states[0].r;
            m = (int)((Network.time - states[0].t) * 1000);    //m is the number of milliseconds that transpire between the packet's original send time and the time it is resent from the server to all the other clients
            stream.Serialize(ref p);
            stream.Serialize(ref r);
            stream.Serialize(ref m);
        }

        else
        {
            stream.Serialize(ref p);
            stream.Serialize(ref r);
            stream.Serialize(ref m);
            State state = new State();
            state.p = p;
            state.r = r;
            state.t = (float)info.timestamp - (m > 0 ? ((m) / 1000) : 0);
            if (stateCount == 0) states[0] = state;
            else if (state.t > states[0].t)
            {
                for (int k = states.Length - 1; k > 0; k--) states[k] = states[k - 1];
                states[0] = state;
            }
            stateCount = Mathf.Min(stateCount + 1, states.Length);
        }
    }
}