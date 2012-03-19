namespace beliEVE
{

    public abstract class PluginInterface
    {
        public IPlugin Plugin { get; private set; }

        internal PluginInterface(IPlugin plugin)
        {
            Plugin = plugin;
        }
    }

}