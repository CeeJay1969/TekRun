using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public static GameController instance;

    //For initialization controller counter
    int controllerInitializationCount;

    //Game State and global variables
    enum GamePhase
    {
        INIT_START, INIT_START_FADE_OUT, INIT_WAIT_FADE_OUT,
        TITLE_START, TITLE_WAIT_FOR_FADE_IN, TITLE_EVENT_OCCURRED, TITLE_WAIT_FOR_FADE_OUT,
        GAME_WAIT_FOR_LOAD_SCENE, GAME_INIT_LEVEL, GAME_CONFIGURE_WAIT_FOR_OBJECT_POOLER, GAME_INIT_LAUNCH, GAME_LAUNCH, GAME_LOOP, GAME_RESET, GAME_RELOCATE_PLAYER,
        OUTRO_TRIGGERED, OUTRO_ANIM, OUTRO_ANIM_END, OUTRO_WAIT_FOR_FADE_OUT,
        PAUSE, END,
        GAME_OVER, GAME_OVER_WAIT_FOR_FADE_OUT
    }

    GamePhase gamePhase;
    static int currentGameLevel = -1;
    static public int CurrentGameLevel
    {
        get { return currentGameLevel; }
    }

    float transitionDistance;
    JToken levelDataPointer;
    JToken levelDataJSON;
    int levelDataIndex;
    int playerStartLaunchPadID;

    JObject controllerMessage;

    //Event publisher for other managers to subscribe to
    public class GameControllerMessage : EventArgs
    {
        public string JSONMessage;
    }

    public event EventHandler<GameControllerMessage> OnGameControllerMessage;

    private void Awake()
    {
        //Make sure it's a singleton and make it persistant
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            //Receive messages from other controllers
            ControllerMessages.OnControllerMessage += ReceiveControllerMessages;

            //Get a count of all the controllers
            controllerInitializationCount = GameObject.FindGameObjectsWithTag("Controller").Count();
        }
        else
        {
            Debug.LogError("FATAL: GameController has tried to instantiate more than once ... destroying");
            Destroy(gameObject);
        }
    }

    //Method called to process messages from other controllers
    void Start()
    {
        gamePhase = GamePhase.INIT_START;
    }

    private void Update()
    {
        //Game states
        switch (gamePhase)
        {

            //Wait for controllers to finish initializing
            case GamePhase.INIT_START:

                if (controllerInitializationCount == 0)
                {
                    gamePhase = GamePhase.INIT_START_FADE_OUT;

                    //Start fade out
                    controllerMessage = new();
                    controllerMessage.Add("scene-fader", "fade-out");
                    OnGameControllerMessage(this, new GameControllerMessage { JSONMessage = controllerMessage.ToString() });

                    gamePhase = GamePhase.INIT_WAIT_FADE_OUT;
                }
                break;


            case GamePhase.GAME_INIT_LEVEL:

                //Collect object pooler paths and send the paths to the pooler
                JObject gameInitializeValues = SOCache.instance.SOCacheDictionary["Initialize_" + currentGameLevel.ToString()].Value<JObject>();
                if (gameInitializeValues["object-pooler"] != null)
                {
                    gamePhase = GamePhase.GAME_CONFIGURE_WAIT_FOR_OBJECT_POOLER;

                    controllerMessage = new();
                    controllerMessage.Add("object-pooler", gameInitializeValues["object-pooler"]);
                    OnGameControllerMessage(this, new GameControllerMessage { JSONMessage = controllerMessage.ToString() });

                    //All spawn configuration data for this level and make the initial call to get configuration data
                    levelDataJSON = null;
                    levelDataPointer = null;
                    levelDataJSON = SOCache.instance.SOCacheDictionary["Configuration_" + currentGameLevel.ToString()].Value<JObject>()["game-events"];
                }
                else
                {
                    Debug.LogError("FATAL: Could not load pool content for level: " + currentGameLevel);
                }

                //Initialize spawner
                if (gameInitializeValues["spawn-content"] != null)
                {
                    controllerMessage = new();
                    controllerMessage.Add("spawn-content", gameInitializeValues["spawn-content"]);
                    controllerMessage.Add("spawn-weather", gameInitializeValues["spawn-weather"]);
                    OnGameControllerMessage(this, new GameControllerMessage { JSONMessage = controllerMessage.ToString() });
                }

                //Enable the UI controller
                controllerMessage = new()
                {
                    {"enabled" , true }
                };
                controllerMessage.Add("ui-controller", controllerMessage);
                OnGameControllerMessage(this, new GameControllerMessage { JSONMessage = controllerMessage.ToString() });

                //Set the spawn controller build distance
                SpawnController.instance.BuildPoint = new Vector3(0f, 0f, 0f);
                ReadNextLevelConfiguration();
                break;

            //Main Game Loop here
            case GamePhase.GAME_LOOP:

                if ((SpawnController.instance.BuildPoint.z > transitionDistance) && (levelDataIndex < levelDataJSON.Count()))
                {
                    ReadNextLevelConfiguration();
                }
                break;
        }
    }

    //Receive and process other controller responses
    private void ReceiveControllerMessages(object sender, ControllerMessages.ControllerMessage e)
    {
        //Parse out the message
        string ArgMessage = e.JSONMessage;
        JObject JSONObjects = JObject.Parse(@ArgMessage);

        //Iterate through each controller message
        foreach (var JSONObject in JSONObjects)
        {

            //Pass messages on to other controllers
            if (JSONObject.Value.ToString() == "PASS-THROUGH")
            {
                Debug.Log("Pass through");

            }

            //Initialization of controllers
            if (gamePhase == GamePhase.INIT_START)
            {
                //Countdown of each controller initialization
                if (JSONObject.Value.ToString() == "ready")
                    controllerInitializationCount--;

                return;
            }

            //Waiting for initialization fade out
            if ((gamePhase == GamePhase.INIT_WAIT_FADE_OUT) && (JSONObject.Key == "scene-fader") && (JSONObject.Value.ToString() == "fade-out-ended"))
            {
                gamePhase = GamePhase.TITLE_START;

                //Load the title scene
                controllerMessage = new();
                controllerMessage.Add("scene-fader", "TITLE");
                OnGameControllerMessage(this, new GameControllerMessage { JSONMessage = controllerMessage.ToString() });

                return;
            }

            //Title screen mode and scene has switched to the title
            if ((gamePhase == GamePhase.TITLE_START) && (SceneManager.GetActiveScene().name == "TITLE"))
            {
                gamePhase = GamePhase.TITLE_WAIT_FOR_FADE_IN;

                //Load default preferences
                JObject gamePreferencesObject = SOCache.instance.SOCacheDictionary["Default_Preferences"].Value<JObject>();

                //If the game level is uninitialized (-1), load all preferences
                if (currentGameLevel == -1)
                {
                    //Get the default game level
                    foreach (var gamePreferenceObject in gamePreferencesObject)
                    {
                        //Player preferences, iterate through properties
                        if (gamePreferenceObject.Key == "player")
                        {
                            JToken playerPreferences = gamePreferenceObject.Value;

                            if (playerPreferences["starting-level"] != null)
                            {
                                currentGameLevel = (int)playerPreferences["starting-level"];
                            }
                        }
                    }
                }

                //Start the title animations ...

                //Start title fade in
                controllerMessage = new();
                controllerMessage.Add("scene-fader", "fade-in");
                OnGameControllerMessage(this, new GameControllerMessage { JSONMessage = controllerMessage.ToString() });

                return;
            }

            //Wait for title fade in
            if ((gamePhase == GamePhase.TITLE_WAIT_FOR_FADE_IN) && (JSONObject.Key == "scene-fader") && (JSONObject.Value.ToString() == "fade-in-ended"))
            {
                //Events handled in the update method
                gamePhase = GamePhase.TITLE_EVENT_OCCURRED;
                return;
            }

            //A button was pressed on the title screen
            if ((gamePhase == GamePhase.TITLE_EVENT_OCCURRED) && (JSONObject.Key == "title-screen") && (JSONObject.Value.ToString() == "game-init"))
            {
                gamePhase = GamePhase.TITLE_WAIT_FOR_FADE_OUT;

                //Start game button was pressed .. start fade out of title screen
                controllerMessage = new();
                controllerMessage.Add("scene-fader", "fade-out");
                OnGameControllerMessage(this, new GameControllerMessage { JSONMessage = controllerMessage.ToString() });

                return;
            }

            //Wait for title screen to fade out
            if ((gamePhase == GamePhase.TITLE_WAIT_FOR_FADE_OUT) && (JSONObject.Key == "scene-fader") && (JSONObject.Value.ToString() == "fade-out-ended"))
            {
                gamePhase = GamePhase.GAME_WAIT_FOR_LOAD_SCENE;

                //Load the game scene
                controllerMessage = new();
                controllerMessage.Add("scene-fader", "GAME");
                OnGameControllerMessage(this, new GameControllerMessage { JSONMessage = controllerMessage.ToString() });

                return;
            }

            //Game scene is loaded ... configure the object pooler ... done in the update loop
            if ((gamePhase == GamePhase.GAME_WAIT_FOR_LOAD_SCENE) && (JSONObject.Key == "scene-fader") && (JSONObject.Value.ToString() == "scene-load-completed"))
            {
                gamePhase = GamePhase.GAME_INIT_LEVEL;
                return;
            }

            //Set and initalize game controllers for this level
            if ((gamePhase == GamePhase.GAME_CONFIGURE_WAIT_FOR_OBJECT_POOLER) && (JSONObject.Key == "object-pooler") && (JSONObject.Value.ToString() == "ready"))
            {

                //Wait for Spawner to create the launcher
                gamePhase = GamePhase.GAME_INIT_LAUNCH;

                //Create PlayerController, enable it and place it at the origin
                GameObject newPlayer;
                ObjectPooler.ObjectPoolDictionary["Player"].Get(out newPlayer);
                newPlayer.transform.position = Vector3.zero;

                //Put camera a little closer to player
                JObject cameraOffset = Utility.Vector3toJSON(new Vector3(0f, 4f, -6f));
                controllerMessage = new();
                JObject controllerEnableMessage = new()
                {
                    {"enabled", "true"},
                    {"camera-offset", cameraOffset }
                };

                //Message to enable the camera controller
                controllerMessage.Add("camera-controller", controllerEnableMessage);

                //Message to enable te spawn controller and place it at the origin and set the build range
                controllerMessage.Add("spawn-config", controllerEnableMessage);
                SpawnController.instance.transform.position = Vector3.zero;
                levelDataIndex = 0;

                //Send messages to controllers
                OnGameControllerMessage(this, new GameControllerMessage { JSONMessage = controllerMessage.ToString() });

                //Message fade in the scene
                controllerMessage = new()
                {
                    { "scene-fader", "fade-in" }
                };
                OnGameControllerMessage(this, new GameControllerMessage { JSONMessage = controllerMessage.ToString() });

                return;
            }

            //Player launcher ready, position player at bottom of the launcher
            if ((gamePhase == GamePhase.GAME_INIT_LAUNCH) && (JSONObject.Key == "launch-pad-control"))
            {
                gamePhase = GamePhase.GAME_LAUNCH;

                //Send the coordinates the player ship
                if (JSONObject.Value["pad-position"] != null)
                {
                    controllerMessage = new();
                    JObject playerPadPosition = new();
                    playerPadPosition.Add("position", JSONObject.Value["pad-position"]);

                    controllerMessage.Add("player-controller", playerPadPosition);
                    OnGameControllerMessage(this, new GameControllerMessage { JSONMessage = controllerMessage.ToString() });
                }

                //Get the gameObjectID of the launch pad
                if (JSONObject.Value["id"] != null)
                {
                    playerStartLaunchPadID = JSONObject.Value["id"].Value<int>();
                }
            }

            //Wait for player to finish launch
            if ((gamePhase == GamePhase.GAME_LAUNCH) && (JSONObject.Key == "player-controller") && (JSONObject.Value.ToString() == "launched"))
            {
                gamePhase = GamePhase.GAME_LOOP;

                //Put camera back a bit
                JObject cameraOffset = Utility.Vector3toJSON(new Vector3(0f, 10f, -15f));
                controllerMessage = new();
                JObject controllerEnableMessage = new()
                {
                    {"camera-offset", cameraOffset }
                };
                OnGameControllerMessage(this, new GameControllerMessage { JSONMessage = controllerMessage.ToString() });
            }

            //Events to check for during game play
            if (gamePhase == GamePhase.GAME_LOOP)
            {
                //Player has been destroyed, find a new position on the track to put player and reset hull to 100
                if ((JSONObject.Key == "player-controller") && (JSONObject.Value.ToString() == "destroyed"))
                {
                    //Get the new position for the player
                    Vector3 playerResetPosition = PlayerControl.playerPosition;

                    playerResetPosition = new Vector3(0f, 10f, playerResetPosition.z - 20f);

                    //Reset hull, player position and run state
                    JObject playerControllerMessages = new()
                    {
                        { "hull-value", 100 },
                        { "position", Utility.Vector3toJSON(playerResetPosition) },
                        { "state", PlayerControl.PlayerState.PLAYING.ToString() },
                        { "enabled", "true"}
                    };

                    JObject playerControllerMessage = new()
                    { 
                        {"player-controller", playerControllerMessages}
                    };
                    OnGameControllerMessage(this, new GameControllerMessage { JSONMessage = playerControllerMessage.ToString() });
                }
            }
        }
    }

    private float ReadNextLevelConfiguration()
    {
        //Send point to the next spawner configuration and trigger the event
        levelDataPointer = levelDataJSON.ElementAt(levelDataIndex);
        levelDataIndex++;

        //Mark data as spawn content
        JObject spawnData = new();
        spawnData.Add("spawn-content", levelDataPointer);

        //Trigger configuration change event to spawner
        OnGameControllerMessage(this, new GameControllerMessage
        { JSONMessage = spawnData.ToString() });

        //Move to the next transition
        transitionDistance += (float)levelDataPointer["transitionDistance"];

        return transitionDistance;
    }
}
