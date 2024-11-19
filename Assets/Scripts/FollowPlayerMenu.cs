using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayerMenu : MonoBehaviour
{   
    [SerializeField] private Transform followPoint;

    [Range(0.001f, 3.0f)]
    [SerializeField] private float smoothSpeed;

    // A distância à frente do usuário em que o menu ficará posicionado
    public float distanceFromHead = 1.0f;

    private void Update()
    {
        Vector3 targetPosition = followPoint.position + followPoint.forward * distanceFromHead;
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, followPoint.rotation, smoothSpeed);
    }
}
