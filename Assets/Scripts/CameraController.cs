using Newtonsoft.Json.Linq;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    static public CameraController instance;

    //Camera offset from player
    Vector3 _cameraOffset = new Vector3(0f, 5f, -7.5f);

    static private Camera _chaseCamera;
    static public Camera ChaseCamera
    {
        get { return _chaseCamera; }
    }

    GameObject _playerReference;
    Rigidbody _playerRb;
    PlayerControl _playerControl;


    float percentTilt = 0.45f;
    float velocityDeadZone = 15f;
    float cameraMinHeight = 5f;

    bool cameraIsEnabled;

    [SerializeField] AnimationCurve _cameraCurveMovement;
    [SerializeField] float _skyDomeHeight = 75f;
    float _outOfBoundsDepth = -50f;

    static private GameObject _skyDomeReference;
    static private GameObject _outOfBoundsReference;

    static public GameObject SkyDomeReference
    {
        get { return _skyDomeReference; }
    }

    static public GameObject OutOfBoundsReference
    {
        get { return _outOfBoundsReference; }
    }

    [SerializeField] float _cameraLerpSpeed;
    private void Awake()
    {
        instance = this;
        _chaseCamera = Camera.main;

        //Disable the camera controller by default
        cameraIsEnabled = false;
        _skyDomeReference = GameObject.FindGameObjectWithTag("SkyDome");
        _outOfBoundsReference = GameObject.FindGameObjectWithTag("OutOfBounds");

        //Subscribe to GameController events
        GameController.instance.OnGameControllerMessage += OnCameraConfigChange;
    }

    private void OnCameraConfigChange(object sender, GameController.GameControllerMessage e)
    {
        string ArgMessage = e.JSONMessage;

        //Parse out the message
        JObject JSONObjects = JObject.Parse(@ArgMessage);

        //Step through the array of each JSONObject:
        foreach (var JSONObject in JSONObjects)
        {
            //Update camera controller 
            if (JSONObject.Key == "camera-controller") UpdateCameraConfig(JSONObject.Value);
        }
    }

    private void LateUpdate()
    {
        if (cameraIsEnabled == false) return;

        //Follow player if it's active
        if (_playerControl.GetPlayerState == PlayerControl.PlayerState.PLAYING)
        {
            //Clip camera a certain height. when player is playing, if player falls below track
            if ((_playerReference.transform.position.y < cameraMinHeight) && (transform.position.y < cameraMinHeight))
            {
                //Simple lerp movement
                Vector3 desiredPosition = Vector3.Lerp(_playerReference.transform.position + _cameraOffset, gameObject.transform.position, 0.998f);
                transform.position = desiredPosition;
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(_playerReference.transform.position - transform.position), 0.25f);
            }
            else
            {
                //Simple lerp movement
                Vector3 desiredPosition = Vector3.Lerp(_playerReference.transform.position + _cameraOffset, gameObject.transform.position, _cameraLerpSpeed);
                transform.position = desiredPosition;

                //Roll camera with player ship
                //gameObject.transform.rotation = Quaternion.Euler(0f, 0f, (1 + Mathf.Sin((_playerRb.velocity.x + 270f) * Mathf.Deg2Rad)) * percentTilt);
                float playerMaxLatitude = Mathf.Abs(_playerControl.GetPlayerMaxLateralVelocity());
                float cameraRollAngle = transform.rotation.eulerAngles.z;

                float playerVelocity = _playerRb.velocity.x;
                if (Mathf.Abs(playerVelocity) < Mathf.Abs(velocityDeadZone))
                {
                    playerVelocity = 0f;
                }
                else playerVelocity = playerVelocity + -Mathf.Sign(playerVelocity) * velocityDeadZone;

                float cameraZRoll = _cameraCurveMovement.Evaluate(playerVelocity / playerMaxLatitude) * percentTilt * playerMaxLatitude * Mathf.Sign(playerVelocity);
                cameraZRoll = Mathf.LerpAngle(cameraRollAngle, cameraZRoll, 0.05f);

                transform.rotation = Quaternion.Euler(0f, 0f, cameraZRoll);
            }
        }

        //Camera behavior while at launch
        if (_playerControl.GetPlayerState == PlayerControl.PlayerState.LAUNCH)
        {
            transform.position = new Vector3(_playerReference.transform.position.x, _playerReference.transform.position.y + 10f, _playerReference.transform.position.z - 7.5f);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(_playerReference.transform.position - transform.position), 0.25f);
        }

        _skyDomeReference.transform.position = new Vector3(0f, _skyDomeHeight, gameObject.transform.position.z);
        _outOfBoundsReference.transform.position = new Vector3(0f, _outOfBoundsDepth, gameObject.transform.position.z);
    }

    void UpdateCameraConfig(JToken cameraConfig)
    {
        //Enable the camera and get the player reference and move the camera to the player
        if (cameraConfig["enabled"] != null)
        {
            //Convert to boolean
            bool cameraEnabledStatus = cameraConfig["enabled"].Value<bool>();

            //If enabled, get a reference to the player, move the camera to the player and turn it on
            if (cameraEnabledStatus)
            {
                _playerControl = PlayerControl.instance;
                _playerReference = _playerControl.gameObject;
                _playerRb = _playerReference.GetComponent<Rigidbody>();
                gameObject.transform.position = _playerRb.transform.position + _cameraOffset;
            }
            cameraIsEnabled = cameraEnabledStatus;

            //Send message to GameController that the manager is ready
            JObject readyMessage = new();
            readyMessage.Add("camera-controller", "ready");
            ControllerMessages.OnControllerMessage(this, new ControllerMessages.ControllerMessage { JSONMessage = readyMessage.ToString() });
        }

        if (cameraConfig["camera-offset"] != null)
        {
            _cameraOffset = Utility.JSONtoVector3(cameraConfig["camera-offset"].Value<JObject>());
        }
    }
}
