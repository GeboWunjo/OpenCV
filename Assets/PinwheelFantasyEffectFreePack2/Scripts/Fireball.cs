using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fireball : MonoBehaviour {

    public Rigidbody rgbd;
    public GameObject fieryParticle;
    public GameObject smokeParticle;
    public GameObject explosionParticle;
    private bool isColliding = false;

    private void OnCollisionEnter(Collision collision)
    {
        isColliding = true;
        fieryParticle.SetActive(false);
        smokeParticle.SetActive(false);
        explosionParticle.SetActive(true);        
        rgbd.constraints = RigidbodyConstraints.FreezeAll;
        Destroy(this.gameObject);
    }

    private void Update()
    {
        if(!isColliding)
            transform.position += transform.forward * 20.0f * Time.deltaTime;
    }

    private void OnDestroy()
    {
        //WaitForSeconds(2);
    }
}
