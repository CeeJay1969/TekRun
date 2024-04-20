using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;

public class PlayerControl : MonoBehaviour
{
    [SerializeField] AnimationCurve animationEaseCurve;

    public static PlayerControl instance;
    bool playerIsEnabled;
    PlayerState playerState;

    float engineCurrentPower = 30f;
    float engineDefaultPower = 70f;
    float acceleration = 5f;
    float turboAcceleration = 20f;

    Vector2 enginePowerLimit = new Vector2(30f, 160f);
    float engineTurboPowerLimit = 260f;
    bool engineTurboActive = false;

    float lateralCurrentPower = 0f;
    float lateralAcceleration = 4f;
    float percentTilt = 0.5f;
    Vector2 lateralPowerLimit = new Vector2(-40f, 40f);

    int hull;

    Rigidbody playerRb;
    Vector3 playerVelocity;
    Camera gameCamera;

    LaunchPadController launchPadControl;

    //Player states
    public enum PlayerState
    {
        NULL,
        PAUSE,
        INITIALIZE,
        LAUNCH,
        PLAYING,
        DESTROYING,
        DESTROYED,
        RESET
    }

    //Return the player state
    public PlayerState GetPlayerState { get { return playerState; } }
    public static bool PlayerPosition(ref Vector3 playerCurrentPosition)
    {
        if (instance.playerState == PlayerState.PLAYING)
        {
            playerCurrentPosition = instance.transform.position;
            return true;
        }

        return false;
    }

    private void Awake()
    {
        //Subscribe to GameController events
        GameController.instance.OnGameControllerMessage += OnPlayerConfigChange;

        instance = this;
        playerIsEnabled = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        playerRb = GetComponentInChildren<Rigidbody>();
        transform.position = new Vector3(0f, -50f, 0f);
        hull = 100;
        playerState = PlayerState.INITIALIZE;
    }

    // Update is called once per frame
    void Update()
    {
        //Waiting for launch pad to complete
        if (playerState == PlayerState.LAUNCH)
        {
            //Poll launch pad status
            if (launchPadControl.LaunchPadStatusValue == LaunchPadController.LaunchPadStatus.IDLE)
            {
                //Set mode and enable the controlls
                playerState = PlayerState.PLAYING;
                playerIsEnabled = true;

                //Send message to GameController that the manager that the player has launched
                JObject readyMessage = new()
                {
                    { "player-controller", "launched" }
                };
                ControllerMessages.OnControllerMessage(this, new ControllerMessages.ControllerMessage { JSONMessage = readyMessage.ToString() });
            }
        }

        //Do nothing if disabled
        if (playerIsEnabled == false) return;

        //Player hull is at zero
        if (hull <= 0)
        {
            playerState = PlayerState.DESTROYING;

            //Play the explosion animation, animation will automatically set to DESTROY state when completed
            playerState = PlayerState.DESTROYED;
        }

        if (playerState == PlayerState.PLAYING)
        {
            bool pressThrottle = false;

            //Check for turbo
            engineTurboActive = false;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                //Active the turbo!!
                engineTurboActive = true;
                engineCurrentPower += turboAcceleration;
            }

            //Throttle controls and turbo is not active
            if (engineTurboActive == false)
            {
                if (Input.GetKey(KeyCode.UpArrow))
                {
                    engineCurrentPower += acceleration;
                    pressThrottle = true;
                }

                if (Input.GetKey(KeyCode.DownArrow))
                {
                    engineCurrentPower -= acceleration;
                    pressThrottle = true;
                }
            }

            if (engineTurboActive == false)
                engineCurrentPower = Mathf.Clamp(engineCurrentPower, enginePowerLimit.x, enginePowerLimit.y);
            else
                //Engine limit if turbo is active:
                engineCurrentPower = Mathf.Clamp(engineCurrentPower, enginePowerLimit.x, engineTurboPowerLimit);

            //Slide engine power back to the default speed if throttle is not used
            if ((pressThrottle == false) && (engineCurrentPower != engineDefaultPower))
            {
                if (engineCurrentPower < engineDefaultPower)
                {
                    if ((engineCurrentPower + acceleration) >= engineDefaultPower)
                        engineCurrentPower = engineDefaultPower;
                    else
                        engineCurrentPower += acceleration;
                }
                if (engineCurrentPower > engineDefaultPower)
                {
                    if ((engineCurrentPower - acceleration) <= engineDefaultPower)
                        engineCurrentPower = engineDefaultPower;
                    else
                        engineCurrentPower -= acceleration;
                }
            }

            //Vary the lateral power based on speed
            float lateralPowerMultiplier = GetPlayerThrottlePercent() * 0.25f + 0.75f;

            //Lateral controls
            bool playerTurning = false;
            if (Input.GetKey(KeyCode.RightArrow))
            {
                lateralCurrentPower += lateralAcceleration;
                playerTurning = true;
            }
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                lateralCurrentPower -= lateralAcceleration;
                playerTurning = true;
            }
            lateralCurrentPower = Mathf.Clamp(lateralCurrentPower, lateralPowerLimit.x * lateralPowerMultiplier, lateralPowerLimit.y * lateralPowerMultiplier);
            if (playerTurning == false) lateralCurrentPower = 0f;

