using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackArea : MonoBehaviour
{
    [SerializeField] GameObject cpParent;
    List<GameObject> checkpoints;
    public int checkpointSize = 0;
    // Start is called before the first frame update
    void Start()
    {
        FindCheckPoints();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void Awake()
    {
        checkpoints = new List<GameObject>();
    }

    public void AreaReset()
    {
        foreach (GameObject checkpoint in checkpoints)
        {
            BoxCollider collider = checkpoint.GetComponent<BoxCollider>();
            if (collider != null)
                collider.enabled = true;
        }
    }

    private void FindCheckPoints()
    {
        foreach (Transform child in cpParent.transform)
        {
            checkpoints.Add(child.gameObject);
        }
        checkpointSize = checkpoints.Count - 1;
    }
}
