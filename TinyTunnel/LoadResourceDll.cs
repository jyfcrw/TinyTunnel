using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace TinyTunnel
{
    static class LoadResourceDll
    {
        static Dictionary<string, Assembly> Dlls = new Dictionary<string, Assembly>();
        static Dictionary<string, object> Assemblies = new Dictionary<string, object>();

        static Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly ass;
            var assName = new AssemblyName(args.Name).FullName;
            if (Dlls.TryGetValue(assName, out ass) && ass != null)
            {
                Dlls[assName] = null;
                return ass;
            }
            else
            {
                throw new DllNotFoundException(assName);
            }
        }

        public static void RegistDLL()
        {
            var ass = new StackTrace(0).GetFrame(1).GetMethod().Module.Assembly;
            if (Assemblies.ContainsKey(ass.FullName))
            {
                return;
            }

            Assemblies.Add(ass.FullName, null);
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;

            var sharpConfigDll = Assembly.Load(TinyTunnel.Properties.Resources.SharpConfig);
            Dlls[sharpConfigDll.FullName] = sharpConfigDll;
        }
    }
}