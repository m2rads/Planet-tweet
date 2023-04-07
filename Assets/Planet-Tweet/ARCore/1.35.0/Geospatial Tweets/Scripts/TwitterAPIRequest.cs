using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <Summary>
/// Get the most recent tweets based on lat long coordinates with a point radius of 1km
/// </Summary>

public class TwitterAPIRequest
{
    private const string API_URL = "https://api.twitter.com/1.1/search/tweets.json?geocode=";
    private const string POINT_RADIUS = "1km";
    private const string BEARER_TOKEN = "Bearer AAAAAAAAAAAAAAAAAAAAABA9lQEAAAAAISLVk3GtigviI4Rhn8bWuWRr7do%3Db5zUegCGpfTvP7N29kuTx9SjfWXQwBcXLse2ohuSheXIEKqXzi";

    public IEnumerator GetData(string coordinates)
    {
        Debug.Log(coordinates);
        var url = API_URL + coordinates + "," + POINT_RADIUS;
        var request = UnityWebRequest.Get(url);

        request.SetRequestHeader("Authorization", BEARER_TOKEN);

        yield return request.SendWebRequest();

        if (request.isNetworkError || request.isHttpError)
        {
            Debug.Log(request.error);
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
