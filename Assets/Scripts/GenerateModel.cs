using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateModel : MonoBehaviour
{   
    [SerializeField] private GameObject prefabObject;
    // Start is called before the first frame update
       public void OnClickGenerate()
    {   
        //infoMenu.SetActive(true);
        Vector3 position = new Vector3 (0,0.5f,1f);
        Instantiate(prefabObject, position, transform.rotation);
    }
}
