using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuFreature : MonoBehaviour
{
    [SerializeField] private GameObject menu;
    [SerializeField] private OVRInput.Button buttonMenu;

    private void Awake() => menu.SetActive(false);

    private void Update()
    {
        if (OVRInput.GetDown(buttonMenu))
        {
            menu.SetActive(!menu.activeSelf);
        }
    }

    public void DeleteAll() => MeasureFeature.Instance.ClearAllTape();
}
