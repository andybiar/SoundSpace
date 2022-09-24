using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Niantic.ARDK.AR;
using Niantic.ARDK.Utilities.Input.Legacy;
using Niantic.ARDK.AR.HitTest;
using Niantic.ARDK.Extensions;
using Niantic.ARDK.AR.WayspotAnchors;
using Niantic.ARDK.LocationService;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.Utilities;
using UnityEngine.UI;

public class WorkshopWayspotAnchorController : MonoBehaviour
{
    public Camera _camera; // the ARDK's AR camera instead of the default Unity camera
    public List<GameObject> _objectPrefabs; // the prefab we will be spawning on screen
    public Text _statusLog; // updates the status log for Wayspot Anchors on screen
    public Text _localizationStatus; // updates the localization status message on screen

    string LocalSaveKey = "my_wayspots"; // key used to store anchors locally
    IARSession _arSession; // the AR session started by ARDK
    WayspotAnchorService _wayspotAnchorService; // controls VPS features used

    int prefabCounter;

    void OnEnable()
    {
        ARSessionFactory.SessionInitialized += OnSessionInitialized;
    }

    // Listen for touch events only if the app has localized to a VPS Wayspot
    void Update()
    {
        if (_wayspotAnchorService == null ||
            _wayspotAnchorService.LocalizationState != LocalizationState.Localized) return;
        
        if (PlatformAgnosticInput.touchCount <= 0) return;
        var touch = PlatformAgnosticInput.GetTouch(0);
        if (touch.IsTouchOverUIObject()) return;
        
        if (touch.phase == TouchPhase.Began)
        {
            OnTouchScreen(touch);
        }
    }

    void OnDisable()
    {
        ARSessionFactory.SessionInitialized -= OnSessionInitialized;
    }

    #region Wayspot Anchor Methods

    void PlaceAnchor(Matrix4x4 poseData)
    {
        var anchors = _wayspotAnchorService.CreateWayspotAnchors(poseData);
        if (anchors.Length == 0) return;

        var position = poseData.ToPosition();
        var rotation = poseData.ToRotation();
        CreateWayspotAnchorGameObject(anchors[0], position, rotation);
        _statusLog.text = "Anchor placed.";
    }

    void CreateWayspotAnchorGameObject(IWayspotAnchor anchor, Vector3 position, Quaternion rotation)
    {
        if (prefabCounter >= _objectPrefabs.Count)
            return;
        
        var go = Instantiate(_objectPrefabs[prefabCounter], position, rotation);
        prefabCounter++;

        var tracker = go.GetComponent<WayspotAnchorTracker>();
        if (tracker == null)
        {
            tracker = go.AddComponent<WayspotAnchorTracker>();
            tracker.AttachAnchor(anchor);
        }
    }

    #endregion

    #region ARDK Event Handlers

    void OnSessionInitialized(AnyARSessionInitializedArgs args)
    {
        _statusLog.text = "Session initialized";
        if (_arSession != null) return;
        _arSession = args.Session;
        _arSession.Ran += OnSessionRan;
    }

    // Once the session has run, we will need to create the wayspot anchor service
    void OnSessionRan(ARSessionRanArgs args)
    {
        _arSession.Ran -= OnSessionRan;
        var wayspotAnchorsConfiguration = WayspotAnchorsConfigurationFactory.Create();
        var locationService = LocationServiceFactory.Create(_arSession.RuntimeEnvironment);
        locationService.Start();
        _wayspotAnchorService = new WayspotAnchorService(_arSession, locationService, wayspotAnchorsConfiguration);
        _wayspotAnchorService.LocalizationStateUpdated += OnLocalizationStateUpdated;
        _statusLog.text = "Session running";
    }

    void OnTouchScreen(Touch touch)
    {
        var currentFrame = _arSession.CurrentFrame;
        if (currentFrame == null) return;
        if (prefabCounter >= _objectPrefabs.Count) return;
        
        var hitTestResults = currentFrame.HitTest(_camera.pixelWidth, _camera.pixelHeight, touch.position,
            ARHitTestResultType.EstimatedHorizontalPlane);
        if (hitTestResults.Count <= 0) return;
        var position = hitTestResults[0].WorldTransform.ToPosition();
        var rotation = Quaternion.Euler(new Vector3(0, UnityEngine.Random.Range(0, 360)));
        Matrix4x4 poseData = Matrix4x4.TRS(position, rotation, _objectPrefabs[prefabCounter].transform.localScale);
        PlaceAnchor(poseData);
    }

    void OnLocalizationStateUpdated(LocalizationStateUpdatedArgs args)
    {
        _localizationStatus.text = args.State.ToString();
        if (args.State == LocalizationState.Failed)
        {
            _statusLog.text = args.FailureReason.ToString();
        }
    }

    #endregion

    #region Button Handlers

    // Gather all of the wayspot anchors and save them on device
    public void SaveLocalWayspotAnchors()
    {
        IWayspotAnchor[] wayspotAnchors = _wayspotAnchorService.GetAllWayspotAnchors();
        MyStoredAnchorsData storedAnchorData = new MyStoredAnchorsData();
        storedAnchorData.Payloads = wayspotAnchors.Select(a => a.Payload.Serialize()).ToArray(); // lookup => notation
        string jsonData = JsonUtility.ToJson(storedAnchorData);
        PlayerPrefs.SetString(LocalSaveKey, jsonData);
        _statusLog.text = $"Saved {wayspotAnchors.Count()} anchors";
    }

    // Using the player key, check if there are stored wayspot anchors on device. Restore them if true.
    public void LoadLocalWayspotAnchors()
    {
        if (PlayerPrefs.HasKey(LocalSaveKey))
        {
            string json = PlayerPrefs.GetString(LocalSaveKey);
            MyStoredAnchorsData storedAnchorsData = JsonUtility.FromJson<MyStoredAnchorsData>(json);
        
            foreach (var wayspotAnchorPayload in storedAnchorsData.Payloads)
            {
                var payload = WayspotAnchorPayload.Deserialize(wayspotAnchorPayload);
        
                var anchors = _wayspotAnchorService.RestoreWayspotAnchors(payload);
                if (anchors.Length == 0)
                    return;
        
                var position = anchors[0].LastKnownPosition;
                var rotation = anchors[0].LastKnownRotation;
                CreateWayspotAnchorGameObject(anchors[0], position, rotation);
            }
        
            _statusLog.text = $"Restored {storedAnchorsData.Payloads.Count()} anchors";
        }
        else
        {
            _statusLog.text = "No key found";
        }
    }

    #endregion

    [Serializable]
    class MyStoredAnchorsData
    {
        public string[] Payloads = Array.Empty<string>();
    }
}
