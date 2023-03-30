using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlowerArea : MonoBehaviour
{
    //the diameter of the area flower and agent can be
    //used to observe relative distance
    public const float areaDiameter = 20;

    //the list of all flower plants in this area (plants has multiple flowers)
    private List<GameObject> flowerPlants;

    //a lookup dictionary for look up a flower from a nectar collider
    private Dictionary<Collider, Flower> nectarFlowerDictionary;
    
    //list of all flowers in the area
    public List<Flower> flowers { get; private set; }

    public void ResetFlowers()
    {
        //rotate each flower plant around y axis and subtly around x and z
        foreach (GameObject flowerPlant in flowerPlants)
        {
            float xRotation = Random.Range(-5f, 5f);
            float yRotation = Random.Range(-100f, 100f);
            float zRotation = Random.Range(-5f, 5f);
            flowerPlant.transform.rotation = Quaternion.Euler(xRotation, yRotation, zRotation);
        }

        //reset flowers
        foreach (Flower flower in flowers)
        {
            flower.resetFlower();
        }
    }

    public Flower GetFlowerFromNectar(Collider collider)
    {
        return nectarFlowerDictionary[collider];
    }

    private void Awake()
    {
        //init vars
        flowerPlants = new List<GameObject>();
        nectarFlowerDictionary = new Dictionary<Collider, Flower>();
        flowers = new List<Flower>();
    }

    void Start()
    {
        //find all flowers that are children of this gameobject/transform
        FindChildFlowers(transform);
    }

    //recursively find all flowers and flower plants of parent transform
    private void FindChildFlowers(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.CompareTag("flower_plant"))
            {
                //find a flower plant, add to list
                flowerPlants.Add(child.gameObject);

                //look for flower

                FindChildFlowers(child);
            }
            else
            {
                //not a flower plant, try to find flower component
                Flower flower = child.GetComponent<Flower>();
                if (flower != null)
                {
                    flowers.Add(flower);

                    nectarFlowerDictionary.Add(flower.nectarCollider, flower);

                    //no flower is children of other flower
                }
                else
                {
                    //flower component not found,so check children
                    FindChildFlowers(child);

                }
            }
        }
        
    }
}
