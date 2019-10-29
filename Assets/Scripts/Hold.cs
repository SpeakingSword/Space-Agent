using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hold : MonoBehaviour
{
    public Transform headPoint;                             // 玩家头部的位置
    public Transform footPoint;                             // 玩家脚部的位置
    public Transform firePoint;                             // 武器的开火位置
    public LayerMask carriable;                             // 设置什么东西可以拿
    [SerializeField] private int rayPrecision = 4;          // 控制射线的数量
    [SerializeField] private float rayDistance = 1f;        // 射线距离
    private bool isHold = false;                            // 检查玩家是否拿着东西
    private float raySpacing;                               // 射线的之间的间隔
    private GameObject carryThing = null;                   // 玩家拿着的东西

    public bool IsHold { get => isHold; set => isHold = value; }
    public GameObject CarryThing { get => carryThing; set => carryThing = value; }

    void Start()
    {
        // 计算射线之间的间隔
        raySpacing = (headPoint.position.y - footPoint.position.y) / rayPrecision;
    }

    // Update is called once per frame
    void Update()
    {
        // 如果玩家按下'E'键且手里没有东西则试着拾取前面被射线探测到的可拾取物品
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isHold)
            {
                GetObject();
            }
            // 放下当前手中拿着的物品
            else
            {
                // 取消方法的自动调用
                CancelInvoke("Carring");
                // 重新设置carryThing的刚体属性为Dynamic（这样它会重新受到力的作用）
                carryThing.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
                Debug.LogFormat("You throw it away!");
                isHold = false;
            }
        }

        // 将手中拿着的物品扔出去
        if(Input.GetKeyDown(KeyCode.Mouse0) && isHold)
        {
            CancelInvoke("Carring");
            carryThing.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
            carryThing.GetComponent<Rigidbody2D>().AddForce(transform.right * 50, ForceMode2D.Impulse);
            Debug.LogFormat("You throw it away!");
            isHold = false;
        }
    }

    // 如果射线探测到可拾取物品则设置该物品为玩家的carryThing
    // 并且设置carryThing的刚体属性为Static（这样它不会受到力的作用）
    // 最后更新carryThing的位置
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

                // 按时间间隔调用方法
                InvokeRepeating("Carring", 0, 0.01f);
                isHold = true;
                break;
            }
        }
    }

    // 更新被拾取物品的位置，保持其在武器的开火位置
    void Carring()
    {
        Vector3 offset = new Vector3(carryThing.GetComponent<BoxCollider2D>().size.x / 2, carryThing.GetComponent<BoxCollider2D>().size.y / 2, 0);
        carryThing.transform.position = firePoint.transform.position - offset;
    }
}
