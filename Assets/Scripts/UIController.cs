using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class UIController : MonoBehaviour
{
    [SerializeField] Canvas GameUI;
    bool UIControllerEnabled = false;
    Dictionary<string, TextMeshProUGUI> UITextFields;

    //Coroutine alpha fader
    TextAlphaFader textAlphaFader;

    private void Awake()
    {
        //Subscribe to GameLogic event
        GameController.instance.OnGameControllerMessage += OnUIConfigChange;

        //Recieve messages to update the UI
        ControllerMessages.OnUIControllerMessage += OnReceiveUIUpdates;

        //Find all text controls and add them to the dictionary
        UITextFields = new Dictionary<string, TextMeshProUGUI>();
        TextMeshProUGUI[] FindTextFields = GameUI.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (TextMeshProUGUI FoundTextField in FindTextFields)
            UITextFields.Add(FoundTextField.name, FoundTextField);

        //References for text animation effects
        textAlphaFader = GetComponent<TextAlphaFader>();
    }

    //Messages from the game controller disable or enable
    void OnUIConfigChange(object sender, GameController.GameControllerMessage e)
    {
        string ArgMessage = e.JSONMessage;

        //Parse out the message
        JObject JSONObjects = JObject.Parse(@ArgMessage);

        //Step through the array of each JSONObject:
        foreach (var JSONObject in JSONObjects)
        {
            //Update UI controller 
            if (JSONObject.Key == "ui-controller") UpdateUIConfig(JSONObject.Value);
        }
    }

    //Called by the game controller to enable and disable the UI controller
    void UpdateUIConfig(JToken UIConfig)
    {
        if (UIConfig["enabled"] != null)
        {
            UIControllerEnabled = bool.Parse(UIConfig["enabled"].Value<string>());
            
            //Enable/disable all fields
            for (int textFieldIndex = 0; textFieldIndex < UITextFields.Count(); textFieldIndex++)
            {
                UITextFields.ElementAt(textFieldIndex).Value.enabled = bool.Parse(UIConfig["enabled"].Value<string>());
            }
        }
    }

    void UpdateUIFields(JToken UIPropertyChanges)
    {
        //Iterate through each property, if a matching field is found change the properties
        foreach (JProperty uiPropertyChange in UIPropertyChanges)
        {
            //Check for match between the property and fields in the UI
            if (UITextFields.ContainsKey(uiPropertyChange.Name))
            {
                //Get the properties
                JToken uiProperties = uiPropertyChange.Value;
                //Modify text if properties are present
                if (uiProperties["text"] != null)
                    UITextFields[uiPropertyChange.Name].text = uiProperties["text"].ToString();

                //Get the current font styles
                //TMPro.FontStyles fontStyle = UITextFields[uiPropertyChange.Name].fontStyle;

                //Change the font color
                if (uiProperties["color"] != null)
                {
                    ColorUtility.TryParseHtmlString(uiProperties["color"].ToString(), out Color newFontColor);
                    UITextFields[uiPropertyChange.Name].color = newFontColor;
                }

                //Enable disable auto sizing
                if (uiProperties["auto-size"] != null)
                    UITextFields[uiPropertyChange.Name].enableAutoSizing = (bool)uiProperties["auto-size"];
                
                //Configure fade in effects
                if (uiProperties["fade"] != null)
                {
                    //Set the default values
                    int alphaRampUpSpeed = 10;
                    int alphaRampDownSpeed = 10;
                    float initialDelay = 2f;
                    float sustainTime = 0f;
                    float animationWaitTime = 0.01f;

                    if (uiProperties["fade"].Value<JToken>("initial-delay") != null)
                        initialDelay = (float)uiProperties["fade"].Value<JToken>("initial-delay");
                    if (uiProperties["fade"].Value<JToken>("sustain-time") != null)
                        sustainTime = (float)uiProperties["fade"].Value<JToken>("sustain-time");

                    //Initiate the fade animation
                    textAlphaFader.FadeAlpha(initialDelay, alphaRampUpSpeed, sustainTime, alphaRampDownSpeed, animationWaitTime, UITextFields[uiPropertyChange.Name]);
                }
            }
        }
    }

    //Update text in fields
    private void OnReceiveUIUpdates(object sender, ControllerMessages.UIControllerMessage e)
    {
        //Ignore updates if not enabled
        if (UIControllerEnabled == false) return;

        string ArgMessage = e.JSONMessage;

        //Parse out the message
        JObject JSONObjects = JObject.Parse(@ArgMessage);

        //Step through the array of each JSONObject:
        foreach (var JSONObject in JSONObjects)
        {
            //Update camera controller 
            if (JSONObject.Key == "ui-update-field") UpdateUIFields(JSONObject.Value);
        }
    }
}
