using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour {

    [SerializeField]
    private GameObject projectile;
    [SerializeField]
    private float speedX;
    [SerializeField]
    private float speedZ;

    private Vector3 moveDirection = Vector3.zero;
    private Transform trans;
    private bool turnDirection = false;
    private bool timerRefresh = false; 
    private static bool isFreeze = false;
    // Use this for initialization
    void Start()
    {
        trans = gameObject.GetComponent<Transform>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        Debug.Log(trans.position.x);
        if(isFreeze == false)
        {
            if (turnDirection == false)
            {
                trans.Translate(speedX * Time.deltaTime, 0, speedZ * Time.deltaTime);
                if (trans.position.x < -15)
                {
                    turnDirection = !turnDirection;
                }
            }
            else
            {
                trans.Translate(-speedX * Time.deltaTime, 0, speedZ * Time.deltaTime);
                if (trans.position.x > 15)
                {
                    turnDirection = !turnDirection;
                }
            }
        }
        else if(timerRefresh == false)
        {
            timerRefresh = true; 
            StartCoroutine(FreezeTimer()); 
        }
        

    }

    private void OnCollisionEnter(Collision collision)
    {
        
        if (collision.gameObject.tag == "Projectile")
        {
            Destroy(this.gameObject);
        }
    }

    public static void Freeze()
    {
        isFreeze = true;         
    }

    
    IEnumerator FreezeTimer()
    {
        yield return new WaitForSeconds(3);
        isFreeze = false;
        timerRefresh = false;
        Debug.Log(timerRefresh);
    }

}
