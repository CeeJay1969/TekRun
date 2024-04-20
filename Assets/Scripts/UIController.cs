using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

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

    void UpdateUIField(JToken UIPropertyChanges)
    {
        //Check for each property
        if (UIPropertyChanges["text"] != null)
        {
            //Step through each field:
            foreach (JProperty UITextProperty in UIPropertyChanges["text"])
                UITextFields[UITextProperty.Name].text = UITextProperty.Value.ToString();
        }

        //Update the style
        if (UIPropertyChanges["style"] != null)
        {
            foreach (JProperty UITextStyle in UIPropertyChanges["style"])
            {
                if (UITextStyle.Value.ToString() == "bold")
                {
                    UITextFields[UITextStyle.Name].fontStyle = FontStyles.Bold;
                }
                if (UITextStyle.Value.ToString() == "italic")
                {
                    UITextFields[UITextStyle.Name].fontStyle = FontStyles.Italic;
                }
                if (UITextStyle.Value.ToString() == "small-caps")
                    UITextFields[UITextStyle.Name].fontStyle = FontStyles.SmallCaps;
            }
        }

        //Basic text settings
        if (UIPropertyChanges["setting"] != null)
        {
            foreach (JProperty UISettings in UIPropertyChanges["setting"])
            {
                //Apply each setting
                foreach (JProperty UISetting in UISettings.Value)
                {
                    if (UISetting.Name == "size")
                    {
                        //Turn off autosizing, if it's on
                        if (UITextFields[UISettings.Name].enableAutoSizing == true)
                            UITextFields[UISettings.Name].enableAutoSizing = false;

                        UITextFields[UISettings.Name].fontSize = (float)UISetting.Value;
                    }
                    if (UISetting.Name == "color")
                    {
                        //Try to convert the string to color
                        ColorUtility.TryParseHtmlString(UISetting.Value.ToString(), out Color newFontColor);
                        UITextFields[UISettings.Name].color = newFontColor;
                    }
                    if (UISetting.Name == "auto-size")
                        UITextFields[UISettings.Name].enableAutoSizing = bool.Parse(UISetting.Value.ToString());
                }
            }
        }

        //Fade effects
        if (UIPropertyChanges["fade"] != null)
        {
            //Configure the fade effects
            foreach (JProperty UITextFades in UIPropertyChanges["fade"])
            {
                //Set the default values
                float initialDelaySeconds = 0f;
                int alphaRampUpSpeed = 10;
                float sustainSeconds = 2;
                int alphaRampDownSpeed = 10;
                float animationWaitTime = 0.01f;

                foreach (JProperty UITextFade in UITextFades.Value)
                {
                    //Tweak settings as needed
                    if (UITextFade.Name == "initial-delay")
                        initialDelaySeconds = (float)UITextFade.Value;
                    if (UITextFade.Name == "ramp-up-speed")
                        alphaRampUpSpeed = (int)UITextFade.Value;
                    if (UITextFade.Name == "ramp-down-speed")
                        alphaRampDownSpeed = (int)UITextFade.Value;
                    if (UITextFade.Name == "sustain-time")
                        sustainSeconds = (float)UITextFade.Value;
                    if (UITextFade.Name == "animation-speed")
                        animationWaitTime = (float)UITextFade.Value;
                }

                //Initiate the fade animation
                textAlphaFader.FadeAlpha(initialDelaySeconds, alphaRampUpSpeed, sustainSeconds, alphaRampDownSpeed, animationWaitTime, UITextFields[UITextFades.Name]);
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
            if (JSONObject.Key == "ui-update-field") UpdateUIField(JSONObject.Value);
        }
    }
}
