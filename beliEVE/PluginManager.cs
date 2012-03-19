using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;

namespace beliEVE
{

    public class PluginManager : IEnumerable<IPlugin>
    {
        public List<Assembly> Assemblies = new List<Assembly>();
        public List<IPlugin> Plugins = new List<IPlugin>();

        public IPlugin Get(string name)
        {
            return Plugins.Where(plug => plug.Name == name).FirstOrDefault();
        }

        public void LoadAll(string baseDir)
        {
            Plugins.Clear();
            Assemblies.Clear();
            var files = Directory.GetFiles(baseDir);
            foreach (var file in files)
            {
                if (file.Contains("beliEVE.dll") || Path.GetExtension(file) != ".dll")
                    continue;
                Load(Path.GetFullPath(file));
            }
        }

        public void Load(string path)
        {
            try
            {
                var assembly = Assembly.LoadFile(path);
                foreach (var type in assembly.GetTypes())
                {
                    if (!type.GetInterfaces().Contains(typeof(IPlugin)))
                        continue;
                    Plugins.Add(assembly.CreateInstance(type.FullName) as IPlugin);
                }
            }
            catch (Exception ex)
            {
                Core.LogWarning("Failed to load plugin " + Path.GetFileName(path) + " (" + ex.GetType().Name + ")");
            }
        }

        public void Initialize(LaunchStage stage)
        {
            foreach (var plugin in Plugins)
                plugin.Initialize(stage);
        }

        public void CreateInterfaces()
        {
            foreach (var plugin in Plugins)
            {
                var type = plugin.Type;
                PluginInterface iface = null;
                switch (type)
                {
                    case PluginType.PacketSource:
                        iface = new PacketSourceHandler(plugin);
                        break;

                    case PluginType.Generic:
                        iface = new GenericHandler(plugin);
                        break;

                    default:
                        Core.Log(LogSeverity.Error, "No interface for type " + type + " and plugin " + plugin.Name);
                        break;
                }

                if (iface != null)
                    plugin.SetInterface(iface);
            }
        }

        public IEnumerator<IPlugin> GetEnumerator()
        {
            return Plugins.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

}