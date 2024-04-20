using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Timeline;
using UnityEngine;

public class LaunchPadController : MonoBehaviour
{
    [SerializeField] float launchPadSpeed;
    [SerializeField] AnimationCurve animationEaseCurve;
    [SerializeField] Vector2 launchHeight;

    float launchSpeedStep;
    float launchTopHeight;
    float launchBottomHeight;
    public LaunchPadController instance;

    Rigidbody launchPadRB;

    public enum LaunchPadStatus { IDLE, GOING_UP, GOING_DOWN };
    LaunchPadStatus launchPadStatusValue = LaunchPadStatus.IDLE;

    public LaunchPadStatus LaunchPadStatusValue
    {
        set { launchPadStatusValue = value; }
        get { return launchPadStatusValue; }
    }

    public Vector3 LaunchPadPosition
    {
        get { return transform.position; }
    }

    private void Awake()
    {
        launchPadStatusValue = LaunchPadStatus.IDLE;
        launchPadSpeed = 0.2f;
        launchTopHeight = launchHeight.x;
        launchBottomHeight = launchHeight.y;
        launchSpeedStep = 1f / Mathf.Abs(launchTopHeight - launchBottomHeight);

        launchPadRB = GetComponent<Rigidbody>();
        instance = this;

        //Send message to GameController that the launchpad is ready along with the coordinates of where the target ship should be
        JObject readyPosition = new();
        //Convert the position of the launch pad to JSON
        readyPosition.Add("pad-position", Utility.Vector3toJSON(launchPadRB.transform.position));
        //Add the gameobject ID for identification
        readyPosition.Add(new JProperty("id", gameObject.GetInstanceID()));

        JObject readyMessage = new();
        readyMessage.Add("launch-pad-control", readyPosition);
        ControllerMessages.OnControllerMessage(this, new ControllerMessages.ControllerMessage { JSONMessage = readyMessage.ToString() });
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 currentLaunchPadPosition = transform.position;
        
        //Move launch pad up
        if ((launchPadStatusValue == LaunchPadStatus.GOING_UP) && (currentLaunchPadPosition.y < launchTopHeight + transform.parent.position.y))
        {
            if (currentLaunchPadPosition.y + launchPadSpeed > launchTopHeight + transform.parent.position.y)
                currentLaunchPadPosition.y = launchTopHeight + transform.parent.position.y;
            else
                currentLaunchPadPosition.y += launchPadSpeed;
            
            launchPadRB.MovePosition (new Vector3 (currentLaunchPadPosition.x, currentLaunchPadPosition.y, currentLaunchPadPosition.z));

            //Lift has reached the top  ... change status to IDLE
            if (currentLaunchPadPosition.y == launchTopHeight + transform.parent.position.y)
            {
                launchPadStatusValue = LaunchPadStatus.IDLE;
            }

        }

        //Move launch pad down
        if ((launchPadStatusValue == LaunchPadStatus.GOING_DOWN) && (currentLaunchPadPosition.y > launchBottomHeight + transform.parent.position.y))
        {
            if (currentLaunchPadPosition.y - launchPadSpeed < launchBottomHeight + transform.parent.position.y)
                currentLaunchPadPosition.y = launchBottomHeight + transform.parent.position.y;
            else
                currentLaunchPadPosition.y -= launchPadSpeed;

            launchPadRB.MovePosition (new Vector3 (currentLaunchPadPosition.x, currentLaunchPadPosition.y, currentLaunchPadPosition.z));

            //Lift has reached the bottom  ... change status to IDLE
            if (currentLaunchPadPosition.y == launchBottomHeight + transform.parent.position.y)
            {
                launchPadStatusValue = LaunchPadStatus.IDLE;
            }
        }
    }
}
