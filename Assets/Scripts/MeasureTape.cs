using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MeasureTape 
{
    public GameObject Tape;
    public TextMeshPro TapeInfo;
    public static double MetersToInches(double meters) => meters * 39.3701;
    public static double MeterstoCentimeters(double meters) => meters * 100;
}
