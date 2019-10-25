using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public Transform firePoint;
    public GameObject bulletPrefab;
    public AudioClip fireSound;

    private AudioSource weaponAudio;

    void Awake()
    {
        weaponAudio = GetComponent<AudioSource>();    
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) && !gameObject.GetComponent<Hold>().IsHold)
        {
            Shoot();
            weaponAudio.PlayOneShot(fireSound, 0.2f);
        }
    }

    // 发射子弹
    void Shoot()
    {
        Instantiate(bulletPrefab, firePoint.transform.position, firePoint.transform.rotation);       
    }
}
