using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuFreature : MonoBehaviour
{
    [SerializeField] private GameObject menu;
    [SerializeField] private OVRInput.Button buttonMenu;
    [SerializeField] private GameObject measurementMenu;
    [SerializeField] private GameObject tapArea1;
    [SerializeField] private GameObject tapArea2;



    private void Awake() => menu.SetActive(false);


    //Trancking Menu Open or Not
    private void Update()
    {
        if (OVRInput.GetDown(buttonMenu))
        {   
            //Animation For The Menu PopUp
            menu.SetActive(!menu.activeSelf);
            
        }

        
    }

    //Delete All Tapes In Scene
    public void DeleteAll() => MeasureFeature.Instance.ClearAllTape();

    //Enable Measurement Feature
    public void EnableMeasurement(){
        measurementMenu.SetActive(!measurementMenu.activeSelf);
        tapArea1.SetActive(!tapArea1.activeSelf);
        tapArea2.SetActive(!tapArea2.activeSelf);
    }

    //Quit App Funciton

    public void OnQuitButtonClick()
    {
        Application.Quit();
    }
    
    

    
}
