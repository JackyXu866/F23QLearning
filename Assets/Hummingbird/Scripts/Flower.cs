using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Manage a flower with nectar
/// </summary>
public class Flower : MonoBehaviour
{
    [Tooltip("the color when the flower is full")]
    public Color fullFlowerColor = new Color(1f, 0f, .3f);

    [Tooltip("the color when the flower is empty")]
    public Color emptyFlowerColor = new Color(.5f, 0f, 1f);

    /// <summary>
    /// the Trigger collider representing the nectar
    /// </summary>
    [HideInInspector]
    public Collider nectarCollider;



    //the solid collider representing the flower
    private Collider flowerCollider;

    //the flower's material
    private Material flowerMaterial;

    /// <summary>
    /// a vector pointing out of flower
    /// </summary>

    public Vector3 FlowerUpVector
    {
        get 
        { 
            return flowerCollider.transform.up; 
        }
    }

    public Vector3 FlowerCenterPosition
    {
        get
        {
            return nectarCollider.transform.position;
        }
    }

    public float nectarAmount { get; private set; }

    public bool hasNectar
    {
        get
        {
            return (nectarAmount > 0f);
        }
    }

    public float Feed(float amount)
    {
        //track how much nectar we successfully taken (cannot take more than available
        float nectarTaken = Mathf.Clamp(amount, 0f, nectarAmount);

        //subtract the amount
        nectarAmount -= nectarTaken;

        if (nectarAmount <= 0f)
        {
            //no nectar remaining
            nectarAmount = 0f;

            //disable flower and nectar collider;

            flowerCollider.gameObject.SetActive(false);
            nectarCollider.gameObject.SetActive(false);

            //change flower color to indicate it is empty

            flowerMaterial.SetColor("_BaseColor", emptyFlowerColor);
        }

        return nectarTaken;
    }


    public void resetFlower()
    {
        //refill nectar
        nectarAmount = 1f;

        //enable flower and nectar collider;

        flowerCollider.gameObject.SetActive(true);
        nectarCollider.gameObject.SetActive(true);

        //change flower color to indicate it is full

        flowerMaterial.SetColor("_BaseColor", fullFlowerColor);

    }

    private void Awake()
    {
        //find mesh renderer and get the right material
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        flowerMaterial =meshRenderer.material;

        //find flower and nectar colliders
        flowerCollider = transform.Find("FlowerCollider").GetComponent<Collider>();
        nectarCollider = transform.Find("FlowerNectarCollider").GetComponent<Collider>();
    }
}
