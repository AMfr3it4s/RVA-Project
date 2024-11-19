using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateModel : MonoBehaviour
{   
    [SerializeField] private GameObject modelObject;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnModel()
    {   
        Vector3 spawnPosition = new Vector3(1, 2, 2);
        Instantiate(modelObject, spawnPosition, transform.rotation);
    }
}