            if (playerTurning == true)
                playerVelocity = new Vector3(lateralCurrentPower, playerRb.velocity.y, engineCurrentPower);
            else
                playerVelocity = new Vector3(Mathf.Lerp(playerRb.velocity.x, 0f, 0.25f), playerRb.velocity.y, engineCurrentPower);

            //Camera field of view distortion
            float fieldOfView = Mathf.Clamp(playerRb.velocity.z - enginePowerLimit.y, 60f, 90f);
            fieldOfView = Mathf.Lerp(gameCamera.fieldOfView, fieldOfView, 0.15f);
            gameCamera.fieldOfView = fieldOfView;
        }

        if (playerState == PlayerState.DESTROYED)
        {
            //Send message to GameController that the manager that the player has been destroyed along with the player position
            JObject destroyMessage = new() { { "player-controller", "destroyed" } };
            ControllerMessages.OnControllerMessage(this, new ControllerMessages.ControllerMessage { JSONMessage = destroyMessage.ToString() });

        }
    }

    private void FixedUpdate()
    {
        //Do nothing if disabled
        if (playerIsEnabled == false) return;

        float levitatorLength = 5.5f;
        float levitatorTargetHeight = 5f;
        int layerMask = 1 << 6;

        Vector3 levitatorOrigin = playerRb.transform.position;

        RaycastHit rayCastInfo;

        if (Physics.Raycast(levitatorOrigin, Vector3.down, out rayCastInfo, levitatorLength, layerMask))
        {
            //Calculate the correcting vertical force
            float targetHeightDifference = levitatorTargetHeight - rayCastInfo.point.y + playerRb.transform.position.y;
            bool atTarget;
            playerVelocity.y = Utility.VelocityToPosition(playerRb.transform.position.y, playerRb.velocity.y, 5f, 0.5f, 0.25f,
                targetHeightDifference, 0f, Utility.TransitionType.FAST, out atTarget);
        }

        Utility.SetVelocityWithForce(playerRb, playerVelocity, true, new Vector3(0f, 0f, 0f));

        float localRollAngle = animationEaseCurve.Evaluate(playerRb.velocity.x / lateralPowerLimit.y) * percentTilt * lateralPowerLimit.y * -Mathf.Sign(playerVelocity.x);
        playerRb.transform.localRotation = Quaternion.Euler(0f, 0f, localRollAngle);
    }

    public float GetPlayerThrottlePercent()
    {
        float throttlePercent;
        if (engineTurboActive == false)
            throttlePercent = (engineCurrentPower - enginePowerLimit.x) / (enginePowerLimit.y - enginePowerLimit.x);
        else
            throttlePercent = (engineCurrentPower - enginePowerLimit.x) / (engineTurboPowerLimit - enginePowerLimit.x);
        return throttlePercent;
    }

    public float GetPlayerMaxLateralVelocity()
    {
        return lateralPowerLimit.x;
    }

    private void OnCollisionEnter(Collision collision)
    {
        //Player is waiting at the launch pad
        if (playerState == PlayerState.INITIALIZE)
        {
            //Get the camera reference
            gameCamera = CameraController.ChaseCamera;

            //Activate the launcher
            GameObject launchPadGameObject = collision.gameObject;
            launchPadControl = launchPadGameObject.GetComponent<LaunchPadController>();
            launchPadControl.LaunchPadStatusValue = LaunchPadController.LaunchPadStatus.GOING_UP;

            //Send intro message to the UI controller
            int currentGameLevel = GameController.CurrentGameLevel;
            JToken initMessageObject = SOCache.instance.SOCacheDictionary["Initialize_" + currentGameLevel.ToString()].Value<JObject>()["mission-brief-message"].Value<JToken>();
            string initMessage = initMessageObject[currentGameLevel.ToString()].ToString();
            string currentLevelMessage = currentGameLevel.ToString();
            currentLevelMessage = new String('0', 3 - currentLevelMessage.Length) + currentLevelMessage;

            //Text data
            JObject textProperty = new();
            JObject textProperties = new();
            textProperty.Add("header", "MISSION - " + currentLevelMessage);
            textProperty.Add("footer", initMessage);
            textProperties.Add("text", textProperty);

            /*
            //Style data
            JObject textStyle = new();
            JObject textStyles = new();
            textStyle.Add("header", "bold");
            textStyle.Add("footer", "italic");
            textStyles.Add("style", textStyle);

            //Color and size for header and footer
            JObject fontSetting1 = new();
            JObject fontSettings1 = new();
            fontSetting1.Add("font-size", 30);
            fontSetting1.Add("color", "#F0F00000");
            fontSettings1.Add("footer", fontSetting1);
            JObject fontSetting2 = new();
            JObject fontSettings2 = new();
            fontSetting1.Add("auto-size", true);
            fontSetting1.Add("color", "#80808000");
            fontSettings1.Add("header", fontSetting2);

            //Fade timing for header and footer
            JObject headerFadeProperty = new();
            JObject headerFadeProperties = new();
            headerFadeProperty.Add("initial-delay", 1);
            headerFadeProperty.Add("sustain-time", 4);
            headerFadeProperty.Add("header", headerFadeProperty);
            headerFadeProperties.Add("fade", headerFadeProperty);
            JObject footerFadeProperty = new();
            JObject footerFadeProperties = new();
            footerFadeProperty.Add("initial-delay", 2);
            footerFadeProperty.Add("sustain-time", 2);
            footerFadeProperty.Add("header", footerFadeProperty);
            footerFadeProperty.Add("fade", footerFadeProperty);
            */

            JObject missionProperties = new();
            missionProperties.Add("ui-controller", textProperties);
            //missionProperties.Add(fontSettings1);
            //missionProperties.Add(fontSettings2);

            ControllerMessages.OnUIControllerMessage(this, new ControllerMessages.UIControllerMessage { JSONMessage = missionProperties.ToString() });

            playerState = PlayerState.LAUNCH;
        }

        if (playerState == PlayerState.PLAYING)
        {
            //Destroy player
            hull = 0;
        }
    }

    void OnPlayerConfigChange(object sender, GameController.GameControllerMessage e)
    {
        string ArgMessage = e.JSONMessage;

        //Parse out the message
        JObject JSONObjects = JObject.Parse(@ArgMessage);

        //Step through the array of each JSONObject:
        foreach (var JSONObject in JSONObjects)
        {
            //Look for messages for the player controller
            if (JSONObject.Key == "player-controller") UpdatePlayerConfig(JSONObject.Value);
        }
    }

    void UpdatePlayerConfig(JToken playerConfigs)
    {
        foreach (var playerConfig in playerConfigs)
        {

            //Check the name and assign the values appropriately 
            var propertyName = playerConfig.Value<JProperty>().Name;
            var propertyValue = playerConfig.Value<JProperty>().Value;

            if (propertyName == "enabled")
            {
                bool playerEnabledStatus = playerConfig["enabled"].Value<bool>();
                playerIsEnabled = playerEnabledStatus;

                //If enabled get the camera reference
                if (playerEnabledStatus)
                {
                    gameCamera = CameraController.ChaseCamera;
                    transform.position = new Vector3(0f, 0f, 0f);
                }

                //Send message to GameController that the manager is ready
                JObject readyMessage = new()
                {
                    { "player-controller", "ready" }
                };
                ControllerMessages.OnControllerMessage(this, new ControllerMessages.ControllerMessage { JSONMessage = readyMessage.ToString() });
            }

            //Move the player to a new coordinate
            if (propertyName == "position")
            {
                JObject newVectorPosition = new();
                foreach (JToken vectorElement in propertyValue)
                {
                    newVectorPosition.Add(vectorElement.ToObject<JProperty>().Name, vectorElement.ToObject<JProperty>().Value);
                }
                transform.position = Utility.JSONtoVector3(newVectorPosition);
            }

            //Set player hull value
            if (propertyName == "hull-value")
            {
                hull = (int)propertyValue;
            }

            //Set the player state
            if (propertyName == "state")
            {
                Enum.TryParse<PlayerState>(propertyValue.ToString(), out playerState);
            }
        }
    }
}