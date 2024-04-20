using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildWall : MonoBehaviour
{
//    [SerializeField] GameObject wall1;

//    Vector2 trackDimension = new Vector2(60, 51);
//    Vector2 wall1Dimension = new Vector2(10, 10);

//    float wallBuildChance = 0.1f;
//    float startWallBuild = 0.5f;

//    // Start is called before the first frame update
//    void Start()
//    {
//        //Chance to build walls or not
//        if (Random.Range(0f, 1f) < startWallBuild)
//        {
//            for (float xBuild = wall1Dimension.x; xBuild < trackDimension.x; xBuild += wall1Dimension.x)
//            {
//                for (float yBuild = wall1Dimension.y / 2f; yBuild < trackDimension.y - wall1Dimension.y; yBuild += wall1Dimension.y)
//                {
//                    if (Random.Range(0f, 1f) < wallBuildChance)
//                    {
//                        //Generate a coordinate and place a block on the track
//                        Vector3 blockCoordinate = new Vector3 (gameObject.transform.position.x - (trackDimension.x / 2f),
//                            5f, gameObject.transform.position.z - (trackDimension.y / 2f));
//                        Instantiate(wall1, new Vector3(xBuild, 0f, yBuild) + blockCoordinate, Quaternion.identity);
//                    }
//                }
//            }
//        }
//    }

//    // Update is called once per frame
//    void Update()
//    {
        
//    }
}
