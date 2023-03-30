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
    [SerializeField] float gamma = 0.9f;
    [SerializeField] float epsilon = 0.2f;
    [SerializeField] float alpha = 0.5f;
    [SerializeField] int episode = 100000;
    [SerializeField] int maxStep = 5000;
    int stateSize = 8;     // num of checkpoints +1
    private int currEpisode = 0;
    private bool episodeFinish = false;
    private int currStep = 0;
    float[,] Q;
    int state;
    int stateCount = 0;
    float reward = 0;
    Vector3 ogPos;
    Quaternion ogRot;

    [Header("Raycast")]
    [SerializeField] int raySize = 6;
    [Range(0, 90)]
    [SerializeField] int halfAngle = 30;
    [SerializeField] float downDeg = -10;
    [SerializeField] float range = 20;
    [SerializeField] List<string> tags;
    int rayPoss = 0; // possible ray results

    TrackArea track;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        track = GetComponentInParent<TrackArea>();

        stateSize = track.checkpointSize + 1;
        rayPoss = tags.Count + 1;
        episodeFinish = false;

        ogPos = transform.position;
        ogRot = transform.rotation;
    }

    private void Awake()
    {
        if(raySize % 2 != 0) raySize++;

        stateCount = (int)Mathf.Pow(rayPoss, raySize) * stateSize;
        Q = new float[actionSize, stateCount];

        for (int i = 0;  i < actionSize; i++)
        {

            for(int j = 0; j < stateCount; j++)
            {
                Q[i, j] = Random.Range(-0.1f, 0.1f);
            }
        }

        state = 0;
    }


    // Update is called once per frame
    void FixedUpdate()
    {
        QLearning();
    }

    void UseAction(int i){
        switch (i)
        {
            case 0:
                CarAccelerate(1f);
                break;
            case 1:
                CarAccelerate(-1f);
                break;
            case 2:
                CarTurning(1f);
                break;
            case 3:
                CarTurning(-1f);
                break;
            default:
                break;
        }
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
                    // update state
                    state += (int)Mathf.Pow(rayPoss, index) * (i + 1);
                }

            }
            Debug.DrawRay(r.origin, r.direction * range * rangeConst, Color.red);
        }
        else
        {
            //update state
            Debug.DrawRay(r.origin, r.direction * range * rangeConst, Color.white);
        }

    }

    private void AddObservation(){
        state = 0;
        RayCaster(transform.forward, 0);
        RayCaster(-transform.forward, 1, 0.5f);
        for(int i=2; i<raySize; i+=2)
        {
            RayCaster(transform.forward + transform.right * halfAngle * Mathf.Deg2Rad * (i - 1), i);
            RayCaster(transform.forward - transform.right * halfAngle * Mathf.Deg2Rad * (i - 1), i + 1);
        }
        state += (int)Mathf.Pow(rayPoss, raySize) * checkpoint;
    }

    private void QLearning(){
        if(!isTraining) return;
        if(currEpisode >= episode) return;
        if(episodeFinish) return;

        AddObservation();

        int action = ChooseAction(state);
        float old_reward = reward;
        float old_Q = Q[action, state];
        int old_state = state;

        UseAction(action);

        AddObservation();

        int maxAction = ChooseAction(state);
        float maxQ = Q[maxAction, state];

        float new_Q = old_Q + alpha * (old_reward + gamma * maxQ - old_Q);
        Q[action, old_state] = new_Q;

        currStep++;

        if(currStep >= maxStep){
            episodeFinish = true;
            StartCoroutine(FinishEpisode());
        }
    }

    private IEnumerator FinishEpisode(){
        yield return new WaitForSeconds(1f);
        
        track.AreaReset();
        transform.position = ogPos;
        transform.rotation = ogRot;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        currEpisode++;
        reward = 0f;
        currStep = 0;
        checkpoint = 0;

        episodeFinish = false;
    }

    private int ChooseAction(int state){
        if(Random.Range(0f, 1f) < epsilon){
            return Random.Range(0, actionSize);
        }
        else{
            int bestAction = 0;
            float bestValue = Q[bestAction, state];
            for(int i = 1; i < actionSize; i++){
                if(Q[i, state] > bestValue){
                    bestAction = i;
                    bestValue = Q[i, state];
                }
            }
            return bestAction;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(isTraining && !episodeFinish){
            if(other.CompareTag("CheckPoint")){
                checkpoint++;
                AddReward(10f);
                other.enabled = false;
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
        }
    }

    private void OnTriggerStay(Collider other) {
        if(isTraining && !episodeFinish){
            if(other.CompareTag("Track")){
                AddReward(.05f * Time.deltaTime);
            }
            else if(other.CompareTag("Lawn")){
                AddReward(-.05f * Time.deltaTime);
            }
        }
    }

    private void AddReward(float r){
        reward += r;
    }


}
