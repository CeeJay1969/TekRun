
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class Utility
{
    const float FIXED_UPDATE = 0.02f;

    static public string PREFAB_BASEPATH = "c:" + "Assets\\Resources\\Prefabs";

    static public GameObject SkyDomeEffect
    {
        get { return _skyDomeEffect; }
        set { _skyDomeEffect = value; }
    }
    static private GameObject _skyDomeEffect;
    
    public enum TransitionType
    {
        LINEAR,
        FAST,
        SLOW
    }
    public enum CurveType
    {
        LOG_INC,
        LOG_DEC,
        EXP_INC,
        EXP_DEC,
        LINEAR_POS,
        LINEAR_NEG
    }
    static public void SetVelocityWithForce(Rigidbody theRb, Vector3 desiredVelocity, bool absolute)
    {
        SetVelocityWithForce(theRb, desiredVelocity, absolute, Vector3.zero);
    }

    static public void SetVelocityWithForce(Rigidbody theRb, Vector3 desiredVelocity, bool absolute, Vector3 ignoreVector)
    {
        //Convert the current velocity to force
        Vector3 currentForce = VelocityToForce(theRb.velocity, theRb.mass);
        //Conver the desired velocity to a force
        Vector3 calculatedForce = VelocityToForce(desiredVelocity, theRb.mass);

        if (absolute == false)
        {
            //Relative, just add the forces
            calculatedForce += currentForce;
        }
        else
        {
            //Absolute, change the velocity
            calculatedForce = calculatedForce - currentForce;
        }

        //Check to see if any vector components should be ignored
        if (ignoreVector.x != 0)
            calculatedForce.x = theRb.velocity.x;
        if (ignoreVector.y != 0)
            calculatedForce.y = theRb.velocity.y;
        if (ignoreVector.z != 0)
            calculatedForce.z = theRb.velocity.z;
        
        theRb.AddForce(calculatedForce);
    }
    static public Vector3 VelocityToForce(Vector3 theVector, float theMass)
    {
        Vector3 theForce = new Vector3(theVector.x / FIXED_UPDATE, theVector.y / FIXED_UPDATE, theVector.z / FIXED_UPDATE) * theMass;
        return theForce;
    }

    static public void RotateToTarget(Rigidbody rb, Transform targetTransform, Vector3 upAxis,
    float maxAngularRotation, float maxAcceleration, float transitionAngularDistance, out bool atTarget)
    {
        atTarget = false;
        rb.transform.LookAt(targetTransform, upAxis);
    }

    static public float VelocityToPosition(float sourcePosition, float sourceVelocity, float maxVelocity, float maxAcceleration,
        float transitionDistance, float targetPosition, float targetVelocity, TransitionType transitionType, out bool atTarget)
    {
        float resultVelocity = sourceVelocity;
        float goalVelocity = 0;
        atTarget = false;
        float atTargetOffset = 0.01f;
        float atVelocityOffset = 0.01f;

        if (maxVelocity < 0f)
        {
            maxVelocity = -maxVelocity;
            Debug.LogWarning("Value 'maxVelocity' was less than zero, flipping sign");
        }

        //Determine the distance
        float distanceToTarget = Mathf.Abs(sourcePosition - targetPosition);

        //Already at the target position?
        if ((distanceToTarget < atTargetOffset) && (distanceToTarget > -atTargetOffset) &&
            (Mathf.Abs(sourceVelocity - targetVelocity) < atVelocityOffset))
        {
            atTarget = true;
            return targetVelocity;
        }

        //Getting close to target, set the goal velocity to start transitioning to the target velocity
        if (distanceToTarget < transitionDistance)
        {
            if (Mathf.Abs(targetVelocity) < Mathf.Abs(sourceVelocity))
                goalVelocity = CalculateCurve(transitionDistance - distanceToTarget, transitionDistance, CurveType.LOG_DEC) * maxVelocity;
            if (Mathf.Abs(targetVelocity) >= Mathf.Abs(sourceVelocity))
                goalVelocity = CalculateCurve(transitionDistance - distanceToTarget, transitionDistance, CurveType.LOG_INC) * maxVelocity;

            if (sourcePosition > targetPosition)
                goalVelocity = -goalVelocity;

            //goalVelocity = targetPosition - Mathf.Lerp(sourcePosition, targetPosition, 0.25f) + sourceVelocity;
            //return goalVelocity;

            //goalVelocity = sourceVelocity;
        }
        else
        {
            if (sourcePosition < targetPosition) goalVelocity = maxVelocity;
            else goalVelocity = -maxVelocity;

            if (resultVelocity < goalVelocity)
                goalVelocity = resultVelocity + maxAcceleration;

            if (resultVelocity > goalVelocity)
                goalVelocity = resultVelocity - maxAcceleration;
        }

        //Keep the result velocity within maximum volocity
        goalVelocity = Mathf.Clamp(goalVelocity, -maxVelocity, maxVelocity);

        resultVelocity = goalVelocity;

        return resultVelocity;
    }

    //Calculate animation curves
    static public float CalculateCurve(float currentValue, float maxValue, CurveType curve)
    {
        float calcCurveValue = 0f;

        if (curve == CurveType.LINEAR_POS)
        {
            calcCurveValue = currentValue / maxValue;
        }
        if (curve == CurveType.LINEAR_NEG)
        {
            calcCurveValue = 1 - (currentValue / maxValue);
        }
        if (curve == CurveType.LOG_INC)
        {
            calcCurveValue = Mathf.Sin((currentValue / maxValue) * (Mathf.PI / 2f));
        }
        if (curve == CurveType.LOG_DEC)
        {
            calcCurveValue = Mathf.Sin((currentValue / maxValue) * (Mathf.PI / 2f) + (Mathf.PI / 2f));
        }
        if (curve == CurveType.EXP_INC)
        {
            calcCurveValue = 1 + Mathf.Sin((currentValue / maxValue) * (Mathf.PI / 2f) + (Mathf.PI * 1.5f));
        }
        if (curve == CurveType.EXP_DEC)
        {
            calcCurveValue = 1 + Mathf.Sin((currentValue / maxValue) * (Mathf.PI / 2f) + (Mathf.PI));
        }

        return calcCurveValue;
    }

    //Return a set off non-repeating integers for a specified range, with an exclusion set
    public static void RandomNonRepeating(int numberOfInts, Vector2Int numberRange, out int[] randomResultSet, int[] excludeSet)
    {
        //Error checking the number range
        if (numberRange.x >= numberRange.y)
        {
            Debug.LogError("First number must be less than the second number in number range");
            randomResultSet = new int[1];
            return;
        }
        if (numberOfInts + excludeSet.Length > numberRange.y - numberRange.x + 1)
        {
            Debug.LogError("Number of requested integers: " + numberOfInts + ".  Cannot be greater than the available numbers in the range: " + numberRange + ".  With the exclude set size of: " + excludeSet.Length);

            randomResultSet = new int[1];
            return;
        }

        int randomNumberSlot;
        int slotRange = numberRange.y - numberRange.x + 1;

        //Allocate the range and populate with the range of pickable numbers
        List<int> randomNumberPickSet = new List<int>(slotRange);
        for (int numberRangePopulate = 0; numberRangePopulate < slotRange; numberRangePopulate++)
        {
            bool excludedNumber = false;

            //Check to see if this in the exlclusion set, if so don't add it
            if (excludeSet.Length != 0)
            {
                foreach (int exclusionNumber in excludeSet)
                {
                    //Number is part of the exclusion set, so don't add it
                    if (exclusionNumber == (numberRangePopulate + numberRange.x))
                    {
                        excludedNumber = true;
                    }
                }
            }

            //If the number considered is not part of the exlusion set add it
            if (!excludedNumber)
            {
                randomNumberPickSet.Add(numberRangePopulate + numberRange.x);
            }
        }

        //Random numbers to be returned in this set
        randomResultSet = new int[numberOfInts];

        //Loop to fill the numbers into the result set
        for (int randomNumberCount = 0; randomNumberCount < numberOfInts; randomNumberCount++)
        {
            //Choose a random slot
            randomNumberSlot = Random.Range(0, slotRange - 1);
            randomNumberSlot = randomNumberSlot % randomNumberPickSet.Count;

            //Put that value in the random result set
            randomResultSet[randomNumberCount] = randomNumberPickSet[randomNumberSlot];

            //Remove that number from the pick set
            randomNumberPickSet.RemoveAt(randomNumberSlot);
        }
    }

    //Return a set off non-repeating integers for a specified range
    public static void RandomNonRepeating(int numberOfInts, Vector2Int numberRange, out int[] randomResultSet)
    {
        //Exclusion set not used
        int[] emptyExcludeSet = new int[0];

        RandomNonRepeating(numberOfInts, numberRange, out randomResultSet, emptyExcludeSet);
    }

    public static Vector3 JSONtoVector3 (JObject ConvertJSON)
    {
        Vector3 newVector3 = Vector3.zero;

        foreach (var coordinateValue in ConvertJSON)
        {
            if (coordinateValue.Key.ToString() == "X")
                newVector3.x = (float)coordinateValue.Value;
            if (coordinateValue.Key.ToString() == "Y")
                newVector3.y = (float)coordinateValue.Value;
            if (coordinateValue.Key.ToString() == "Z")
                newVector3.z = (float)coordinateValue.Value;
        }

        return newVector3;
    }

    public static JObject Vector3toJSON(Vector3 ConvertVector3)
    {
        JObject newJSON = new();

        newJSON.Add("X", ConvertVector3.x);
        newJSON.Add("Y", ConvertVector3.y);
        newJSON.Add("Z", ConvertVector3.z);

        return newJSON;
    }

    //Gather list of GameObjects attached to gameObjectWeaponCheck that match gameObjectNameStartsWith
    public static List<GameObject> FindGameObjects(GameObject gameObjectSearchCheck, string gameObjectNameStartsWith)
    {
        List<GameObject> foundGameObject = null;

        //Add the current gameoject if it matches the starts with criteria
        if (gameObjectSearchCheck.name.StartsWith(gameObjectNameStartsWith))
        {
            if (foundGameObject == null)
            {
                foundGameObject = new List<GameObject>();
            }

            foundGameObject.Add(gameObjectSearchCheck);
        }

        //Check of children, make recursive call if they exist
        if (gameObjectSearchCheck.transform.childCount != 0)
        {
            for (int objectChildCount = 0; objectChildCount < gameObjectSearchCheck.transform.childCount; objectChildCount++)
            {
                List<GameObject> childGameObjects = FindGameObjects(gameObjectSearchCheck.transform.GetChild(objectChildCount).gameObject, gameObjectNameStartsWith);
                if (childGameObjects != null)
                {
                    if (foundGameObject == null)
                    {
                        foundGameObject = new List<GameObject>();
                    }
                    foundGameObject.AddRange(childGameObjects);
                }
            }
        }

        return foundGameObject;
    }
}
