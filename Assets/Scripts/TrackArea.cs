using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackArea : MonoBehaviour
{

    List<GameObject> checkpoints;
    // Start is called before the first frame update
    void Start()
    {
        FindCheckPoints(this.transform);
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

    private void FindCheckPoints(Transform parent)
    {
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            Transform child = parent.transform.GetChild(i);
            if (child.CompareTag("CheckPoint"))
            {
                checkpoints.Add(child.gameObject);
                FindCheckPoints(child);
            }
        }
    }
}
