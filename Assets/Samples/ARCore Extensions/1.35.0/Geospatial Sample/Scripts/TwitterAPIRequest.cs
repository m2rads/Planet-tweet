using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <Summary>
/// Get the most recent tweets based on lat long coordinates with a point radius of 1km
/// </Summary>

public class TwitterAPIRequest : MonoBehaviour
{
    private const string API_URL = "https://api.twitter.com/1.1/search/tweets.json";
    private const string POINT_RADIUS = "1km";
    private const string BEARER_TOKEN = "Bearer AAAAAAAAAAAAAAAAAAAAABA9lQEAAAAAiyYtfnDZiSOH%2B4IsRchUL0JlIro%3DtzVzT1geVNHnnGLSqoRPoSZjJsqvg1IlSuBXes1GiyGh8afYGu";

    public IEnumerator GetData(string coordinates)
    {
        var url = API_URL + "geocode=" + coordinates + POINT_RADIUS;
        var request = new UnityWebRequest(url, "GET");

        request.SetRequestHeader("Authorization", BEARER_TOKEN);

        yield return request.SendWebRequest();

        if (request.isNetworkError || request.isHttpError)
        {
            Debug.LogError(request.error);
        }
        else
        {
            var json = request.downloadHandler.text;
            Debug.Log(json);
            // Do something with the search results
            yield return json;
        }
    }
}
