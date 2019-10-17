using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretEnemy : MonoBehaviour
{
    [SerializeField] private int lookAngle = 90;
    [SerializeField] private int lookAccurate = 2;
    [SerializeField] private float rayDistance = 10.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        ShowView();
    }

    void ShowView()
    {
        float subAngle = lookAngle / lookAccurate * 2;
        for(int i = 0; i < lookAccurate; i++)
        {
            Debug.DrawRay(transform.position, AfterRotate(-transform.up, subAngle * (i + 1)) * rayDistance, Color.green);
            Debug.DrawRay(transform.position, AfterRotate(-transform.up, -subAngle * (i + 1)) * rayDistance, Color.green);
        }
    }

    Vector2 AfterRotate(Vector2 direction, float angle)
    {
        return Quaternion.Euler(0, 0, angle) * direction;
    }
}
