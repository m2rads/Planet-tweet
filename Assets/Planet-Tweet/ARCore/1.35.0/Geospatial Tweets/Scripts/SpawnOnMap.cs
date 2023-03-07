namespace Mapbox.Examples
{
	using UnityEngine;
	using Mapbox.Utils;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.Utilities;
	using System.Collections.Generic;

	public class SpawnOnMap : MonoBehaviour
	{
		[SerializeField]
		AbstractMap _map;

		[SerializeField]
		[Geocode]
		string[] _locationStrings;
		Vector2d[] _locations;

		[SerializeField]
		float _spawnScale = 100f;

		[SerializeField]
		GameObject _markerPrefab;

		List<GameObject> _spawnedObjects;

		public Vector2d[] tweetCoordinates;

		void Start()
		{
			// _locations = new Vector2d[_locationStrings.Length];
			// for (int i = 0; i < _locationStrings.Length; i++)
			// {
			// 	var locationString = _locationStrings[i];
			// 	Debug.Log(locationString);
			// 	_locations[i] = Conversions.StringToLatLon(locationString);
			// 	Debug.Log(_locations[i]);
			// 	var instance = Instantiate(_markerPrefab);
			// 	instance.transform.localPosition = _map.GeoToWorldPosition(_locations[i], true);
			// 	instance.transform.localScale = new Vector3(_spawnScale, _spawnScale, _spawnScale);
			// 	_spawnedObjects.Add(instance);
			// }


			tweetCoordinates = new Vector2d[0];
			_spawnedObjects = new List<GameObject>();

			for (int i = 0; i < tweetCoordinates.Length; i++)
			{
				var instance = Instantiate(_markerPrefab);
				instance.transform.localPosition = _map.GeoToWorldPosition(tweetCoordinates[i], true);
				instance.transform.localScale = new Vector3(_spawnScale, _spawnScale, _spawnScale);
				_spawnedObjects.Add(instance);
			}
			
		}

		private void Update()
		{
			int count = _spawnedObjects.Count;
			for (int i = 0; i < count; i++)
			{
				var spawnedObject = _spawnedObjects[i];
				var tweetCoordinate = tweetCoordinates[i];
				spawnedObject.transform.localPosition = _map.GeoToWorldPosition(tweetCoordinate, true);
				spawnedObject.transform.localScale = new Vector3(_spawnScale, _spawnScale, _spawnScale);
			}
		}
	}
}