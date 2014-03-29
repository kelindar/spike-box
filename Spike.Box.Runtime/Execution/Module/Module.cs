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
    internal static class Modules
    {
        #region Constructor
        /// <summary>
        /// The list of resolved modules
        /// </summary>
        private static IEnumerable<IModule> Cache = null;


        /// <summary>
        /// Registers all the modules into the script context.
        /// </summary>
        /// <param name="context"></param>
        public static void IncludeIn(ScriptContext context)
        {
            try
            {
                // Set the context to be the current one
                ScriptContext.Current = context;

                // Inject the modules in parallel, as they are no dependancies
                // at the moment between components.
                foreach (var module in Modules.Resolve())
                    context.Import(module.Name, module.Register);
            }
            finally
            {
                // Reset back the context
                ScriptContext.Current = null;
            }
        }

        #endregion


        #region Resove() Method
        /// <summary>
        /// Resolve all the native modules.
        /// </summary>
        /// <returns>All activated modules.</returns>
        public static IEnumerable<IModule> Resolve()
        {
            // Return cached
            if (Cache != null)
                return Cache;

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

            // Make sure native module is the first loaded
            modules = modules
                .OrderBy((m) => m.Name != "native")
                .ToList();
            
            // Cache the modules
            Cache = modules;

            // Return the modules
            return modules;
        }

        #endregion
    }
}
