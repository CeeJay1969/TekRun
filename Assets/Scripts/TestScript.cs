using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //Text data
        JObject textProperty = new();
        JObject textProperties = new();
        //JObject passThru = new();

        textProperty.Add("header", "MISSION - " + "Mission #1");
        textProperty.Add("footer", "Welcome to the CAKE WALK");
        textProperties.Add("text", textProperty);
        //passThru.Add("PASS-THRU", textProperties);


        //Style data
        //JObject textStyle = new();
        //JObject textStyles = new();
        //textStyle.Add("header", "bold");
        //textStyle.Add("footer", "italic");
        //textStyles.Add("style", textStyle);

        JObject headerFadeData = new() {
            {"initial-delay", 1},
            {"sustain-time", 4}
        };
        JObject footerFadeData = new() {
            {"initial-delay", 2},
            {"sustain-time", 2}
        };

        JObject testMessage = new();
        JObject headerData = new() {
            {"text","MISSION : 1"},
            {"bold", true},
            {"auto-size", true},
            {"font-size", 30},
            {"color", "#80808000"},
            {"fade", headerFadeData},
            };
        JObject footerData = new() {
            {"text","Welcome to the CAKE WALK"},
            {"italic", true},
            {"color", "#F0F00000"},
            {"fade", footerFadeData},
            };
        JObject fullMessage = new()
        {
            {"header", headerData},
            {"footer", footerData}
        };

        ////Color and size for header and footer
        //JObject fontSetting1 = new();
        //JObject fontSettings1 = new();
        //fontSetting1.Add("font-size", 30);
        //fontSetting1.Add("color", "#F0F00000");
        //fontSettings1.Add("footer", fontSetting1);
        //JObject fontSetting2 = new();
        //JObject fontSettings2 = new();
        //fontSetting1.Add("auto-size", true);
        //fontSetting1.Add("color", "#80808000");
        //fontSettings1.Add("header", fontSetting2);

        ////Fade timing for header and footer
        //JObject headerFadeProperty = new();
        //JObject headerFadeProperties = new();
        //headerFadeProperty.Add("initial-delay", 1);
        //headerFadeProperty.Add("sustain-time", 4);
        //headerFadeProperty.Add("header", headerFadeProperty);
        //headerFadeProperties.Add("fade", headerFadeProperty);
        //JObject footerFadeProperty = new();
        //JObject footerFadeProperties = new();
        //footerFadeProperty.Add("initial-delay", 2);
        //footerFadeProperty.Add("sustain-time", 2);
        //footerFadeProperty.Add("header", footerFadeProperty);
        //footerFadeProperty.Add("fade", footerFadeProperty);


        //JObject missionProperties = new();
        //missionProperties.Add(textProperties);
        //missionProperties.Add(fontSettings1);
        //missionProperties.Add(fontSettings2);
        testMessage.Add("ui-controller", fullMessage);
        Debug.Log(testMessage);


    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
