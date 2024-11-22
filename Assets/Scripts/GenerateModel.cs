using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateModel : MonoBehaviour
{   
    [SerializeField] private GameObject infoMenu;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnClickGenerate()
    {   
        infoMenu.SetActive(true);
    }
}
