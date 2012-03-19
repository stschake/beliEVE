namespace beliEVE
{
    public class PacketSourceHandler : PluginInterface
    {
        public PacketSourceHandler(IPlugin plugin) 
            : base(plugin)
        {
            
        }

        public void Report(Packet packet)
        {
            Core.Pipeline.Pipe(packet);
        }
    }

}