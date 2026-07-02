namespace AtlasXR.XR.Highlighting
{
    public interface IHighlightService
    {
        void Register(EquipmentComponent component);

        void Unregister(EquipmentComponent component);

        bool HighlightComponent(string componentId);

        void ClearHighlight();
    }
}
