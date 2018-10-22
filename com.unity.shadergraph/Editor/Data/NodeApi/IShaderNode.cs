namespace UnityEditor.ShaderGraph
{
    public interface IShaderNode
    {
        void Setup(ref NodeSetupContext context);
        void OnChange(NodeChangeContext context);
    }
}
