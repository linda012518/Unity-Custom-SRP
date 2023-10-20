using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //float x = 0.5f;
        //float y = 0.9f;
        //float z = 0.2f;
        //float w = 0.3f;

        //float a = x + y / 255.0f;
        //float b = z + w / 255.0f;

        //float r = Mathf.Floor(a * 255.0f) / 255.0f;
        //float g = a * 255.0f;
        //Debug.Log(r);
        //Debug.Log(g);

        float x = 188;
        float y = 234;

        float a = 256 * x + y;

        int go = (int)a;
        int temp = go / 256;
        Debug.Log(temp);
        Debug.Log(go - temp * 256);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
