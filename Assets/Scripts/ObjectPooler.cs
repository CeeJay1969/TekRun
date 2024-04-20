using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Pool;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

public class ObjectPooler:MonoBehaviour
{
    public static ObjectPooler instance;

    string[] listOfPrefabs;
    string[,] prefabReferences;

    Dictionary<string, GameObject> prefabDictionary = new();

    public static Dictionary<string, IObjectPool<GameObject>> ObjectPoolDictionary
    {
        get { return _objectPoolDictionary; }
    }
    private static Dictionary<string, IObjectPool<GameObject>> _objectPoolDictionary;

    private void Awake()
    {
        if (instance == null)
        {
            //BuildGameObjectPool();
            instance = this;
            DontDestroyOnLoad(gameObject);

            //Subscribe to GameController event
            GameController.instance.OnGameControllerMessage += ObjectPooler_OnBuildChange;

            //Send message to GameController that the manager is ready
            JObject readyMessage = new();
            readyMessage.Add("object-pooler", "ready");
            ControllerMessages.OnControllerMessage(this, new ControllerMessages.ControllerMessage { JSONMessage = readyMessage.ToString() });
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void ObjectPooler_OnBuildChange(object sender, GameController.GameControllerMessage e)
    {
        string ArgMessage = e.JSONMessage;

        //Parse out the message
        JObject JSONObjects = JObject.Parse(@ArgMessage);

        //Step through the array of each JSONObject and look for messages for the object pooler
        foreach (var JSONObject in JSONObjects)
        {
            if (JSONObject.Key == "object-pooler")
            {
                if (JSONObject.Value["set-paths"] != null) SetPoolPath(JSONObject.Value["set-paths"]);
            }
        }
    }

    void SetPoolPath(JToken objectPaths)
    {
        //Clear pool dictionary if it contains anything
        _objectPoolDictionary = new();

        string prefabBasePath;

        //Iterate through each path and load the gameobject into the dictionary
        foreach (JToken objectPath in objectPaths)
        {
            //Set the base directory of where all the prefabs are located
            prefabBasePath = Utility.PREFAB_BASEPATH + objectPath["path"].ToString();

            //Get the file paths to all the prefabs
            listOfPrefabs = Directory.GetFiles(prefabBasePath, "*.prefab");

            //Create a list of paths and name of any found prefabs
            prefabReferences = new string[listOfPrefabs.Length, 2];

            string prefabName;
            string prefabPath;

            //Iterate through the list of found prefabs and add the name and path
            for (int xLoop = 0; xLoop < listOfPrefabs.Length; xLoop++)
            {
                prefabPath = listOfPrefabs[xLoop];
                //Remove everything in front of Prefabs directory
                prefabPath = prefabPath.Substring(prefabPath.IndexOf("Prefabs"));
                //Remove the .prefab at the end of the path
                prefabPath = prefabPath.Remove(prefabPath.Length - 7);

                //Get name of the prefab for searchabilty
                prefabName = prefabPath.Substring(prefabPath.LastIndexOf("\\") + 1);

                //Place the values in the refernce array
                prefabReferences[xLoop, 0] = prefabName;
                prefabReferences[xLoop, 1] = prefabPath;

                //Load the prefab and store it in the dictionary
                GameObject prefabGameObject;
                prefabGameObject = Resources.Load(prefabPath) as GameObject;
                prefabDictionary.Add(prefabName, prefabGameObject);
            }

            IObjectPool<GameObject> newGameObjectPool;

            //Loop through each prefab name and create appropriate pool
            for (int searchIndex = 0; searchIndex < prefabReferences.GetLength(0); searchIndex++)
            {
                //Get the prefab to instantiate
                GameObject prefabGameObject = prefabDictionary[prefabReferences[searchIndex, 0]];

                int poolItemSize = 100;
                int poolItemMaxSize = 1000;

                //Get pool attributes
                JObject SOAttributes = SOCache.instance.SOCacheDictionary[prefabReferences[searchIndex, 0]];

                //Check to see if pool-attributes exists in JObject
                JToken poolAttributes;
                if (SOAttributes.TryGetValue("pool-attributes", out poolAttributes))
                {
                    poolItemSize = poolAttributes[0].Value<int>("default");
                    poolItemMaxSize = poolAttributes[1].Value<int>("maximum");
                }
                //If it's not created get it from the defaults from the Global configuration
                else
                {
                    SOAttributes = SOCache.instance.SOCacheDictionary["Global"];
                    poolItemSize = SOAttributes.GetValue("pool-attributes-default")[0].Value<int>("default");
                    poolItemMaxSize = SOAttributes.GetValue("pool-attributes-default")[1].Value<int>("maximum");
                }

                //Create the pool
                newGameObjectPool = new ObjectPool<GameObject>(
                    createFunc: () => GameObject.Instantiate(prefabGameObject),
                    actionOnGet: gameObject => GetPoolObject(gameObject),
                    actionOnRelease: gameObject => ReleasePoolObject(gameObject),
                    collectionCheck: false,
                    defaultCapacity: poolItemSize,
                    maxSize: poolItemMaxSize
                    );

                //Add the pool to the pool dictionary
                _objectPoolDictionary.Add(prefabReferences[searchIndex, 0], newGameObjectPool);
            }
        }

        //Send message to GameController that the manager is ready
        JObject readyMessage = new();
        readyMessage.Add("object-pooler", "ready");
        ControllerMessages.OnControllerMessage(this, new ControllerMessages.ControllerMessage { JSONMessage = readyMessage.ToString() });
    }

    public void GetPoolObject(GameObject activateGameObject)
    {
        activateGameObject.SetActive(true);
    }

    public void ReleasePoolObject(GameObject releaseGameObject)
    {
        releaseGameObject.SetActive(false);
    }
}
