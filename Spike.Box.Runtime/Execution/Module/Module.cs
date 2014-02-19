using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spike.Box
{
    /// <summary>
    /// Module helper.
    /// </summary>
    internal static class Module
    {
        /// <summary>
        /// The list of resolved modules
        /// </summary>
        private static IEnumerable<IModule> Modules = null;

        /// <summary>
        /// Registers all the modules into the script context.
        /// </summary>
        /// <param name="context"></param>
        public static void IncludeIn(ScriptContext context)
        {
            // Inject the modules
            foreach (var module in Module.Resolve())
            {
                try
                {
                    //Print what we are doing
                    Console.WriteLine("Scripting: Importing {0} library ...", module.Name);

                    // Attempt to register
                    module.Register(context);
                }
                catch (Exception ex)
                {
                    // Log the exception
                    Service.Logger.Log(ex);
                }
            }
        }

        /// <summary>
        /// Resolve all the native modules.
        /// </summary>
        /// <returns>All activated modules.</returns>
        public static IEnumerable<IModule> Resolve()
        {
            // Return cached
            if (Modules != null)
                return Modules;

            // Module interface
            var contract = typeof(IModule);
            var modules = new List<IModule>();

            try
            {
                // Check every item for module presence
                var types = Service.MetadataProvider.Types;
                types.ForEach((type) =>
                {
                    try
                    {
                        // Check if the type implements an interface.
                        if (contract.IsAssignableFrom(type) && type.IsClass)
                        {
                            // Check if we have a ctor
                            var ctor = type.GetConstructor(Type.EmptyTypes);
                            if (ctor == null)
                                throw new ArgumentException("The module " + type.Name + " does not provide a parameterless constructor.");

                            // Activate it
                            var module = Activator.CreateInstance(type) as IModule;
                            if (module != null)
                                modules.Add(module);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the exception
                        Service.Logger.Log(ex);
                    }
                });
            }
            catch(Exception ex)
            {
                // Log the exception
                Service.Logger.Log(ex);
            }
            
            // Cache the modules
            Modules = modules;

            // Return the modules
            return modules;
        }
    }
}
