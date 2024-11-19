using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MeasureTape 
{
    public GameObject Tape;
    public TextMeshPro TapeInfo;
    public static double MeterstoCentimeters(double meters) => meters * 100;
}
