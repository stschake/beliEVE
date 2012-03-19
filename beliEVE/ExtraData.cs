namespace beliEVE
{
    public enum ExtraDataType
    {
        Traceback,
    }

    public class ExtraData
    {
        public ExtraDataType Type { get; private set; }
        public object Data;

        public ExtraData(ExtraDataType type, object data)
        {
            Type = type;
            Data = data;
        }
    }
}