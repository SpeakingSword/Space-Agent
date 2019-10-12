using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    private float scaleX;
    private float scaleY;

    [SerializeField] private float moveForce = 10.0f;       // 推动玩家前进的力，决定了玩家的移动速度
    [SerializeField] private float jumpForce = 100.0f;      // 玩家跳跃的力，决定玩家跳跃的高度
    private Rigidbody2D playerRigidbody2D;

    private Transform footPoint;
    private CircleCollider2D footCollider;
    [SerializeField] private bool onGround;
    [SerializeField] private float rayDistance = 1.0f;

    public TextMeshProUGUI moveSpeedText;                   // 在屏幕上显示玩家的移动速度

    public LayerMask groundLayer;

    private void Awake()
    {
        playerRigidbody2D = GetComponent<Rigidbody2D>();
        footPoint = transform.Find("FootPoint");
        footCollider = GetComponent<CircleCollider2D>();
        scaleX = transform.localScale.x;
        scaleY = transform.localScale.y;
    }

    private void FixedUpdate()
    {
           
    }

    private void Update()
    {
        PlayerMove();
        IsOnGround();
        moveSpeedText.text = "MoveSpeed: " + Mathf.RoundToInt(playerRigidbody2D.velocity.x);
        
        if (Input.GetKeyDown(KeyCode.Space) && onGround)
        {
            PlayerJump();
        }
    }

    void PlayerMove()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        if(horizontalInput > 0)
        {
            transform.rotation = new Quaternion(0, 0, 0, 1);
            //transform.localScale = new Vector2(scaleX, scaleY);
            playerRigidbody2D.AddForce(new Vector2(moveForce, 0));
        }
        else if(horizontalInput < 0)
        {
            transform.rotation = new Quaternion(0, 180, 0, 1);
            //transform.localScale = new Vector2(-scaleX, scaleY);
            playerRigidbody2D.AddForce(new Vector2(-moveForce, 0));
        }
    }

    void PlayerJump()
    {
        playerRigidbody2D.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
        //Debug.Log("我跳起来了");
    }

    void IsOnGround()
    {
        Vector2 raysSartPosition = footPoint.transform.position;
        Vector2 rayDirection = Vector2.down;

        RaycastHit2D rayHits = Physics2D.Raycast(raysSartPosition, rayDirection, rayDistance, groundLayer);
        if(rayHits.collider != null)
        {
            //Debug.Log("在地上，可以跳");
            onGround = true;
        }
        else
        {
            //Debug.Log("不在地上，不可以跳");
            onGround = false;
        }
    }

    
}
