using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialButton : MonoBehaviour
{
    [SerializeField] private GameObject menu;
    [SerializeField] private GameObject tutorial;

    public void OpenTutorial(){
        menu.SetActive(false);
        tutorial.SetActive(true);
    }
}
