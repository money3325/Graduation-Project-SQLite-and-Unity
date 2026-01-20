using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerContrl : MonoBehaviour
{
    [SerializeField]//想要私有暴露外边用这个
    private float speed = 1f;
    private Rigidbody2D rb;//刚体组件

    // Update is called once per frame
      void Start()
    {
        // 尝试获取Rigidbody2D（如果添加了则用物理移动，更平滑且带碰撞）
        rb = GetComponent<Rigidbody2D>();
    }
    void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");//进行对比，GetAxisRaw消除惯性，GetAxis会有惯性
        float vertical = Input.GetAxisRaw("Vertical");
        //normalized是用来防止斜线走速度不一致
        Vector2 move = new Vector2(horizontal, vertical).normalized;
        //transform.Translate(move * speed * Time.deltaTime);//Time.deltaTime跟着帧数走，不容易因配置而不同
        //有刚体情况下的移动
        if(rb!=null)
        {
            rb.velocity = move * speed;
        }
    }
}
