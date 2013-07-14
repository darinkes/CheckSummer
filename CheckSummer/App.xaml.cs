using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace CheckSummer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssembly;
        }

        private static Assembly OnResolveAssembly(object sender, ResolveEventArgs e)
        {
            var thisAssembly = Assembly.GetExecutingAssembly();

            // Get the Name of the AssemblyFile
            var assemblyName = new AssemblyName(e.Name);
            var dllName = assemblyName.Name + ".dll";

            System.Diagnostics.Debug.WriteLine("Searching for dll: " + dllName);

            foreach (var resname in thisAssembly.GetManifestResourceNames())
            {
            System.Diagnostics.Debug.WriteLine("dlls: " + resname);
            }

            // Load from Embedded Resources - This function is not called if the Assembly is already
            // in the same folder as the app.
            var resources = thisAssembly.GetManifestResourceNames().Where(s => s.EndsWith(dllName));
            if (resources.Any())
            {

                // 99% of cases will only have one matching item, but if you don't,
                // you will have to change the logic to handle those cases.
                var resourceName = resources.First();
                using (var stream = thisAssembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return null;
                    var block = new byte[stream.Length];

                    // Safely try to load the assembly.
                    try
                    {
                        stream.Read(block, 0, block.Length);
                        System.Diagnostics.Debug.WriteLine("Loaded DLL: " + resourceName);
                        return Assembly.Load(block);
                    }
                    catch (IOException exp)
                    {
                        System.Diagnostics.Debug.WriteLine(exp);
                        return null;
                    }
                    catch (BadImageFormatException exp)
                    {
                        System.Diagnostics.Debug.WriteLine(exp);
                        return null;
                    }
                }
            }

            // in the case the resource doesn't exist, return null.
            System.Diagnostics.Debug.WriteLine("Nothing found");
            return null;
        }
    }
}
