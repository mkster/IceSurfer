using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scaler : MonoBehaviour
{
    // Start is called before the first frame update
    float startTime;
    Vector3 targetScale;
    float speed = 3f;

    void Start()
    {
        startTime = Time.time;
        targetScale = transform.localScale;
        transform.localScale = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        float t = (Time.time - startTime) * speed;
        if (t >= 1){
            t = 1f;
            this.enabled = false; //done
        }
        transform.localScale = targetScale * t; //+ Vector3.one * 0.1f;
    }
}
