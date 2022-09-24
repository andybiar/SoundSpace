using System;
using System.Collections;
using System.Collections.Generic;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.WayspotAnchors;
using Niantic.ARDK.Extensions;
using Niantic.ARDK.LocationService;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDKExamples.RemoteContent
{
	public class PayloadSpawner : MonoBehaviour
	{
		[SerializeField]
		private Text _vpsStatusText;
		
		[SerializeField]
		private Text _wayspotStatusText;

		public string[] PayloadStrings;
		public GameObject Prefab;
		
		private IARSession _arSession;
		private ILocationService _locationService;
		private WayspotAnchorService _waService;

		void Start()
		{
			ARSessionFactory.SessionInitialized += OnARSessionInitialized;
		}

		private void OnDestroy()
		{
			ARSessionFactory.SessionInitialized -= OnARSessionInitialized;
		}

		private void OnARSessionInitialized(AnyARSessionInitializedArgs args)
		{
			_arSession = args.Session;
			_locationService = LocationServiceFactory.Create();
			_locationService.Start();

			var config = WayspotAnchorsConfigurationFactory.Create();
			_waService = new WayspotAnchorService(_arSession, _locationService, config);

			_waService.LocalizationStateUpdated += OnLocalizationStateUpdated;
		}

		private void OnLocalizationStateUpdated(LocalizationStateUpdatedArgs args)
		{
			_vpsStatusText.text = "LocalizationState: " + args.State;
			if (args.State == LocalizationState.Failed)
			{
				Debug.Log(args.FailureReason);
				_waService.Restart();
				return;
			}
			
			if (args.State == LocalizationState.Localized)
			{
				int count = 0;
				
				WayspotAnchorPayload[] payloads = new WayspotAnchorPayload[PayloadStrings.Length];

				for (int index = 0; index < PayloadStrings.Length; index++)
				{
					payloads[index] = WayspotAnchorPayload.Deserialize(PayloadStrings[index]);
				}

				IWayspotAnchor[] wayspotAnchors = _waService.RestoreWayspotAnchors(payloads);

				_wayspotStatusText.text = "WA resolved " + count + " of " + PayloadStrings.Length;

				foreach (var anchor in wayspotAnchors)
				{
					anchor.StatusCodeUpdated += update =>
					{
						if (update.Code == WayspotAnchorStatusCode.Success)
						{
							count++;
							_wayspotStatusText.text = "WA resolved " + count + " of " + PayloadStrings.Length;
							
							var go = Instantiate(Prefab);

							
							var tracker = go.GetComponent<WayspotAnchorTracker>();
							if (tracker == null)
							{
								Debug.Log("Anchor prefab was missing WayspotAnchorTracker, so one will be added.");
								tracker = go.AddComponent<WayspotAnchorTracker>();
							}

							tracker.AttachAnchor(anchor);
						}
					};
				}
			}
		}
	}
}