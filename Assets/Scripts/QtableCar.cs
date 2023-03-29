using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QtableCar : MonoBehaviour
{
    Rigidbody rb;

    [Header("Movement")]
    public float speed = 15f;
    public float angularSpeed = 3f;
    public float acceleration = 8f;
    [Range(0f, 1f)]
    public float deceleration = 0.5f;
    int checkpoint = 0;


    [Header("Q Learning")]
    [SerializeField] bool isTraining = false;
    [SerializeField] int actionSize = 2;
    [SerializeField] int stateSize = 8;
    [SerializeField] float gamma = 0.9f;
    [SerializeField] float epsilon = 0.2f;
    [SerializeField] float alpha = 0.5f;
    float[,] Q;
    float[] states;
    int stateCount = 0;

    [Header("Raycast")]
    [SerializeField] int raySize = 6;
    [Range(0, 90)]
    [SerializeField] int halfAngle = 30;
    [SerializeField] float downDeg = -10;
    [SerializeField] float range = 20;
    [SerializeField] List<string> tags;


    float rad;

    TrackArea track;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        track = GetComponentInParent<TrackArea>();
        rad = Mathf.Rad2Deg * rad;
    }

    private void Awake()
    {
        if(raySize % 2 != 0) raySize++;

        stateCount = raySize * tags.Count + stateSize;
        Q = new float[actionSize, stateCount];

        for (int i = 0;  i < actionSize; i++)
        {

            for(int j = 0; j < stateCount; j++)
            {
                Q[i, j] = Random.Range(-0.1f, 0.1f);
            }
        }

        states = new float[stateCount];
    }


    // Update is called once per frame
    void FixedUpdate()
    {
        RayCaster(transform.forward, 0);
        RayCaster(-transform.forward, 1, 0.5f);
        for(int i=2; i<raySize; i+=2)
        {
            RayCaster(transform.forward + transform.right * halfAngle * Mathf.Deg2Rad * (i - 1), i);
            RayCaster(transform.forward - transform.right * halfAngle * Mathf.Deg2Rad * (i - 1), i + 1);
        }
        AddObservation();

        QLearning();
    }


    void CarAccelerate(float rate)
    {
        
            rb.velocity = Mathf.Sign(Vector3.Dot(rb.velocity, transform.forward)) * rb.velocity.magnitude * transform.forward;
            rb.AddForce(acceleration * rate * transform.forward * rb.mass, ForceMode.Force);
            //rb.AddForceAtPosition(acceleration * rate * transform.forward * rb.mass, transform.TransformPoint(new Vector3(0, 0.3f, -1.2f)));

            if (rb.velocity.magnitude > speed)
            {
                rb.velocity = Mathf.Sign(Vector3.Dot(rb.velocity, transform.forward)) * speed * transform.forward;

            }
            else if (Mathf.Abs(rate) < Mathf.Epsilon)
            {
                //rb.velocity = Mathf.Sign(Vector3.Dot(rb.velocity, transform.forward)) * rb.velocity.magnitude * transform.forward;
                rb.AddForce(rb.velocity.magnitude / speed * acceleration * deceleration * (-Mathf.Sign(Vector3.Dot(rb.velocity, transform.forward))) * transform.forward * rb.mass, ForceMode.Force);

            }

        
    }

    void CarTurning(float rate)
    {
        if (Mathf.Abs(Vector3.Dot(rb.velocity, transform.forward)) > float.Epsilon)
        {
            rb.angularVelocity = transform.up * rate * angularSpeed;

        }
    }


    private void RayCaster(Vector3 dir, int index, float rangeConst = 1f)
    {
        dir = dir + transform.up * Mathf.Tan(downDeg * Mathf.Deg2Rad);

        RaycastHit hit;
        Ray r = new Ray(transform.position + transform.up * 0.5f, dir);

        if (Physics.Raycast(r, out hit, range * rangeConst))
        {
            for(int i = 0; i < tags.Count; i++)
            {
                if (hit.collider.CompareTag(tags[i]))
                {
                    states[stateSize-1 + index*tags.Count + i] = 1;
                }
                else
                {
                    states[stateSize-1 + index*tags.Count + i] = 0;
                }
            }
            Debug.DrawRay(r.origin, r.direction * range * rangeConst, Color.red);
        }
        else
        {
            for (int i = 0; i < tags.Count; i++)
            {
                states[stateSize-1 + index*tags.Count + i] = 0;
            }
            Debug.DrawRay(r.origin, r.direction * range * rangeConst, Color.white);
        }

    }

    private void AddObservation(){
        Quaternion r = transform.localRotation.normalized;
        states[0] = r.x;
        states[1] = r.y;
        states[2] = r.z;
        states[3] = r.w;
        Vector3 v = rb.velocity.normalized;
        states[4] = v.x;
        states[5] = v.y;
        states[6] = v.z;
        states[7] = checkpoint;
    }

    private void QLearning(){

    }
}
