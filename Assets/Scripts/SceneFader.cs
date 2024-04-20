using System;
using UnityEngine;
using Newtonsoft.Json.Linq;
using UnityEngine.SceneManagement;
using System.Collections;

/*
 * Changing the scene via the SceneState function will AUTOMATICALLY fade out, load the scene and fade into the loaded scene
 * Events are triggered on the "scene-fader" key
 * Keys with the value of fade-in or fade-out will use fade the scene in and out manually
 * Other key value will attempt to match it with available scenes and switch to them immediately via the SceneManagement
 * 
*/
public class SceneFader : MonoBehaviour
{
    public static SceneFader instance;
    
    //Game State
    public enum SceneState { INITIALIZE, TITLE, GAME, RUNNING, END }
    SceneState gameScene = SceneState.INITIALIZE; 
    SceneState _newSceneState = SceneState.INITIALIZE;

    public SceneState NewSceneState
    {
        set { _newSceneState = value; }
        get { return gameScene; }
    }

    //Return fade status
    public bool FadeComplete => _fadeComplete;

    //Canvas group reference
    CanvasGroup _sceneCanvasGroupReference;

    //Scene fade alpha value
    float _fadeAlphaValue;
    //Scent fade alpha speed
    float _fadeAlphaSpeed = 0.1f;
    //Coroutine update speed
    float _fadeCoroutineSpeed = 0.05f;

    //Scene fade variables to indicate if fade is in progress
    bool _fadingIn = false;
    bool _fadingOut = false;
    bool _fadeComplete = true;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;

            //Get reference to the Canvas
            _sceneCanvasGroupReference = GetComponentInChildren<CanvasGroup>();
        }
        else
            Destroy(gameObject);

        //Start Coroutine that will fade the screen via Canvas group alpha value
        StartCoroutine (updateSceneFade());

        //Subscribe to the GameController message sending event
        GameController.instance.OnGameControllerMessage += OnScreenFadeChange;

        //Send message to GameController to indicate that the SceneFader is ready
        JObject readyMessage = new();
        readyMessage.Add("scene-fader", "ready");
        ControllerMessages.OnControllerMessage(this, new ControllerMessages.ControllerMessage { JSONMessage = readyMessage.ToString() });
    }

    private void OnScreenFadeChange(object sender, GameController.GameControllerMessage e)
    {
        string ArgMessage = e.JSONMessage;

        //Parse out the message
        JObject JSONObjects = JObject.Parse(@ArgMessage);

        //Step through the array of each JSONObject:
        foreach (var JSONObject in JSONObjects)
        {
            //Look for any messages for the scene-fader
            if (JSONObject.Key == "scene-fader")
                AnimateScreenFade(JSONObject.Value);
        }
    }

    private void AnimateScreenFade(JToken newGameState)
    {
        //Look for fade in request and trigger if the fade in is not already happening
        if (newGameState.ToString() == "fade-in")
        {
            //Start the fade in
            _fadingIn = true;
        }
        else if (newGameState.ToString() == "fade-out")
        {
            //Start the fade out
            _fadingOut = true;
        }
        else if (newGameState.ToString() == "fade-next")
        {
            //Auto fade to the next screen
            _newSceneState = gameScene + 1;
        }
        else if (newGameState.ToString() == "fade-previous")
        {
            //Auto fade to the previous screen
            _newSceneState = gameScene - 1;
        }
        else
        {
            //Assume that it must be a scene name and evaluate it, load the scene immediately
            Enum.TryParse(newGameState.ToString(), true, out _newSceneState);
            gameScene = _newSceneState;
            SceneManager.LoadScene(gameScene.ToString());
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //Send message to GameController to indicate that the Scene has been loaded
        JObject readyMessage = new();
        readyMessage.Add("scene-fader", "scene-load-completed");
        ControllerMessages.OnControllerMessage(this, new ControllerMessages.ControllerMessage { JSONMessage = readyMessage.ToString() });
    }

    private void Update()
    {
        //If the current scene and the target scene are not the same and the fade out is completed, then load the new scene and start the fade in
        if ((gameScene != _newSceneState) && (_fadeAlphaValue == 0f))
        {
            //Change gameState flag
            gameScene = _newSceneState;

            //Switch to the new scene
            SceneManager.LoadScene(gameScene.ToString());

            //Fade in the new scene
            _fadingIn = true;
        }

        //If the current scene and target scene are not the same and the fade out has not yet stat
        if ((gameScene != _newSceneState) && (_fadingOut == false))
        {
            _fadingOut = true;
        }
    }

    IEnumerator updateSceneFade()
    {
        while (true)
        {

            if (_fadingOut || _fadingIn)
            {
                _fadeComplete = false;
                _fadeAlphaValue = _sceneCanvasGroupReference.alpha;

                //Reduce alpha of the canvas group, fade in
                if (_fadingIn)
                {
                    _fadeAlphaValue -= _fadeAlphaSpeed;
                    _fadeAlphaValue = Mathf.Clamp(_fadeAlphaValue, 0f, 1f);

                    if (_fadeAlphaValue == 0f)
                    {
                        _fadingIn = false;
                        _fadeComplete = true;

                        //Send message to GameController to indicate that the Scene has been loaded
                        JObject readyMessage = new();
                        readyMessage.Add("scene-fader", "fade-in-ended");
                        ControllerMessages.OnControllerMessage(this, new ControllerMessages.ControllerMessage { JSONMessage = readyMessage.ToString() });
                    }
                }

                //Increase alpha of the canvas group, fade out
                if (_fadingOut)
                {
                    _fadeAlphaValue += _fadeAlphaSpeed;
                    _fadeAlphaValue = Mathf.Clamp(_fadeAlphaValue, 0f, 1f);

                    if (_fadeAlphaValue == 1f)
                    {
                        _fadingOut = false;
                        _fadeComplete = true;

                        //Send message to GameController to indicate that the Scene has been loaded
                        JObject readyMessage = new();
                        readyMessage.Add("scene-fader", "fade-out-ended");
                        ControllerMessages.OnControllerMessage(this, new ControllerMessages.ControllerMessage { JSONMessage = readyMessage.ToString() });
                    }
                }

                //Update the alpha in the Canvas group
                _sceneCanvasGroupReference.alpha = _fadeAlphaValue;
            }

            yield return new WaitForSeconds(_fadeCoroutineSpeed);
        }
    }
}