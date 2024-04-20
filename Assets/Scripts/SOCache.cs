using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.IO;

public class SOCache : MonoBehaviour
{
    public static SOCache instance;

    const string PREFAB_BASEPATH = "c:" + "Assets\\Resources\\SOData";

    //Public dictionaries to cache resources
    Dictionary<string, JObject> soCacheDictionary = new Dictionary<string, JObject>();
    public Dictionary<string, JObject> SOCacheDictionary
    {
        get { return soCacheDictionary; }
    }

    //Load resources
    private void Awake()
    {
        //Get a list of all scriptable object files (asset) under the SObjectData directory
        string[] listOfAssets = Directory.GetFiles(PREFAB_BASEPATH, "*.asset", SearchOption.AllDirectories);
        JSONData SOFileData;

        if (instance == null)
        {
            //Load in each object and convert to JSON and add to the dictionary
            for (int fileIndex = 0; fileIndex < listOfAssets.Length; fileIndex++)
            {
                string assetPath = listOfAssets[fileIndex];
                //Remove the up to the "Resource" path
                assetPath = assetPath.Substring(assetPath.IndexOf("SOData"));
                //Remove the asset portion
                assetPath = assetPath.Remove(assetPath.Length - 6);

                //Load in contents of each file and convert to JSON data
                SOFileData = Resources.Load<JSONData>(assetPath);

                //Convert to JSON object
                JObject JSONFileData = JObject.Parse(SOFileData.FieldData);

                //Add to the dictionary
                soCacheDictionary.Add(SOFileData.name, JSONFileData);
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            //Send message to GameController to indicate that the SOcache is ready
            JObject readyMessage = new();
            readyMessage.Add("so-cache", "ready");
            ControllerMessages.OnControllerMessage(this, new ControllerMessages.ControllerMessage { JSONMessage = readyMessage.ToString() });
        }
        else
            Destroy(gameObject);
    }
}
