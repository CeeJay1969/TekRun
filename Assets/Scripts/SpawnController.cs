using UnityEngine;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class SpawnController : MonoBehaviour
{
    public static SpawnController instance;

    Enums.TrackPlacement trackRail;
    Enums.TrackPlacement trackLight;
    Enums.TrackBuildMode trackBuildMode;
    Enums.BoolValue spawnEnable;
    Enums.BoolValue fogLayerEnable = Enums.BoolValue.FALSE;

    //TODO add to scriptable objects ...
    //Distance at which to draw the background
    Vector3 _buildDistance;
    float bridgeSupportIncrement = 250f;
    float trackRailIncrement = 25f;
    float trackIncrement = 50f;
    float trackLightIncrement = 125f;
    float fogLayerIncrement = 500f;

    //Obstacles
    float _obstacleUnitWidth = 10f;
    //Number of track segments
    int _trackUnitWidth = 5;

    //The build point
    Vector3 _buildPoint;
    float _buildPointIncrement = 1f;

    float trackDistanceTarget;
    float bridgeSupportDistanceTarget;
    float trackRailDistanceTarget;
    float trackLightLeftDistanceTarget, trackLightRightDistanceTarget;
    float fogLayerDistanceTarget;

    Vector3 _buildZoneRange = new Vector2(5, 20);
    Vector3 _buildZoneBuffer = new Vector2(5, 20);

    int fogLayerDensity = 5;
    float fogLayerAlpha = 0.5f;
    Color fogLayerColor = Color.gray;

    //Returns and sets the distance at where the game data will be generated with respects to the player position
    public Vector3 BuildPoint
    {
        get { return _buildPoint; }
        set { _buildPoint = value; }
    }

    //Track segment
    Vector3 _trackDimension = new Vector3(50f, 0f, 50f);

    //Spacing between obstacle zones
    float _buildBufferZone;

    //Player reference
    GameObject _playerReference;
    Renderer _skyDomeEffect;
    [SerializeField] GameObject _skyDomeReference;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);

        //Disable the spawn manager
        spawnEnable = Enums.BoolValue.FALSE;

        //Subscribe to GameLogic event
        GameController.instance.OnGameControllerMessage += OnSpawnChange;

        //Subscribe to the Scenemanager event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Start is called before the first frame update
    void Start()
    {
        trackLightLeftDistanceTarget = trackLightIncrement / 2f;
        fogLayerDistanceTarget = fogLayerIncrement;

        SpawnChangeCompleted();
    }

    //Triggered from game controller to change spawn controller behavior
    private void OnSpawnChange(object sender, GameController.GameControllerMessage e)
    {
        string ArgMessage = e.JSONMessage;

        //Parse out the message
        JObject JSONObjects = JObject.Parse(@ArgMessage);

        //Step through the array of each JSONObject:
        foreach (KeyValuePair<string, JToken> JSONObject in JSONObjects)
        {
            //Update spawn manager content
            if (JSONObject.Key == "spawn-content") UpdateContent(JSONObject.Value);

            //Update weather conditions
            if (JSONObject.Key == "spawn-weather") UpdateWeather(JSONObject);
        }
    }

    private void SpawnChangeCompleted()
    {
        //Send message to GameController that the manager is ready
        JObject readyMessage = new();
        readyMessage.Add("spawn-controller", "ready");
        ControllerMessages.OnControllerMessage(this, new ControllerMessages.ControllerMessage { JSONMessage = readyMessage.ToString() });
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //Only spawn if flag is not null and true
        if (spawnEnable == Enums.BoolValue.FALSE) return;

        //If begining the level build the launch pad
        if (_buildPoint.z == 0)
        {
            ObjectPooler.ObjectPoolDictionary["LaunchPad"].Get(out GameObject startLaunchPad);
            startLaunchPad.transform.position = Vector3.zero;
            _buildPoint.z += 40f;
        }

        //Build track and all element if track mode is active
        if (trackBuildMode != Enums.TrackBuildMode.NULL)
        {
            //Track mode enabled draw it
            DrawPlayField();
        }

        //Pause building if in a buffer building zone
        if (_buildPoint.z >= _buildBufferZone)
        {
            //Build control variables
            int zArrayBuild = 0;

            //Build obstacles based on mode settings
            switch (trackBuildMode)
            {
                case Enums.TrackBuildMode.TANK:
                    break;

                //Build barrier walls
                case Enums.TrackBuildMode.OBSTACLE:
                    {
                        //Track x coordinate on the left edge
                        float trackLeftBuildValue = (-_trackDimension.x / 2f) + (_obstacleUnitWidth / 2f);

                        //Determine the positions that will be occupied
                        int numberOfBarriers = UnityEngine.Random.Range(2, _trackUnitWidth - 1);
                        int[] occupiedSlots;
                        int[] barrierArray = new int[_trackUnitWidth];
                        Utility.RandomNonRepeating(numberOfBarriers, new Vector2Int(0, _trackUnitWidth - 1), out occupiedSlots);

                        //Populate the barrier array with the occupied slots
                        foreach (int slotNumber in occupiedSlots) barrierArray[slotNumber] = 1;

                        //Build the barrier wall
                        for (int barrierIndex = 0; barrierIndex < barrierArray.Length; barrierIndex++)
                        {
                            //If there is a barrier obstacle present
                            if (barrierArray[barrierIndex] != 0)
                            {
                                bool noBarrierToTheLeft = LeftOfBarrierNotOccupied(barrierIndex, barrierArray);
                                bool noBarrierToTheRight = RightOfBarrierNotOccupied(barrierIndex, barrierArray);

                                //If both sides of the barrier are occupied
                                if ((noBarrierToTheLeft == false) && (noBarrierToTheRight == false))
                                {
                                    //Draw center barrier
                                    GameObject newBarrier;
                                    ObjectPooler.ObjectPoolDictionary["Barrier_Center"].Get(out newBarrier);
                                    newBarrier.transform.position = new Vector3(trackLeftBuildValue, 10f, _buildPoint.z);
                                }
                                //If there is nothing to the left or the right OR some thing on the left and right
                                if ((noBarrierToTheLeft == true) && (noBarrierToTheRight == true))
                                {
                                    //Draw a single length barrier
                                    GameObject newBarrier;
                                    ObjectPooler.ObjectPoolDictionary["Barrier_Single"].Get(out newBarrier);
                                    newBarrier.transform.position = new Vector3(trackLeftBuildValue, 10f, _buildPoint.z);
                                }
                                //If only one side of the barrier is occupied
                                if (noBarrierToTheLeft != noBarrierToTheRight)
                                {
                                    //Draw left cornered barrier
                                    if (noBarrierToTheLeft)
                                    {
                                        GameObject newLeftBarrier;
                                        ObjectPooler.ObjectPoolDictionary["Barrier_Right"].Get(out newLeftBarrier);
                                        newLeftBarrier.transform.position = new Vector3(trackLeftBuildValue, 10f, _buildPoint.z);
                                    }
                                    //Draw right cornered barrier
                                    if (noBarrierToTheRight)
                                    {
                                        GameObject newRightBarrier;
                                        ObjectPooler.ObjectPoolDictionary["Barrier_Left"].Get(out newRightBarrier);
                                        newRightBarrier.transform.position = new Vector3(trackLeftBuildValue, 10f, _buildPoint.z);
                                    }
                                }
                            }

                            //Move to the next position
                            trackLeftBuildValue += _obstacleUnitWidth;
                        }
                    }
                    break;
            }

            //Calculate a new build buffer zone
            _buildBufferZone = _buildPoint.z + (zArrayBuild * _obstacleUnitWidth) + UnityEngine.Random.Range(_buildZoneBuffer.x * _obstacleUnitWidth, _buildZoneBuffer.y * _obstacleUnitWidth);
        }
    }

    private void DrawPlayField()
    {
        //Iteratively draw the tracks out in front of the player
        while (_buildPoint.z < (_playerReference.transform.position.z + _buildDistance.z))
        {
            if (trackBuildMode != Enums.TrackBuildMode.END_MISSION)
            {
                DrawTracks();
                DrawBridgeSupports();
                DrawTrackRails();
                DrawTrackLight();
                DrawFogLayer();
            }

            if (trackBuildMode == Enums.TrackBuildMode.END_MISSION)
            {
                GameObject endMission;
                ObjectPooler.ObjectPoolDictionary["Exit"].Get(out endMission);
                endMission.transform.position = new Vector3(0f, 0f, _buildPoint.z);

                //Turn off the track building
                trackBuildMode = Enums.TrackBuildMode.NULL;
            }

            //Move the track build point
            _buildPoint = new Vector3(0f, 0f, _buildPoint.z + _buildPointIncrement);
        }
    }
    private void DrawTracks()
    {
        if (_buildPoint.z >= trackDistanceTarget)
        {
            GameObject newTrack;
            ObjectPooler.ObjectPoolDictionary["Track"].Get(out newTrack);
            newTrack.transform.position = new Vector3(0f, 0f, _buildPoint.z);
            newTrack.transform.rotation = Quaternion.identity;

            //Update the new distance for the next track piece
            trackDistanceTarget += trackIncrement;
        }
    }
    private void DrawBridgeSupports()
    {
        if (_buildPoint.z >= bridgeSupportDistanceTarget)
        {
            //Draw the supports
            GameObject newBridgeSupportR;
            ObjectPooler.ObjectPoolDictionary["BridgeSupportR"].Get(out newBridgeSupportR);
            newBridgeSupportR.transform.position = new Vector3(30f, 0f, _buildPoint.z);

            GameObject newBridgeSupportL;
            ObjectPooler.ObjectPoolDictionary["BridgeSupportL"].Get(out newBridgeSupportL);
            newBridgeSupportL.transform.position = new Vector3(-30f, 0f, _buildPoint.z);

            //Update the new distance for the next support
            bridgeSupportDistanceTarget += bridgeSupportIncrement;
        }
    }
    private void DrawTrackRails()
    {
        while (_buildPoint.z > trackRailDistanceTarget)
        {
            if (trackRail != Enums.TrackPlacement.NULL)
            {
                if ((trackRail == Enums.TrackPlacement.BOTH) || (trackRail == Enums.TrackPlacement.LEFT))
                {
                    GameObject newTrackRailLeft;
                    ObjectPooler.ObjectPoolDictionary["TrackRail_L"].Get(out newTrackRailLeft);
                    newTrackRailLeft.transform.position = new Vector3(-_trackUnitWidth * _obstacleUnitWidth / 2f, 0f, _buildPoint.z);
                }

                if ((trackRail == Enums.TrackPlacement.BOTH) || (trackRail == Enums.TrackPlacement.RIGHT))
                {
                    GameObject newTrackRailRight;
                    ObjectPooler.ObjectPoolDictionary["TrackRail_R"].Get(out newTrackRailRight);
                    newTrackRailRight.transform.position = new Vector3(_trackUnitWidth * _obstacleUnitWidth / 2f, 0f, _buildPoint.z);
                }
            }

            //Update new distance for next rails
            trackRailDistanceTarget += trackRailIncrement;
        }
    }
    private void DrawTrackLight()
    {
        while (_buildPoint.z > trackLightLeftDistanceTarget)
        {
            //Generate lights and railing if enabled
            if ((trackLight == Enums.TrackPlacement.BOTH) || (trackLight == Enums.TrackPlacement.LEFT))
            {
                ObjectPooler.ObjectPoolDictionary["TrackLight_R"].Get(out GameObject newTrackLightLeft);
                newTrackLightLeft.transform.position = new Vector3(-_trackUnitWidth * _obstacleUnitWidth / 2f, 0f, _buildPoint.z);
            }
            //Update distance for the next light
            trackLightLeftDistanceTarget += trackLightIncrement;
        }
        while (_buildPoint.z > trackLightRightDistanceTarget)
        {
            //Generate lights and railing if enabled
            if ((trackLight == Enums.TrackPlacement.BOTH) || (trackLight == Enums.TrackPlacement.RIGHT))
            {
                ObjectPooler.ObjectPoolDictionary["TrackLight_L"].Get(out GameObject newTrackLightRight);
                newTrackLightRight.transform.position = new Vector3(_trackUnitWidth * _obstacleUnitWidth / 2f, 0f, _buildPoint.z);
            }
            //Update distance for the next light
            trackLightRightDistanceTarget += trackLightIncrement;
        }

    }
    private void DrawFogLayer()
    {
        //Do nothing if not enabled
        if (fogLayerEnable == Enums.BoolValue.FALSE) return;

        if (_buildPoint.z >= fogLayerDistanceTarget)
        {
            ObjectPooler.ObjectPoolDictionary["FogLayer"].Get(out GameObject newFogLayer);

            //Set color
            ParticleSystem ps = newFogLayer.GetComponentInChildren<ParticleSystem>();
            var psMain = ps.main;
            psMain.startColor = fogLayerColor;

            //Set gradient color
            var psColorGradient = ps.colorOverLifetime;
            psColorGradient.enabled = true;
            Gradient psGradient = new Gradient();
            psGradient.mode = GradientMode.Blend;
            psGradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.gray, 0.0f), new GradientColorKey(Color.gray, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(1.0f, 0.5f), new GradientAlphaKey(0.0f, 1.0f) });
            psColorGradient.color = psGradient;

            //Place fog layer and start animation
            newFogLayer.transform.position = new Vector3(0f, -20f, fogLayerDistanceTarget);
            newFogLayer.GetComponentInChildren<ParticleSystem>().Play();

            //Update the new distance for the next fog layer
            fogLayerDistanceTarget += fogLayerIncrement;
        }
    }

    bool LeftOfBarrierNotOccupied(int slotPosition, int[] barrierArray)
    {
        bool notOccupied = false;
        slotPosition--;

        if (slotPosition < 0)
        {
            notOccupied = true;
            return notOccupied;
        }
        if (barrierArray[slotPosition] == 0) notOccupied = true;

        return notOccupied;
    }
    bool RightOfBarrierNotOccupied(int slotPosition, int[] barrierArray)
    {
        bool notOccupied = false;
        slotPosition++;

        if (slotPosition > _trackUnitWidth - 1)
        {
            notOccupied = true;
            return notOccupied;
        }
        if (barrierArray[slotPosition] == 0) notOccupied = true;

        return notOccupied;
    }

    private void UpdateContent(JToken spawnContentValues)
    {

        //Iterate through each property
        foreach (JToken spawnContentValue in spawnContentValues)
        {
            //Check the name and assign the values appropriately 
            string propertyName = spawnContentValue.Value<JProperty>().Name.ToString();
            string propertyValue = spawnContentValue.Value<JProperty>().Value.ToString();

            //Basic configuration
            if (propertyName == "enabled")
            {
                Enum.TryParse(propertyValue, true, out spawnEnable);

                //Get reference dependancies if being enabled
                if (spawnEnable == Enums.BoolValue.TRUE)
                {
                    _playerReference = PlayerControl.instance.gameObject;
                    _skyDomeEffect = CameraController.SkyDomeReference.GetComponent<Renderer>();
                }
            }
            if (propertyName == "buildDistance")
                _buildDistance = new Vector3(0f, 0f, float.Parse(propertyValue));

            //Set flags for obstacles and enemies
            if (propertyName == "trackLight")
                Enum.TryParse(propertyValue, true, out trackLight);
            if (propertyName == "trackRail")
                Enum.TryParse(propertyValue, true, out trackRail);
            if (propertyName == "trackBuildMode")
                Enum.TryParse(propertyValue, true, out trackBuildMode);

            //Set flags for environmental effects
            if (propertyName == "fog-enable") RenderSettings.fog = bool.Parse(propertyValue);
            if (propertyName == "fog-start") RenderSettings.fogStartDistance = float.Parse(propertyValue);
            if (propertyName == "fog-end")
            {
                //Set the fog distance
                RenderSettings.fogEndDistance = float.Parse(propertyValue);

                //Set camera clipping plane to the same as the distance
                CameraController.ChaseCamera.farClipPlane = float.Parse(propertyValue);
            }
            if (propertyName == "fog-color")
            {
                //Set the fog color
                ColorUtility.TryParseHtmlString(propertyValue, out Color fogColor);
                RenderSettings.fogColor = fogColor;

                //Calculate the shadow color 150% of the fog color
                Color.RGBToHSV(fogColor, out float h, out float s, out float v);
                //v *= 2f;

                Color shadowColor = Color.HSVToRGB(h, s, v, false);
                RenderSettings.ambientLight = shadowColor;

                //Should match fog color with camera solid color an skydome color
                CameraController.ChaseCamera.backgroundColor = fogColor;
            }
            //Fog layer attributes
            if (propertyName == "fog-layer-enable")
            {
                Enum.TryParse(propertyValue, true, out fogLayerEnable);
                fogLayerDistanceTarget = _buildPoint.z;
            }
            if (propertyName == "fog-layer-density")
                fogLayerDensity = int.Parse(propertyValue);
            if (propertyName == "fog-layer-alpha")
                fogLayerAlpha = float.Parse(propertyValue);
            if (propertyName == "fog-layer-color")
                ColorUtility.TryParseHtmlString(propertyValue, out fogLayerColor);
        }

        SpawnChangeCompleted();
    }

    private void UpdateWeather(KeyValuePair<string, JToken> spawnWeather)
    {
        foreach (JToken weatherConfiguration in spawnWeather.Value)
        {
            if (weatherConfiguration.Value<JProperty>().Name.ToString() == "cloud-color")
            {
                ColorUtility.TryParseHtmlString(weatherConfiguration.Value<JProperty>().Value.ToString(), out Color cloudColor);
                _skyDomeEffect.material.SetColor("_CloudColor", cloudColor);
            }

            if (spawnWeather.Key.ToString() == "cloud-scale")
                _skyDomeEffect.material.SetFloat("_CloudScale", (float)spawnWeather.Value);

            if (spawnWeather.Key.ToString() == "cloud-alpha")
                _skyDomeEffect.material.SetFloat("_CloudAlpha", (float)spawnWeather.Value);
        }
        SpawnChangeCompleted();
    }

    //Get references when the Game screen is loaded
    void OnSceneLoaded(Scene currentScene, LoadSceneMode sceneMode)
    {
        //Check to see if this is the game scene, if so get the SkyDome reference
        if (currentScene.name == "GAME")
        {
            _skyDomeEffect = GameObject.FindWithTag("SkyDome").GetComponent<Renderer>();
        }
    }

    private void OnDisable()
    {
        //Remove from the scene load event
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
