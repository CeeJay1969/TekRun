using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using UnityEngine;
using UnityEngine.UI;

public class TitleScreen : MonoBehaviour
{

    [SerializeField] Canvas menuCanvas;
    Button[] menuButtons;

    private void Awake()
    {
        SceneFader gameControllerReference = SceneFader.instance;

        //Find menu canvas and buttons
        menuButtons = menuCanvas.GetComponentsInChildren<Button>();

        //Disable all buttons
        //EnableAllButtons(false);

        //Add event listeners to buttons on the menu
        foreach (Button menuButton in menuButtons)
        {
            //Start button, change game state to load the game screen
            if (menuButton.name == "StartButton")
                menuButton.onClick.AddListener(() => TriggerInitializeGame());
            if (menuButton.name == "QuitButton")
                menuButton.onClick.AddListener(() => Application.Quit());
        }

    }

    void TriggerInitializeGame()
    {
        //Send message to GameController to initialize the game
        JObject readyMessage = new();
        readyMessage.Add("title-screen", "game-init");
        ControllerMessages.OnControllerMessage(this, new ControllerMessages.ControllerMessage { JSONMessage = readyMessage.ToString() });
    }

    public void EnableAllButtons(bool enableStatus)
    {
        foreach (Button menuButton in menuButtons)
        {
            //Disable each menu button
            menuButton.enabled = enableStatus;
        }
    }
}
