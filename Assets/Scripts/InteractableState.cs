using UnityEngine;

namespace Oculus.Interaction
{
    public class InteractableStateController : MonoBehaviour
    {
        [SerializeField]
        private Object _interactableView;

        private IInteractableView _view;

        private void Awake()
        {
            _view = _interactableView as IInteractableView;
        }

        private void OnEnable()
        {
            if (_view != null)
            {
                _view.WhenStateChanged += OnStateChanged;
            }
        }

        private void OnDisable()
        {
            if (_view != null)
            {
                _view.WhenStateChanged -= OnStateChanged;
            }
        }

        private void OnStateChanged(InteractableStateChangeArgs args)
        {
            // 状態変更時の処理が必要ならここに記述
        }

        // 外部から現在の状態を取得するためのメソッド
        public InteractableState GetCurrentState()
        {
            if (_view != null)
            {
                return _view.State;
            }
            else
            {
                return InteractableState.Normal;
            }
        }


    }
}
