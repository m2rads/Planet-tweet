using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class ImageLoader
{
    private UnityWebRequest _currentRequest;

    public IEnumerator LoadTextureFromUrl(string url)
    {
        if (_currentRequest != null)
        {
            _currentRequest.Abort();
        }

        _currentRequest = UnityWebRequestTexture.GetTexture(url);

        yield return _currentRequest.SendWebRequest();

        if (_currentRequest.isNetworkError || _currentRequest.isHttpError)
        {
            Debug.Log("Error loading image: " + _currentRequest.error);
        }
        else
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(_currentRequest);
            yield return texture;
        }

        _currentRequest = null;
    }
}
