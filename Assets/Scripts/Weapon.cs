using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public Transform firePoint;                     // 开火位置
    public GameObject bulletPrefab;                 // 子弹实例
    public AudioClip fireSound;                     // 开火音效

    private AudioSource weaponAudio;                // 武器音效控制器

    void Awake()
    {
        weaponAudio = GetComponent<AudioSource>();    
    }

    // 每帧都会执行一次
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) && !gameObject.GetComponent<Hold>().IsHold)
        {
            Shoot();    
        }
    }

    // 发射子弹
    void Shoot()
    {
        Instantiate(bulletPrefab, firePoint.transform.position, firePoint.transform.rotation);
        weaponAudio.PlayOneShot(fireSound, 0.2f);
    }
}
