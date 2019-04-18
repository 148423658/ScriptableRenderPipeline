
namespace UnityEngine.Experimental.VoxelizedShadows
{
    [ExecuteAlways]
    [AddComponentMenu("Rendering/VxShadowMaps/PointVxShadowMap", 110)]
    public sealed class PointVxShadowMap : VxShadowMap
    {
        // TODO :
        public override int voxelResolutionInt => (int)VoxelResolution._4096;
        public override VoxelResolution subtreeResolution => VoxelResolution._4096;

        private void OnEnable()
        {
            VxShadowMapsManager.instance.RegisterVxShadowMapComponent(this);
        }
        private void OnDisable()
        {
            VxShadowMapsManager.instance.UnregisterVxShadowMapComponent(this);
        }

        public override bool IsValid()
        {
            return false;
        }
    }
}
