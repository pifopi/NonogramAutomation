namespace NonogramAutomation
{
    public class FoundElement
    {
        public int Index { get; }
        public string Query { get; }
        public AdvancedSharpAdbClient.DeviceCommands.Models.Element Element { get; }
        public FoundElement(int index, string query, AdvancedSharpAdbClient.DeviceCommands.Models.Element element)
        {
            Index = index;
            Query = query;
            Element = element;
        }
    }
}
