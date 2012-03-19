namespace beliEVE
{

    public interface IPlugin
    {
        string Name { get; }
        string ShortName { get; }
        string Description { get; }
        int Revision { get; }
        PluginType Type { get; }

        void SetInterface(PluginInterface iface);
        bool Initialize(LaunchStage stage);
        void ProcessCommand(string command);
    }

}