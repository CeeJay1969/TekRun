using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableMe : MonoBehaviour
{
    float resetDistance = 200f;

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 playerPosition = PlayerControl.instance.transform.position;

        //Check to see if player has passed this track piece, if so delete it
        if (playerPosition.z > transform.position.z + resetDistance)
        {
            //Special disable case
            if (gameObject.CompareTag("Fog"))
            {
                //Remove all particles and disable it
                ParticleSystem particleDisable = GetComponentInChildren<ParticleSystem>();
                particleDisable.Clear();
                particleDisable.Stop();
            }
            //Return gameobject to pooler
            string poolName = gameObject.name.Replace("(Clone)","");
            ObjectPooler.ObjectPoolDictionary[poolName].Release(gameObject);
        }
    }
}
