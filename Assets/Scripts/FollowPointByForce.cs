using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPointByForce : MonoBehaviour
{
    public Transform target;

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Following...");
        gameObject.GetComponent<Rigidbody2D>().AddForce(-transform.right);
    }
}
