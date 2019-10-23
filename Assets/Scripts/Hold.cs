using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hold : MonoBehaviour
{
    public Transform headPoint;
    public Transform footPoint;
    public Transform firePoint;
    public LayerMask carriable;
    [SerializeField] private int rayPrecision = 4;          // 控制射线的数量
    [SerializeField] private float rayDistance = 1f;        // 射线距离
    private bool isHold = false;                            // 检查玩家是否拿着东西
    private float raySpacing;
    private GameObject carryThing = null;

    public bool IsHold { get => isHold; set => isHold = value; }
    public GameObject CarryThing { get => carryThing; set => carryThing = value; }

    void Start()
    {
        raySpacing = (headPoint.position.y - footPoint.position.y) / rayPrecision;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isHold)
            {
                GetObject();
            }
            else
            {
                CancelInvoke("Carring");
                carryThing.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
                Debug.LogFormat("You throw it away!");
                isHold = false;
            }
        }

        if(Input.GetKeyDown(KeyCode.Mouse0) && isHold)
        {
            CancelInvoke("Carring");
            carryThing.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
            carryThing.GetComponent<Rigidbody2D>().AddForce(transform.right * 50, ForceMode2D.Impulse);
            Debug.LogFormat("You throw it away!");
            isHold = false;
        }
    }

    void GetObject()
    {
        for(int i = 0; i < rayPrecision - 1; i++)
        {
            Debug.DrawRay(footPoint.position + new Vector3(0, (i + 1) * raySpacing, 0), transform.right * rayDistance, Color.green, 1);
            RaycastHit2D hit = Physics2D.Raycast(footPoint.position + new Vector3(0, (i + 1) * raySpacing, 0),
                                                 transform.right,
                                                 rayDistance,
                                                 carriable);
            if(hit.collider != null)
            {
                Debug.LogFormat("You are now carring a thing!");
                carryThing = hit.collider.gameObject;
                carryThing.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
                InvokeRepeating("Carring", 0, 0.01f);
                isHold = true;
                break;
            }
        }
    }

    void Carring()
    {
        Vector3 offset = new Vector3(carryThing.GetComponent<BoxCollider2D>().size.x / 2, carryThing.GetComponent<BoxCollider2D>().size.y / 2, 0);
        carryThing.transform.position = firePoint.transform.position - offset;
    }
}
