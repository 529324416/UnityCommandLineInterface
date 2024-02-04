using System.Collections;
using System.Collections.Generic;
using RedSaw.CommandLineInterface;
using UnityEngine;

public class Example : MonoBehaviour
{
    [CommandProperty("player")]
    private static Example Instance { get; set; }

    public Vector3 pos{
        get => transform.position;
        set => transform.position = value;
    }

    public void Start(){
        Example.Instance = this;
    }

    public void Jump(float value = 10){
        GetComponent<Rigidbody>().AddForce(Vector3.up * value, ForceMode.Impulse);
        transform.rotation = Random.rotation;
    }
}
