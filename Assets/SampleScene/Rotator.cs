using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
{
    public Vector3 rotate;

    void Update()
    {
        transform.Rotate(rotate * Time.deltaTime * 50);
    }
}
