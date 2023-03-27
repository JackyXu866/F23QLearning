using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class CarController : Agent
{
    Rigidbody rb;

    [Header("Movement")]
    public float speed = 10f;
    public float angularSpeed = 200f;
    public float acceleration = 6f;
    [Range(0f, 1f)]
    public float deceleration = 0.2f;

    // ---------------------------------------------

    [Header("Race")]
    public int checkpoint = 0;

    public BoxCollider grounder;
    public bool grounded  = true;

    // ---------------------------------------------

    [Header("Training")]
    public bool isTraining = true;
    Vector3 ogPos;
    Quaternion ogRot;


    private TrackArea track;

    // ---------------------------------------------

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        ogPos = transform.position;
        ogRot = transform.rotation;
        if(!isTraining){
            MaxStep = 0;
        }
        track = GetComponentInParent<TrackArea>();
    }

    public override void OnEpisodeBegin()
    {
        if(isTraining){
            transform.position = ogPos;
            transform.rotation = ogRot;
            Debug.Log(ogPos);
            track.AreaReset();
        }

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        checkpoint = 0;
        
    }

    /// <summary>
    /// Index 0: Vertical
    /// Index 1: Horizontal
    /// <\summary>
    public override void OnActionReceived(ActionBuffers actions)
    {
        CarAccelerate(actions.ContinuousActions[0]);
        CarTurning(actions.ContinuousActions[1]);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;
        actions[0] = Input.GetAxis("Vertical");
        actions[1] = Input.GetAxis("Horizontal");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // space size + 4
        sensor.AddObservation(transform.localRotation.normalized);

        // space size + 1
        sensor.AddObservation(checkpoint);

        // space size + 3
        sensor.AddObservation(rb.velocity.normalized);
    }


    void CarAccelerate(float rate)
    {
        if (grounded)
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
    }

    void CarTurning(float rate)
    {
        if (Mathf.Abs(Vector3.Dot(rb.velocity, transform.forward)) > float.Epsilon && grounded)
        {
            rb.angularVelocity = transform.up * rate * angularSpeed;
            
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // grounded = true;
        // if(isTraining){
            if(other.CompareTag("CheckPoint")){
                checkpoint++;
                AddReward(10f);
                other.enabled = false;
                // Debug.Log("checkpoints: " + checkpoint);
            }
            else if(other.CompareTag("Fence")){
                AddReward(-2f);
            }
            else if(other.CompareTag("Lawn")){
                AddReward(-.5f);
            }
            else if(other.CompareTag("FinishLine") && checkpoint == track.checkpointSize){
                AddReward(60f);
                track.AreaReset();
                checkpoint = 0;
            }
        // }
    }

    private void OnTriggerStay(Collider other) {
        if(other.CompareTag("Track")){
            AddReward(.05f * Time.deltaTime);
        }
        else if(other.CompareTag("Lawn")){
            AddReward(-.05f * Time.deltaTime);
        }
    }

    // private void OnTriggerExit(Collider other)
    // {
    //     grounded = false;
    // }
}
