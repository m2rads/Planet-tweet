// <copyright file="GeospatialController.cs" company="Google LLC">
//
// Copyright 2022 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

namespace Google.XR.ARCoreExtensions.Samples.Geospatial
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
    using UnityEngine.XR.ARFoundation;
    using UnityEngine.XR.ARSubsystems;
    using Newtonsoft.Json;
    using UnityEngine.Networking;
    using TMPro;
    using Mapbox.Utils;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.Utilities;


#if UNITY_ANDROID
    using UnityEngine.Android;
#endif

    /// <summary>
    /// Controller for Geospatial sample.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:ParameterMustNotSpanMultipleLines",
        Justification = "Bypass source check.")]
    public class GeospatialController : MonoBehaviour
    {
        [Header("AR Components")]

        /// <summary>
        /// The ARSessionOrigin used in the sample.
        /// </summary>
        public ARSessionOrigin SessionOrigin;

        /// <summary>
        /// The ARSession used in the sample.
        /// </summary>
        public ARSession Session;

        /// <summary>
        /// The ARAnchorManager used in the sample.
        /// </summary>
        public ARAnchorManager AnchorManager;

        /// <summary>
        /// The ARRaycastManager used in the sample.
        /// </summary>
        public ARRaycastManager RaycastManager;

        /// <summary>
        /// The AREarthManager used in the sample.
        /// </summary>
        public AREarthManager EarthManager;

        /// <summary>
        /// The ARCoreExtensions used in the sample.
        /// </summary>
        public ARCoreExtensions ARCoreExtensions;

        [Header("UI Elements")]

        /// <summary>
        /// A 3D object that presents an Geospatial Anchor.
        /// </summary>
        public GameObject GeospatialPrefab;

        /// <summary>
        /// A 3D object that presents an Geospatial Terrain anchor.
        /// </summary>
        public GameObject TerrainPrefab;

        /// <summary>
        /// UI element showing privacy prompt.
        /// </summary>
        public GameObject PrivacyPromptCanvas;

        /// <summary>
        /// UI element showing VPS availability notification.
        /// </summary>
        public GameObject VPSCheckCanvas;

        /// <summary>
        /// UI element containing all AR view contents.
        /// </summary>
        public GameObject ARViewCanvas;

        /// <summary>
        /// UI element for clearing all anchors, including history.
        /// </summary>
        public Button ClearAllButton;

        /// <summary>
        /// UI element for adding a new anchor at current location.
        /// </summary>
        public Button SetAnchorButton;

        /// <summary>
        /// UI element that enables terrain anchors.
        /// </summary>
        // public Toggle TerrainToggle;

        /// <summary>
        /// UI element to guide user to scan their enviornment
        /// </summary>
        // public GameObject InfoPanel;
        public GameObject ARGuidance;

        /// <summary>
        /// Text displaying <see cref="GeospatialPose"/> information at runtime.
        /// </summary>
        // public Text InfoText;

        /// <summary>
        /// Text displaying in a snack bar at the bottom of the screen.
        /// </summary>
        public Text SnackBarText;

        /// <summary>
        /// Text displaying debug information, only activated in debug build.
        /// </summary>
        // public Text DebugText;

        /// <summary>
        /// Help message shows while localizing.
        /// </summary>
        private const string _localizingMessage = "Localizing your device to set anchor.";

        /// <summary>
        /// Help message shows while initializing Geospatial functionalities.
        /// </summary>
        private const string _localizationInitializingMessage =
            "Initializing Geospatial functionalities.";

        /// <summary>
        /// Help message shows when <see cref="AREarthManager.EarthTrackingState"/> is not tracking
        /// or the pose accuracies are beyond thresholds.
        /// </summary>
        private const string _localizationInstructionMessage =
            "Point your camera at buildings, stores, and signs near you.";

        /// <summary>
        /// Help message shows when location fails or hits timeout.
        /// </summary>
        private const string _localizationFailureMessage =
            "Localization not possible.\n" +
            "Close and open the app to restart the session.";

        /// <summary>
        /// Help message shows when location success.
        /// </summary>
        private const string _localizationSuccessMessage = "Localization completed.";

        /// <summary>
        /// Help message shows when resolving takes too long.
        /// </summary>
        private const string _resolvingTimeoutMessage =
            "Still resolving the terrain anchor.\n" +
            "Please make sure you're in an area that has VPS coverage.";

        /// <summary>
        /// The timeout period waiting for localization to be completed.
        /// </summary>
        private const float _timeoutSeconds = 180;

        /// <summary>
        /// Indicates how long a information text will display on the screen before terminating.
        /// </summary>
        private const float _errorDisplaySeconds = 3;

        /// <summary>
        /// The key name used in PlayerPrefs which indicates whether the privacy prompt has
        /// displayed at least one time.
        /// </summary>
        private const string _hasDisplayedPrivacyPromptKey = "HasDisplayedGeospatialPrivacyPrompt";

        /// <summary>
        /// The key name used in PlayerPrefs which stores geospatial anchor history data.
        /// The earliest one will be deleted once it hits storage limit.
        /// </summary>
        private const string _persistentGeospatialAnchorsStorageKey = "PersistentGeospatialAnchors";

        /// <summary>
        /// The limitation of how many Geospatial Anchors can be stored in local storage.
        /// </summary>
        private const int _storageLimit = 5;

        /// <summary>
        /// Accuracy threshold for orientation yaw accuracy in degrees that can be treated as
        /// localization completed.
        /// </summary>
        private const double _orientationYawAccuracyThreshold = 25;

        /// <summary>
        /// Accuracy threshold for heading degree that can be treated as localization completed.
        /// </summary>
        private const double _headingAccuracyThreshold = 25;

        /// <summary>
        /// Accuracy threshold for altitude and longitude that can be treated as localization
        /// completed.
        /// </summary>
        private const double _horizontalAccuracyThreshold = 20;

        /// <summary>
        /// Color reference for clear all button
        /// </summary>
        private ColorBlock ClearAllButtonColors;

        /// <summary>
        /// Color reference for clear all button
        /// </summary>
        private ColorBlock SetAnchorButtonColors;

        private bool _waitingForLocationService = false;
        private bool _isInARView = false;
        private bool _isReturning = false;
        private bool _isLocalizing = false;
        private bool _enablingGeospatial = false;
        private bool _shouldResolvingHistory = false;
        private bool _usingTerrainAnchor = false;
        private float _localizationPassedTime = 0f;
        private float _configurePrepareTime = 3f;
        private GeospatialAnchorHistoryCollection _historyCollection = null;
        private List<GameObject> _anchorObjects = new List<GameObject>();
        private IEnumerator _startLocationService = null;
        private IEnumerator _asyncCheck = null;
        public static List<GeospatialAnchorHistory> _histories = new List<GeospatialAnchorHistory>();
        TwitterAPIRequest twitterAPIRequest = new TwitterAPIRequest();
        ImageLoader imageDownloader = new ImageLoader();
        TweetResponseObject model;
        string coordinates = "";


        /// <summary>
        /// bench mark for spawn on map logic
        /// </summary>
        [SerializeField]
		AbstractMap _map;

        [SerializeField]
		float _spawnScale = 100f;

		[SerializeField]
		GameObject _markerPrefab;

        List<GameObject> _spawnedTweets;
        List<Vector2d> _tweetLocations;


        // [SerializeField]
        // private LocationProvider _locationProvider;

        /// <summary>
        /// Request for handling tweets
        /// </summary>
        public void GetTweets() {
            StartCoroutine(GetTwitterResponse());
        }

        IEnumerator GetTwitterResponse()
        {
            var pose = EarthManager.CameraGeospatialPose;
            Quaternion eunRotation = pose.EunRotation;
#if UNITY_IOS
            // Update the quaternion from landscape orientation to portrait orientation.
            Quaternion quaternion = Quaternion.Euler(Vector3.forward * 90);
            eunRotation = eunRotation * quaternion;
#endif
            coordinates = pose.Latitude + "," + pose.Longitude;

            string response = "";
            var twitterData = twitterAPIRequest.GetData(coordinates);
            yield return StartCoroutine(twitterData);
            response = twitterData.Current as string;
            model = JsonConvert.DeserializeObject<TweetResponseObject>(response);
            Debug.LogError(response);
            Debug.Log(" this is twitter responese object" + model);

            // fill the array with histories
            foreach (var item in model.statuses)
            {
                double? lat = null;
                double? lon = null;
                
                try
                {
                    lat = item.place?.bounding_box?.coordinates?[0][0][1];
                    lon = item.place?.bounding_box?.coordinates?[0][0][0];
                }
                catch (Exception ex)
                {
                    // Handle the exception as appropriate for your application
                    Debug.LogError($"Exception while retrieving lat/lon values: {ex.Message}");
                    continue; // Skip this iteration and move on to the next item
                }

                if (lat != null && lon != null) {
                    // 
                    // 
                    // you need to delete Value
                    _histories.Add(new GeospatialAnchorHistory(lat.Value, lon.Value, 4, eunRotation, item));
                    _tweetLocations.Add(new Vector2d((float)lat.Value, (float)lon.Value));
                }    
            }
        }

        /// <summary>
        /// Callback for handling image loader 
        /// </summary>
        public void UpdateImageTexture(string url, Action<Texture2D> callback)
        {
            StartCoroutine(GetTextureFromUrl(url, callback));
        }

        /// <summary>
        /// Handler function for downloading images using image urls
        /// </summary>
        private IEnumerator GetTextureFromUrl(string url, Action<Texture2D> callback)
        {
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log("Failed to download image: " + www.error);
                callback(null);
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                callback(texture);
            }
        }

        /// <summary>
        /// Callback handling "Get Started" button click event in Privacy Prompt.
        /// </summary>
        public void OnGetStartedClicked()
        {
            PlayerPrefs.SetInt(_hasDisplayedPrivacyPromptKey, 1);
            PlayerPrefs.Save();
            SwitchToARView(true);
        }

        /// <summary>
        /// Callback handling "Learn More" Button click event in Privacy Prompt.
        /// </summary>
        public void OnLearnMoreClicked()
        {
            Application.OpenURL(
                "https://developers.google.com/ar/data-privacy");
        }

        /// <summary>
        /// Callback handling "Clear All" button click event in AR View.
        /// </summary>
        public void OnClearAllClicked()
        {
            foreach (var anchor in _anchorObjects)
            {
                Destroy(anchor);
            }
            
            foreach (var spawnedTweet in _spawnedTweets)
            {
                Destroy(spawnedTweet);
            }

            _anchorObjects.Clear();
            _spawnedTweets.Clear();
            _historyCollection.Collection.Clear();
            SnackBarText.text = "Tweet(s) cleared!";
            SetAnchorButton.interactable = true;
            
            ClearAllButtonColors.disabledColor = Color.gray;
            ClearAllButton.colors = ClearAllButtonColors;
            ClearAllButton.interactable = false;
            // ClearAllButton.gameObject.SetActive(false);
            SaveGeospatialAnchorHistory();
        }

        /// <summary>
        /// Callback handling "Continue" button click event in AR View.
        /// </summary>
        public void OnContinueClicked()
        {
            VPSCheckCanvas.SetActive(false);
        }

        /// <summary>
        /// Callback handling "Set Anchor" button click event in AR View.
        /// </summary>
        public void OnSetAnchorClicked()
        {
            ClearAllButton.interactable = true;
            var pose = EarthManager.CameraGeospatialPose;
            Quaternion eunRotation = pose.EunRotation;
#if UNITY_IOS
            // Update the quaternion from landscape orientation to portrait orientation.
            Quaternion quaternion = Quaternion.Euler(Vector3.forward * 90);
            eunRotation = eunRotation * quaternion;
#endif
            // GeospatialAnchorHistory history = new GeospatialAnchorHistory(
            //     pose.Latitude, pose.Longitude, 4, eunRotation, _histories[0].Status);

            // spawn the tweets on the map
            for (int i = 0; i < _tweetLocations.Count; i++)
			{
				var instance = Instantiate(_markerPrefab);
				instance.transform.localPosition = _map.GeoToWorldPosition(_tweetLocations[i], true);
				instance.transform.localScale = new Vector3(_spawnScale, _spawnScale, _spawnScale);
				_spawnedTweets.Add(instance);
			}

            try {
                var anchor = PlaceGeospatialAnchor(_histories[0], true);
                if (anchor != null)
                {
                    _historyCollection.Collection.Add(_histories[0]);
                }

                for (int i = 1; i <= _histories.Count; i++) {
                    anchor = PlaceGeospatialAnchor(_histories[i], true);
                    if (anchor != null)
                    {
                        _historyCollection.Collection.Add(_histories[i]);
                    } 
                }
            }
            // figure out the precise exception
            catch(Exception e)
            {
                Debug.Log(e);
            }



            // ClearAllButton.gameObject.SetActive(_anchorObjects.Count > 0);
            // ClearAllButton.interactable = (_anchorObjects.Count > 0);
            SaveGeospatialAnchorHistory();
            SetAnchorButtonColors.disabledColor = Color.gray;
            SetAnchorButton.colors = SetAnchorButtonColors;
            SetAnchorButton.interactable = false;
        }

        /// <summary>
        /// Callback handling "Terrain" toggle event in AR View.
        /// </summary>
        /// <param name="enabled">Whether to enable terrain anchors.</param>
        public void OnTerrainToggled(bool enabled)
        {
            _usingTerrainAnchor = enabled;
        }

        void SetActiveRecursively(GameObject obj, bool isActive)
        {
            obj.SetActive(isActive);
            foreach (Transform child in obj.transform)
            {
                SetActiveRecursively(child.gameObject, isActive);
            }
        }

        /// <summary>
        /// Unity's Awake() method.
        /// </summary>
        public void Awake()
        {
            // Lock screen to portrait.
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.orientation = ScreenOrientation.Portrait;

            // Enable geospatial sample to target 60fps camera capture frame rate
            // on supported devices.
            // Note, Application.targetFrameRate is ignored when QualitySettings.vSyncCount != 0.
            Application.targetFrameRate = 60;

            if (SessionOrigin == null)
            {
                Debug.LogError("Cannot find ARSessionOrigin.");
            }

            if (Session == null)
            {
                Debug.LogError("Cannot find ARSession.");
            }

            if (ARCoreExtensions == null)
            {
                Debug.LogError("Cannot find ARCoreExtensions.");
            }
        }

        /// <summary>
        /// Unity's OnEnable() method.
        /// </summary>
        public void OnEnable()
        {
            _startLocationService = StartLocationService();
            StartCoroutine(_startLocationService);
            
            // initlized the map prefab locations for tweets
            _tweetLocations = new List<Vector2d>();
            _spawnedTweets = new List<GameObject>();

            ClearAllButtonColors = ClearAllButton.colors;
            SetAnchorButtonColors = SetAnchorButton.colors;

            _isReturning = false;
            _enablingGeospatial = false;
            // InfoPanel.SetActive(false);
            // ARGuidance.SetActive(false);
            SetActiveRecursively(ARGuidance, true);
            // SetAnchorButton.gameObject.SetActive(false);
            SetAnchorButtonColors.disabledColor = Color.gray;
            SetAnchorButton.colors = SetAnchorButtonColors;
            SetAnchorButton.interactable = false;
            // TerrainToggle.gameObject.SetActive(false);
            // ClearAllButton.gameObject.SetActive(false);
            ClearAllButtonColors.disabledColor = Color.gray;
            ClearAllButton.colors = ClearAllButtonColors;
            ClearAllButton.interactable = false;
            // DebugText.gameObject.SetActive(Debug.isDebugBuild && EarthManager != null);
            // TerrainToggle.onValueChanged.AddListener(OnTerrainToggled);

            _localizationPassedTime = 0f;
            _isLocalizing = true;
            SnackBarText.text = _localizingMessage;

            LoadGeospatialAnchorHistory();
            _shouldResolvingHistory = _historyCollection.Collection.Count > 0;

            SwitchToARView(PlayerPrefs.HasKey(_hasDisplayedPrivacyPromptKey));
        }

        /// <summary>
        /// Unity's OnDisable() method.
        /// </summary>
        public void OnDisable()
        {
            StopCoroutine(_asyncCheck);
            _asyncCheck = null;
            StopCoroutine(_startLocationService);
            _startLocationService = null;
            Debug.Log("Stop location services.");
            Input.location.Stop();

            foreach (var anchor in _anchorObjects)
            {
                Destroy(anchor);
            }

            _anchorObjects.Clear();
            SaveGeospatialAnchorHistory();
        }

        /// <summary>
        /// Unity's Update() method.
        /// </summary>
        public void Update()
        {
            if (!_isInARView)
            {
                return;
            }

            int count = _spawnedTweets.Count;
            for (int i = 0; i < count; i++)
            {
                var spawnedTweet = _spawnedTweets[i];
                var tweetlocation = _tweetLocations[i];
                spawnedTweet.transform.localPosition = _map.GeoToWorldPosition(tweetlocation, true);
                spawnedTweet.transform.localScale = new Vector3(_spawnScale, _spawnScale, _spawnScale);
            }
			

            //  delete  this
            UpdateDebugInfo();

            // Check session error status.
            LifecycleUpdate();
            if (_isReturning)
            {
                return;
            }

            if (ARSession.state != ARSessionState.SessionInitializing &&
                ARSession.state != ARSessionState.SessionTracking)
            {
                return;
            }

            // Check feature support and enable Geospatial API when it's supported.
            var featureSupport = EarthManager.IsGeospatialModeSupported(GeospatialMode.Enabled);
            switch (featureSupport)
            {
                case FeatureSupported.Unknown:
                    return;
                case FeatureSupported.Unsupported:
                    ReturnWithReason("Geospatial API is not supported by this devices.");
                    return;
                case FeatureSupported.Supported:
                    if (ARCoreExtensions.ARCoreExtensionsConfig.GeospatialMode ==
                        GeospatialMode.Disabled)
                    {
                        Debug.Log("Geospatial sample switched to GeospatialMode.Enabled.");
                        ARCoreExtensions.ARCoreExtensionsConfig.GeospatialMode =
                            GeospatialMode.Enabled;
                        _configurePrepareTime = 3.0f;
                        _enablingGeospatial = true;
                        return;
                    }
                    break;
            }

            // Waiting for new configuration taking effect.
            if (_enablingGeospatial)
            {
                _configurePrepareTime -= Time.deltaTime;
                if (_configurePrepareTime < 0)
                {
                    _enablingGeospatial = false;
                }
                else
                {
                    return;
                }
            }

            // Check earth state.
            var earthState = EarthManager.EarthState;
            if (earthState == EarthState.ErrorEarthNotReady)
            {
                SnackBarText.text = _localizationInitializingMessage;
                return;
            }
            else if (earthState != EarthState.Enabled)
            {
                string errorMessage =
                    "Geospatial sample encountered an EarthState error: " + earthState;
                Debug.LogWarning(errorMessage);
                SnackBarText.text = errorMessage;
                return;
            }

            // Check earth localization.
            bool isSessionReady = ARSession.state == ARSessionState.SessionTracking &&
                Input.location.status == LocationServiceStatus.Running;
            var earthTrackingState = EarthManager.EarthTrackingState;
            var pose = earthTrackingState == TrackingState.Tracking ?
                EarthManager.CameraGeospatialPose : new GeospatialPose();
            if (!isSessionReady || earthTrackingState != TrackingState.Tracking ||
                pose.OrientationYawAccuracy > _orientationYawAccuracyThreshold ||
                pose.HorizontalAccuracy > _horizontalAccuracyThreshold)
            {
                // Lost localization during the session.
                if (!_isLocalizing)
                {
                    _isLocalizing = true;
                    _localizationPassedTime = 0f;
                    // SetAnchorButton.gameObject.SetActive(false);
                    SetAnchorButtonColors.disabledColor = Color.gray;
                    SetAnchorButton.colors = SetAnchorButtonColors;
                    SetAnchorButton.interactable = false;
                    // TerrainToggle.gameObject.SetActive(false);
                    // ClearAllButton.gameObject.SetActive(false);
                    ClearAllButtonColors.disabledColor = Color.gray;
                    ClearAllButton.colors = ClearAllButtonColors;
                    ClearAllButton.interactable = false;
                    SetActiveRecursively(ARGuidance, true);
                    foreach (var go in _anchorObjects)
                    {
                        go.SetActive(false);
                    }
                }

                if (_localizationPassedTime > _timeoutSeconds)
                {
                    Debug.LogError("Geospatial sample localization passed timeout.");
                    ReturnWithReason(_localizationFailureMessage);
                }
                else
                {
                    _localizationPassedTime += Time.deltaTime;
                    SnackBarText.text = _localizationInstructionMessage;
                }
            }
            else if (_isLocalizing)
            {
                // Finished localization.
                _isLocalizing = false;
                _localizationPassedTime = 0f;
                GetTweets();
                SetActiveRecursively(ARGuidance, false);

                // SetAnchorButton.gameObject.SetActive(true);
                // SetAnchorButtonColors.disabledColor = Color.gray;
                // SetAnchorButton.colors = SetAnchorButtonColors;
                SetAnchorButton.interactable = true;

                // TerrainToggle.gameObject.SetActive(true);
                // ClearAllButton.gameObject.SetActive(_anchorObjects.Count > 0);
                ClearAllButton.interactable = (_anchorObjects.Count > 0);
                SnackBarText.text = _localizationSuccessMessage;
                foreach (var go in _anchorObjects)
                {
                    var terrainState = go.GetComponent<ARGeospatialAnchor>().terrainAnchorState;
                    if (terrainState != TerrainAnchorState.None &&
                        terrainState != TerrainAnchorState.Success)
                    {
                        // Skip terrain anchors that are still waiting for resolving
                        // or failed on resolving.
                        continue;
                    }

                    go.SetActive(true);
                }

                ResolveHistory();
            }
            else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began
                && !EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            {
                // Set anchor on screen tap.
                // PlaceAnchorByScreenTap(Input.GetTouch(0).position);
                Debug.Log("screen touch");
            }
            
            // ARGuidance.SetActive(true);
            // InfoPanel.SetActive(true);
            // if (earthTrackingState == TrackingState.Tracking)
            // {
            //     InfoText.text = string.Format(
            //     "Latitude/Longitude: {1}°, {2}°{0}" +
            //     "Horizontal Accuracy: {3}m{0}" +
            //     "Altitude: {4}m{0}" +
            //     "Vertical Accuracy: {5}m{0}" +
            //     "Eun Rotation: {6}{0}" +
            //     "Orientation Yaw Accuracy: {7}°",
            //     Environment.NewLine,
            //     pose.Latitude.ToString("F6"),
            //     pose.Longitude.ToString("F6"),
            //     pose.HorizontalAccuracy.ToString("F6"),
            //     pose.Altitude.ToString("F2"),
            //     pose.VerticalAccuracy.ToString("F2"),
            //     pose.EunRotation.ToString("F1"),
            //     pose.OrientationYawAccuracy.ToString("F1"));
            // }
            // else
            // {
            //     InfoText.text = "GEOSPATIAL POSE: not tracking";
            // }
        }

        private IEnumerator CheckTerrainAnchorState(ARGeospatialAnchor anchor)
        {
            if (anchor == null || _anchorObjects == null)
            {
                yield break;
            }

            int retry = 0;
            while (anchor.terrainAnchorState == TerrainAnchorState.TaskInProgress)
            {
                if (_anchorObjects.Count == 0 || !_anchorObjects.Contains(anchor.gameObject))
                {
                    Debug.LogFormat(
                        "{0} has been removed, exist terrain anchor state check.",
                        anchor.trackableId);
                    yield break;
                }

                if (retry == 100 && _anchorObjects.Last().Equals(anchor.gameObject))
                {
                    SnackBarText.text = _resolvingTimeoutMessage;
                }

                yield return new WaitForSeconds(0.1f);
                retry = Math.Min(retry + 1, 100);
            }

            anchor.gameObject.SetActive(
                !_isLocalizing && anchor.terrainAnchorState == TerrainAnchorState.Success);
            if (_anchorObjects.Last().Equals(anchor.gameObject))
            {
                SnackBarText.text = $"Terrain anchor state: {anchor.terrainAnchorState}";
            }

            yield break;
        }

        private void PlaceAnchorByScreenTap(Vector2 position)
        {
            List<ARRaycastHit> hitResults = new List<ARRaycastHit>();
            RaycastManager.Raycast(
                position, hitResults, TrackableType.Planes | TrackableType.FeaturePoint);
            if (hitResults.Count > 0)
            {
                GeospatialPose geospatialPose = EarthManager.Convert(hitResults[0].pose);
                GeospatialAnchorHistory history = new GeospatialAnchorHistory(
                    geospatialPose.Latitude, geospatialPose.Longitude, geospatialPose.Altitude,
                    geospatialPose.EunRotation, _histories[0].Status);
                var anchor = PlaceGeospatialAnchor(history, false);
                if (anchor != null)
                {
                    _historyCollection.Collection.Add(history);
                }

                // ClearAllButton.gameObject.SetActive(_anchorObjects.Count > 0);
                ClearAllButton.interactable = (_anchorObjects.Count > 0);
                SaveGeospatialAnchorHistory();
            }
        }

        private ARGeospatialAnchor PlaceGeospatialAnchor(
            GeospatialAnchorHistory history, bool terrain = false)
        {
            Quaternion eunRotation = history.EunRotation;
            if (eunRotation == Quaternion.identity)
            {
                // This history is from a previous app version and EunRotation was not used.
                eunRotation =
                    Quaternion.AngleAxis(180f - (float)history.Heading, Vector3.up);
            }
            var anchor = terrain ?
                AnchorManager.ResolveAnchorOnTerrain(
                    history.Latitude, history.Longitude, 0, eunRotation) :
                AnchorManager.AddAnchor(
                    history.Latitude, history.Longitude, history.Altitude, eunRotation);

                    if (anchor != null)

            if (anchor != null)
            {
                
                GameObject anchorGO = terrain ?
                    Instantiate(TerrainPrefab, anchor.transform) :
                    Instantiate(GeospatialPrefab, anchor.transform);
                anchor.gameObject.SetActive(!terrain);
                _anchorObjects.Add(anchor.gameObject);

                TextMeshPro[] textComponents = anchorGO.GetComponentsInChildren<TextMeshPro>();
                SpriteRenderer[] spriteRenderers = anchorGO.GetComponentsInChildren<SpriteRenderer>();

                if (textComponents.Length > 0)
                {
                    TextMeshPro tweetText = textComponents[0];
                    tweetText.text = history.Status.text;
                    
                    TextMeshPro profileHandle = textComponents[1];
                    profileHandle.text = history.Status.user.name;
                }

                if (spriteRenderers.Length > 0)
                {
                    string url = history.Status.user.profile_image_url_https;
                    UpdateImageTexture(url, (texture) => {
                        // // Create a sprite from the downloaded texture
                        // Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                        
                        // // Display the sprite in a SpriteRenderer component
                        SpriteRenderer spriteRenderer = spriteRenderers[0];
                        // spriteRenderer.sprite = sprite;

                        // Create a new Texture2D to hold the circular image
                        Texture2D circularTexture = texture;

                        // Set the clearPixelsColor to white
                        // circularTexture.SetPixels(Enumerable.Repeat(Color.white, circularTexture.width * circularTexture.height).ToArray());


                        // Create a new Color array to hold the pixels of the circular image
                        Color[] pixels = new Color[circularTexture.width * circularTexture.height];

                        // Calculate the center and radius of the circular image
                        int centerX = circularTexture.width / 2;
                        int centerY = circularTexture.height / 2;
                        int radius = Mathf.Min(centerX, centerY);

                        // Loop through all the pixels in the texture and set the corresponding pixel in the circular image
                        for (int y = 0; y < circularTexture.height; y++)
                        {
                            for (int x = 0; x < circularTexture.width; x++)
                            {
                                // Calculate the distance from the current pixel to the center of the image
                                float distance = Mathf.Sqrt(Mathf.Pow(x - centerX, 2) + Mathf.Pow(y - centerY, 2));

                                // If the distance is less than the radius, set the pixel to the corresponding pixel in the original texture
                                if (distance < radius)
                                {
                                    pixels[y * circularTexture.width + x] = texture.GetPixel(x, y);
                                }
                                // Otherwise, set the pixel to transparent
                                else
                                {
                                    pixels[y * circularTexture.width + x] = Color.white;
                                }
                            }
                        }

                        // Apply the pixels to the circular image and set the filter mode to point (for pixel art)
                        circularTexture.SetPixels(pixels);
                        circularTexture.Apply();
                        circularTexture.filterMode = FilterMode.Point;

                        // Create a new sprite from the circular image
                        Sprite sprite = Sprite.Create(circularTexture, new Rect(0, 0, circularTexture.width, circularTexture.height), Vector2.one * 0.5f);
                        spriteRenderer.sprite = sprite;

                    });
                }

                if (terrain)
                {
                    StartCoroutine(CheckTerrainAnchorState(anchor));
                }
                else
                {
                    SnackBarText.text = $"{_anchorObjects.Count} Tweet(s) Set! {_spawnedTweets.Count} markers Set!";
                }
            }
            else
            {
                SnackBarText.text = string.Format(
                    "Failed to set {0}!", terrain ? "a terrain anchor" : "an anchor");
            }

            return anchor;
        }


        private void ResolveHistory()
        {
            if (!_shouldResolvingHistory)
            {
                return;
            }

            _shouldResolvingHistory = false;
            foreach (var history in _historyCollection.Collection)
            {
                PlaceGeospatialAnchor(history);
            }

            // ClearAllButton.gameObject.SetActive(_anchorObjects.Count > 0);
            ClearAllButton.interactable = (_anchorObjects.Count > 0);
            SnackBarText.text = string.Format("{0} Tweet(s) set from history.",
                _anchorObjects.Count);
        }

        private void LoadGeospatialAnchorHistory()
        {
            if (PlayerPrefs.HasKey(_persistentGeospatialAnchorsStorageKey))
            {
                _historyCollection = JsonUtility.FromJson<GeospatialAnchorHistoryCollection>(
                    PlayerPrefs.GetString(_persistentGeospatialAnchorsStorageKey));

                // Remove all records created more than 24 hours and update stored history.
                DateTime current = DateTime.Now;
                _historyCollection.Collection.RemoveAll(
                    data => current.Subtract(data.CreatedTime).Days > 0);
                PlayerPrefs.SetString(_persistentGeospatialAnchorsStorageKey,
                    JsonUtility.ToJson(_historyCollection));
                PlayerPrefs.Save();
            }
            else
            {
                _historyCollection = new GeospatialAnchorHistoryCollection();
            }
        }

        private void SaveGeospatialAnchorHistory()
        {
            // Sort the data from latest record to earliest record.
            _historyCollection.Collection.Sort((left, right) =>
                right.CreatedTime.CompareTo(left.CreatedTime));

            // Remove the earliest data if the capacity exceeds storage limit.
            if (_historyCollection.Collection.Count > _storageLimit)
            {
                _historyCollection.Collection.RemoveRange(
                    _storageLimit, _historyCollection.Collection.Count - _storageLimit);
            }

            PlayerPrefs.SetString(
                _persistentGeospatialAnchorsStorageKey, JsonUtility.ToJson(_historyCollection));
            PlayerPrefs.Save();
        }

        private void SwitchToARView(bool enable)
        {
            _isInARView = enable;
            SessionOrigin.gameObject.SetActive(enable);
            Session.gameObject.SetActive(enable);
            ARCoreExtensions.gameObject.SetActive(enable);
            ARViewCanvas.SetActive(enable);
            ARGuidance.SetActive(enable);
            PrivacyPromptCanvas.SetActive(!enable);
            VPSCheckCanvas.SetActive(false);
            if (enable && _asyncCheck == null)
            {
                _asyncCheck = AvailabilityCheck();
                StartCoroutine(_asyncCheck);
            }
        }

        private IEnumerator AvailabilityCheck()
        {
            if (ARSession.state == ARSessionState.None)
            {
                yield return ARSession.CheckAvailability();
            }

            // Waiting for ARSessionState.CheckingAvailability.
            yield return null;

            if (ARSession.state == ARSessionState.NeedsInstall)
            {
                yield return ARSession.Install();
            }

            // Waiting for ARSessionState.Installing.
            yield return null;

#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Debug.Log("Requesting camera permission.");
                Permission.RequestUserPermission(Permission.Camera);
                yield return new WaitForSeconds(3.0f);
            }

            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                // User has denied the request.
                Debug.LogWarning(
                    "Failed to get camera permission. VPS availability check is not available.");
                yield break;
            }
