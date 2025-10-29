using UnityEngine;

namespace MRSculpture
{
    public class HandDeviceController : MonoBehaviour
    {
        [SerializeField] private GameObject _controllerLeft;
        [SerializeField] private GameObject _controllerRight;
        private SkinnedMeshRenderer _controllerVisual;
        private OVRInput.Controller _controllerType;

        void Awake()
        {
            _controllerType = gameObject.GetComponent<OVRControllerHelper>().m_controller;
            if (_controllerType == OVRInput.Controller.LTouch)
            {
                _controllerVisual = _controllerLeft.GetComponent<SkinnedMeshRenderer>();
            }
            else if (_controllerType == OVRInput.Controller.RTouch)
            {
                _controllerVisual = _controllerRight.GetComponent<SkinnedMeshRenderer>();
            }
        }

        void Update()
        {
            if (_controllerType == OVRInput.Controller.LTouch)
            {
                if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger)) DownLeftHandTrigger();
                if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger)) UpLeftHandTrigger();
            }
            else if (_controllerType == OVRInput.Controller.RTouch)
            {
                if (OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger)) DownRightHandTrigger();
                if (OVRInput.GetUp(OVRInput.Button.SecondaryHandTrigger)) UpRightHandTrigger();
            }
        }

        private void DownLeftHandTrigger()
        {
            _controllerVisual.enabled = false;
        }

        private void UpLeftHandTrigger()
        {
            _controllerVisual.enabled = true;
        }

        private void DownRightHandTrigger()
        {
            _controllerVisual.enabled = false;
        }

        private void UpRightHandTrigger()
        {
            _controllerVisual.enabled = true;
        }
    }
}
