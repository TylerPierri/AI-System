using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneHover : MonoBehaviour
{
    public Vector3 posOffset;
    Vector3 tempPos = new Vector3();

    public float amplitude; // distance for it to travel up and down
    public float frequency; // how fast do you want it to hover in a given time
    float rand;
    private void Start()
    {
        posOffset = gameObject.transform.position;
        rand = Random.Range(0, 1f);
    }
    private void Update()
    {
        Invoke("Hover", rand);
    }

    void Hover()
    {
        tempPos = posOffset;
        tempPos.y += (Mathf.Sin(Time.fixedTime * Mathf.PI * frequency) * amplitude) + 1.2f; // calculates a smooth tranistion when hovering up and down

        transform.position = tempPos;
    }
}
