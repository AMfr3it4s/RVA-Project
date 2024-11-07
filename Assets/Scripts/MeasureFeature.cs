using LearnXR.Core.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class MeasureFeature : MonoBehaviour
{
    
    public static MeasureFeature Instance { get; private set; }  // Singleton Instance

    [Range(0.005f, 0.05f)]
    [Header("Tape Properties")]
    [SerializeField] private float tapeWidth = 0.01f;
    [SerializeField] private Material tapeMaterial;
    [SerializeField] private GameObject tapePrefabInfo;
    [SerializeField] private Vector3 controllerOffset = new (0, 0.045f,0);
    [SerializeField] private string measurementInfoFormat = "<mark=#0000005A padding=\"20,20,10,10\"><color=white>{0}</color></mark>";
    [SerializeField] private float measurementInfoLength = 0.01f;
    [Header("Trigger Button")]
    [SerializeField] private OVRInput.Button tapeActionButton;
    [SerializeField] private OVRInput.Controller? currentController;
    [SerializeField] private OVRInput.Button clearActionButton;
    [Header("Controlers Transform")]
    [SerializeField] private Transform leftController;
    [SerializeField] private Transform rightController;

    private List<MeasureTape> savedTapeLines = new();
    private TextMeshPro lastMeasurementInfo;
    private LineRenderer lastTapeLineRenderer;
    private OVRCameraRig cameraRig;

    private void Awake()
    {
        // Implementação do Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        cameraRig = FindObjectOfType<OVRCameraRig>();
    }


    void Update()
    {
        HandleControllerActions(OVRInput.Controller.LTouch, leftController);
        HandleControllerActions(OVRInput.Controller.RTouch, rightController);
    }


    private void HandleControllerActions(OVRInput.Controller controller, Transform tapeArea)
    {
        if(currentController != controller && currentController != null) return;
        if (OVRInput.GetDown(tapeActionButton, controller))
        {   
            currentController = controller;
            HandleDownAction(tapeArea);
           
        }
        if (OVRInput.Get(tapeActionButton, controller))
        {
            HandleHoldAction(tapeArea);
           
        }
            
        if (OVRInput.GetUp(tapeActionButton, controller))
        {   
            currentController = null;
            HandleUpAction(tapeArea);
           
        }

    }

    private void HandleDownAction(Transform tapeArea) 
    {
        CreateNewTapeLine(tapeArea.position);
        AttachAndDetachMeasurementInfo(tapeArea);
    }
    private void HandleHoldAction(Transform tapeArea)
    {
        lastTapeLineRenderer.SetPosition(1, tapeArea.position);
        CalculateMeasurement();
        AttachAndDetachMeasurementInfo(tapeArea);
    }
    private void HandleUpAction(Transform tapeArea)
    {
        AttachAndDetachMeasurementInfo(tapeArea, false);
    }

    private void CreateNewTapeLine(Vector3 initialPosiiton)
    {
        var newTapeLine = new GameObject($"Tapeline_{savedTapeLines.Count}", typeof(LineRenderer));
        lastTapeLineRenderer = newTapeLine.GetComponent<LineRenderer>();
        lastTapeLineRenderer.positionCount = 2;
        lastTapeLineRenderer.startWidth = tapeWidth;
        lastTapeLineRenderer.endWidth = tapeWidth;
        lastTapeLineRenderer.material = tapeMaterial;
        lastTapeLineRenderer.SetPosition(0, initialPosiiton);
        
        lastMeasurementInfo = Instantiate(tapePrefabInfo, Vector3.zero, Quaternion.identity).GetComponent<TextMeshPro>();
        lastMeasurementInfo.GetComponent<BillboardAlignment>().AttachTo(cameraRig.centerEyeAnchor);
        lastMeasurementInfo.gameObject.SetActive(false);
        savedTapeLines.Add(new MeasureTape {
            Tape = newTapeLine,
            TapeInfo = lastMeasurementInfo

        } );
    }

    private void AttachAndDetachMeasurementInfo(Transform tapeArea, bool attachToController = true )
    {   
        
        //Atach Measurement while we're doing the measurement
        if (attachToController)
        {
            lastMeasurementInfo.gameObject.SetActive(true);
            lastMeasurementInfo.transform.SetParent(tapeArea.transform.parent);
            lastMeasurementInfo.transform.localPosition = controllerOffset;

        }
        else // Place info between both points
        {
            lastMeasurementInfo.transform.SetParent(lastTapeLineRenderer.transform);
            var lineDirection = lastTapeLineRenderer.GetPosition(0) - lastTapeLineRenderer.GetPosition(1);

            Vector3 lineCrossPorduct = Vector3.Cross(lineDirection, Vector3.up);

            //Mid Point

            Vector3 lineMidPoint = (lastTapeLineRenderer.GetPosition(0) + lastTapeLineRenderer.GetPosition(1)) / 2.0f;


            lastMeasurementInfo.transform.position = lineMidPoint + (lineCrossPorduct.normalized * measurementInfoLength);

        }
    }



    
    private void CalculateMeasurement()
    {
        var distance = Vector3.Distance(lastTapeLineRenderer.GetPosition(0), lastTapeLineRenderer.GetPosition(1));
        var centimeters = MeasureTape.MeterstoCentimeters(distance);
        var lastLine = savedTapeLines.Last();
        lastLine.TapeInfo.text = string.Format(measurementInfoFormat, $"{centimeters:F2}cm");
    }

    private void OnDestroy() => ClearAllTape();
    
    public void ClearAllTape()
    {
        foreach (var tapLine in savedTapeLines)
        {
            Destroy(tapLine.TapeInfo);
            Destroy(tapLine.Tape);
        }
        savedTapeLines.Clear();

    }
   
    
}