#endif

            while (_waitingForLocationService)
            {
                yield return null;
            }

            if (Input.location.status != LocationServiceStatus.Running)
            {
                Debug.LogWarning(
                    "Location service is not running. VPS availability check is not available.");
                yield break;
            }

            // Update event is executed before coroutines so it checks the latest error states.
            if (_isReturning)
            {
                yield break;
            }

            var location = Input.location.lastData;
            var vpsAvailabilityPromise =
                AREarthManager.CheckVpsAvailability(location.latitude, location.longitude);
            yield return vpsAvailabilityPromise;

            Debug.LogFormat("VPS Availability at ({0}, {1}): {2}",
                location.latitude, location.longitude, vpsAvailabilityPromise.Result);
            VPSCheckCanvas.SetActive(vpsAvailabilityPromise.Result != VpsAvailability.Available);
        }

        private IEnumerator StartLocationService()
        {
            _waitingForLocationService = true;
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Debug.Log("Requesting fine location permission.");
                Permission.RequestUserPermission(Permission.FineLocation);
                yield return new WaitForSeconds(3.0f);
            }
#endif

            if (!Input.location.isEnabledByUser)
            {
                Debug.Log("Location service is disabled by User.");
                _waitingForLocationService = false;
                yield break;
            }

            Debug.Log("Start location service.");
            Input.location.Start();

            while (Input.location.status == LocationServiceStatus.Initializing)
            {
                yield return null;
            }

            _waitingForLocationService = false;
            if (Input.location.status != LocationServiceStatus.Running)
            {
                Debug.LogWarningFormat(
                    "Location service ends with {0} status.", Input.location.status);
                Input.location.Stop();
            }
        }

        private void LifecycleUpdate()
        {
            // Pressing 'back' button quits the app.
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                Application.Quit();
            }

            if (_isReturning)
            {
                return;
            }

            // Only allow the screen to sleep when not tracking.
            var sleepTimeout = SleepTimeout.NeverSleep;
            if (ARSession.state != ARSessionState.SessionTracking)
            {
                sleepTimeout = SleepTimeout.SystemSetting;
            }

            Screen.sleepTimeout = sleepTimeout;

            // Quit the app if ARSession is in an error status.
            string returningReason = string.Empty;
            if (ARSession.state != ARSessionState.CheckingAvailability &&
                ARSession.state != ARSessionState.Ready &&
                ARSession.state != ARSessionState.SessionInitializing &&
                ARSession.state != ARSessionState.SessionTracking)
            {
                returningReason = string.Format(
                    "Geospatial sample encountered an ARSession error state {0}.\n" +
                    "Please start the app again.",
                    ARSession.state);
            }
            else if (Input.location.status == LocationServiceStatus.Failed)
            {
                returningReason =
                    "Geospatial sample failed to start location service.\n" +
                    "Please start the app again and grant precise location permission.";
            }
            else if (SessionOrigin == null || Session == null || ARCoreExtensions == null)
            {
                returningReason = string.Format(
                    "Geospatial sample failed with missing AR Components.");
            }

            ReturnWithReason(returningReason);
        }

        private void ReturnWithReason(string reason)
        {
            if (string.IsNullOrEmpty(reason))
            {
                return;
            }

            SetAnchorButton.gameObject.SetActive(false);
            SetAnchorButton.interactable = false;
            // TerrainToggle.gameObject.SetActive(false);
            ClearAllButton.gameObject.SetActive(false);
            ClearAllButton.interactable = false;
            // InfoPanel.SetActive(false);
            SetActiveRecursively(ARGuidance, false);

            Debug.LogError(reason);
            SnackBarText.text = reason;
            _isReturning = true;
            Invoke(nameof(QuitApplication), _errorDisplaySeconds);
        }

        private void QuitApplication()
        {
            Application.Quit();
        }

        private void UpdateDebugInfo()
        {
            if (!Debug.isDebugBuild || EarthManager == null)
            {
                return;
            }

            var pose = EarthManager.EarthState == EarthState.Enabled &&
                EarthManager.EarthTrackingState == TrackingState.Tracking ?
                EarthManager.CameraGeospatialPose : new GeospatialPose();
            var supported = EarthManager.IsGeospatialModeSupported(GeospatialMode.Enabled);
            // DebugText.text =
            //     $"IsReturning: {_isReturning}\n" +
            //     $"IsLocalizing: {_isLocalizing}\n" +
            //     $"SessionState: {ARSession.state}\n" +
            //     $"LocationServiceStatus: {Input.location.status}\n" +
            //     $"FeatureSupported: {supported}\n" +
            //     $"EarthState: {EarthManager.EarthState}\n" +
            //     $"EarthTrackingState: {EarthManager.EarthTrackingState}\n" +
            //     $"  LAT/LNG: {pose.Latitude:F6}, {pose.Longitude:F6}\n" +
            //     $"  HorizontalAcc: {pose.HorizontalAccuracy:F6}\n" +
            //     $"  ALT: {pose.Altitude:F2}\n" +
            //     $"  VerticalAcc: {pose.VerticalAccuracy:F2}\n" +
            //     $". EunRotation: {pose.EunRotation:F2}\n" +
            //     $"  OrientationYawAcc: {pose.OrientationYawAccuracy:F2}";
        }
    }
}