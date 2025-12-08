using UnityEngine;

namespace MRSculpture
{
    public class MaterialSwitcher : MonoBehaviour
    {
        [SerializeField] private Renderer _targetRenderer;

        public void SetMaterial(Material material)
        {
            Material[] matterials = _targetRenderer.materials;
            matterials[0] = material;
            _targetRenderer.materials = matterials;
        }
    }
}
