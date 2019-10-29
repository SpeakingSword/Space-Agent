using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Transform target;               // 跟随目标
    [SerializeField] Vector3 offset;        // 相机与目标的位置偏差

    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;    
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.position = target.position + offset;
    }
}
