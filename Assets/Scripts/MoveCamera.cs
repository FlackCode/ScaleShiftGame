using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    public Transform CameraPosition;
    // Start is called before the first frame update
    void Start()
    {
        transform.position = CameraPosition.position;
        transform.rotation = CameraPosition.rotation; 
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = CameraPosition.position;
        transform.rotation = CameraPosition.rotation;
    }
}
