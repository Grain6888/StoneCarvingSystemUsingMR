using UnityEngine;
using System.Collections.Generic;

namespace MRSculpture
{
    public class HandToolsController : MonoBehaviour
    {
        [SerializeField] private GameObject leftPokeLocation = null;
        [SerializeField] private GameObject rightPokeLocation = null;
        [SerializeField] private List<GameObject> chiselPrefabs = null;
        private GameObject chiselInstance = null;
        private Vector3 chiselPosition = Vector3.zero;
        private Quaternion chiselRotation = Quaternion.identity;
        [SerializeField] private GameObject hammerPrefab = null;
        private GameObject hammerInstance = null;
        private Vector3 hammerPosition = Vector3.zero;
        private Quaternion hammerRotation = Quaternion.identity;

        void Start()
        {
            chiselPosition = chiselPrefabs[0].transform.position;
            chiselRotation = chiselPrefabs[0].transform.rotation;
            chiselInstance = Instantiate(chiselPrefabs[0], leftPokeLocation.transform.position + chiselPosition, leftPokeLocation.transform.rotation * chiselRotation);

            hammerPosition = hammerPrefab.transform.position;
            hammerRotation = hammerPrefab.transform.rotation;
            hammerInstance = Instantiate(hammerPrefab, rightPokeLocation.transform.position + hammerPosition, rightPokeLocation.transform.rotation * hammerRotation);
        }

        void Update()
        {
            chiselInstance.transform.SetPositionAndRotation(leftPokeLocation.transform.position + chiselPosition, leftPokeLocation.transform.rotation * chiselRotation);
            hammerInstance.transform.SetPositionAndRotation(rightPokeLocation.transform.position + hammerPosition, rightPokeLocation.transform.rotation * hammerRotation);
        }
    }
}
