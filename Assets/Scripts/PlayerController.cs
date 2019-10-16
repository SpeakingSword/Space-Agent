using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    
    [SerializeField] private float moveForce = 10.0f;       // 推动玩家前进的力，决定了玩家的移动速度
    [SerializeField] private float jumpForce = 100.0f;      // 玩家跳跃的力，决定玩家跳跃的高度
    private Rigidbody2D playerRigidbody2D;                  // 获取玩家的刚体， 用来做物理效果
    private Transform footPoint;                            // 玩家的脚的位置
    private int playerHealth = 100;                         // 玩家的生命值

    [SerializeField] private float rayDistance = 1.0f;      // 射线的距离，用来判断玩家是否在地面
    private Animator playerAnimator;                        // 玩家的动画控制器

    public TextMeshProUGUI moveSpeedText;                   // 在屏幕上显示玩家的移动速度
    public TextMeshProUGUI playerHealthText;
    public LayerMask groundLayer;                           // 场景里的地面层级

    public int PlayerHealth
    {
        set { playerHealth = value; }
        get { return playerHealth; }
    }

    private void Awake()
    {
        playerRigidbody2D = GetComponent<Rigidbody2D>();
        footPoint = transform.Find("FootPoint");
        playerAnimator = GetComponent<Animator>();

        
    }

    private void Update()
    {
        if(playerHealth == 0)
        {
            Debug.Log("The player dead, and the anemy can't detect him!");
            gameObject.GetComponent<CapsuleCollider2D>().enabled = false;
            gameObject.GetComponent<Weapon>().enabled = false;
            gameObject.GetComponent<Melee>().enabled = false;
            gameObject.GetComponent<PlayerController>().enabled = false;  
        }

        // 显示移动速度和生命值
        moveSpeedText.text = "MoveSpeed: " + Mathf.RoundToInt(playerRigidbody2D.velocity.x);
        playerHealthText.text = "Health: " + playerHealth;

        // 角色移动
        PlayerMove();
        
        // 跳跃
        if (Input.GetKeyDown(KeyCode.Space) && IsOnGround())
        {
            PlayerJump();
            playerAnimator.SetBool("Jump_b", true);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 落地的时候取消跳跃动画
        if(collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Crate"))
        {
            playerAnimator.SetBool("Jump_b", false);
        }
    }

    void PlayerMove()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        if(horizontalInput > 0)
        {
            transform.rotation = new Quaternion(0, 0, 0, 1);
            playerRigidbody2D.AddForce(new Vector2(moveForce, 0));
        }
        else if(horizontalInput < 0)
        {
            // 反转玩家的方向
            transform.rotation = new Quaternion(0, 180, 0, 1);
            playerRigidbody2D.AddForce(new Vector2(-moveForce, 0));
        }

        // 设置动画的Speed_f参数
        playerAnimator.SetFloat("Speed_f", Mathf.Abs(playerRigidbody2D.velocity.x));
    }

    void PlayerJump()
    {
        playerRigidbody2D.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
    }

    // 通过射线判断玩家是否站在地面上
    bool IsOnGround()
    {
        Vector2 raysSartPosition = footPoint.transform.position;
        Vector2 rayDirection = Vector2.down;

        RaycastHit2D rayHits = Physics2D.Raycast(raysSartPosition, rayDirection, rayDistance, groundLayer);
        if(rayHits.collider != null)
        {  
            return true;
        }
        else
        {
            return false;
        }
    }

    
}
