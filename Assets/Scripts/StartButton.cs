using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButton : MonoBehaviour
{   
    [SerializeField] private GameObject menu;
    [SerializeField] private GameObject menuFeature;



    public void OnStartButtonClick(){
       menu.SetActive(false);
       menuFeature.SetActive(true);
    }

   
}
