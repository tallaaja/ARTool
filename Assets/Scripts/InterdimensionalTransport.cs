using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class InterdimensionalTransport : MonoBehaviour {

    public Material[] materials;

	// Use this for initialization
	void Start () {

        foreach (var mat in materials)
        {
            mat.SetInt("_StencilTest", (int)CompareFunction.Equal);
        }

    }

    void OnTriggerStay(Collider other)
    {
        if (other.tag != "MainCamera")
            return;

        //outside of other world
        if(transform.position.z > other.transform.position.z)
        {

            foreach (var mat in materials)
            {
                Debug.Log("outside of other");
                mat.SetInt("_StencilTest", (int)CompareFunction.Equal);
            }

        }
        //inside of other dimension
        else
        {
            foreach(var mat in materials)
            {
                Debug.Log("inside of dimensision");
                mat.SetInt("_StencilTest", (int)CompareFunction.NotEqual);
            }
        }
    }


    void OnDestroy()
    {
        foreach (var mat in materials)
        {
            mat.SetInt("_StencilTest", (int)CompareFunction.Equal);
        }
    }
    // Update is called once per frame
    void Update () {
		
	}
}
