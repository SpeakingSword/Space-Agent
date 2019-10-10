using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveForce = 10.0f;       // 推动玩家前进的力，决定了玩家的移动速度
    [SerializeField] private float jumpForce = 100.0f;      // 玩家跳跃的力，决定玩家跳跃的高度
    private Rigidbody2D playerRigidbody2D;

    public TextMeshProUGUI moveSpeedText;                   // 在屏幕上显示玩家的移动速度

    private void Awake()
    {
        playerRigidbody2D = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        PlayerMove();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayerJump();
        }
    }

    private void Update()
    {
        moveSpeedText.text = "MoveSpeed: " + Mathf.RoundToInt(playerRigidbody2D.velocity.x);
    }

    void PlayerMove()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        if(horizontalInput > 0)
        {
            transform.localScale = new Vector2(0.1f, 0.1f);
            playerRigidbody2D.AddForce(new Vector2(moveForce, 0));
        }
        else if(horizontalInput < 0)
        {
            transform.localScale = new Vector2(-0.1f, 0.1f);
            playerRigidbody2D.AddForce(new Vector2(-moveForce, 0));
        }
    }

    void PlayerJump()
    {
            playerRigidbody2D.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
    }
}
