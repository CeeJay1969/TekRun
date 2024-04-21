using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    ParticleSystem explosionParticle;

    private void Awake()
    {
        explosionParticle = GetComponent<ParticleSystem>();
        explosionParticle.Play();
    }

    private void OnEnable()
    {
        explosionParticle.Play();
    }

    // Update is called once per frame
    void Update()
    {
        //Wait for animation to complete
        if (explosionParticle.isStopped)
        {
            //Animation completed, start the player reset
            JObject playerResetMessage = new()
            {
                {"player-controller","destroyed"}
            };

            //Send message to GameController that animation has completed
            ControllerMessages.OnControllerMessage(this, new ControllerMessages.ControllerMessage { JSONMessage = playerResetMessage.ToString() });

            //Send it back to the object pool
            explosionParticle.Stop();
            string poolName = gameObject.name.Replace("(Clone)", "");
            ObjectPooler.ObjectPoolDictionary[poolName].Release(gameObject);
        }
            
    }
}
