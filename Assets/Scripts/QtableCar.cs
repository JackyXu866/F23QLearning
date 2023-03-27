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

    bool isTraining = false;

    public int actionSize = 2;
    public int stateSize = 8;
    float gamma = 0.9f;
    float epsilon = 0.2f;
    float alpha = 0.5f;

    public int raySize = 6;
    [Range(0, 90)]
    public int halfAngle = 30;
    float rad;
    public float range = 20;

    public string[] tags;


    float[,] Q;

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
        int allobs = raySize * 5 + stateSize;
        Q = new float[actionSize, allobs];

        for (int i = 0;  i < actionSize; i++)
        {

            for(int j = 0; j < allobs; j++)
            {
                Q[i, j] = Random.Range(-0.1f, 0.1f);
            }
        }
    }


    // Update is called once per frame
    void FixedUpdate()
    {
        
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


    private void RayCaster(int index)
    {
        float finalrad;
        RaycastHit hit;
        float ran;
        if (index >= 0)
        {
            
            finalrad = Mathf.PI / 2 - rad + rad * index;
            ran = range;
        }
        else
        {
            finalrad = -Mathf.PI / 2 + rad - rad * index;
            ran = range / 2;
        }

        Vector3 v = Vector3.Normalize(new Vector3(Mathf.Cos(finalrad), 0, Mathf.Sin(finalrad)));


        Ray r = new Ray(transform.position, v * ran);

        if (Physics.Raycast(r, out hit, ran))
        {

        }
    }
}
