using UnityEngine;
using System.Collections.Generic;

namespace MRSculpture
{
    public class HandToolsController : MonoBehaviour
    {
        [SerializeField] private GameObject leftPokeLocation = null;
        [SerializeField] private GameObject rightPokeLocation = null;
        [SerializeField] private GameObject chiselPrefab = null;
        private GameObject chiselInstance = null;
        private Vector3 chiselPosition = Vector3.zero;
        private Quaternion chiselRotation = Quaternion.identity;
        [SerializeField] private GameObject hammerPrefab = null;
        private GameObject hammerInstance = null;
        private Vector3 hammerPosition = Vector3.zero;
        private Quaternion hammerRotation = Quaternion.identity;

        void Start()
        {
            chiselPosition = chiselPrefab.transform.position;
            chiselRotation = chiselPrefab.transform.rotation;
            chiselInstance = Instantiate(chiselPrefab, chiselPosition, chiselRotation);

            hammerPosition = hammerPrefab.transform.position;
            hammerRotation = hammerPrefab.transform.rotation;
            hammerInstance = Instantiate(hammerPrefab, hammerPosition, hammerRotation);
        }

        void Update()
        {
        }
    }
}
