using System;
using System.Linq;
using Spike.Scripting;
using Spike.Scripting.Runtime;
using Spike.Scripting.Compiler;
using Spike.Scripting.Hosting;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.IO;

namespace Spike.Box
{
    public abstract class BaseObject : ScriptObject
    {
        #region Constructors
        /// <summary>
        /// Creates a new container that servers as an application container for a scope.
        /// </summary>
        /// <param name="prototypeName">The name of the prototype to use.</param>
        public BaseObject(ScriptContext context, string prototypeName)
            : this(context.GetPrototype(prototypeName)) { }

        /// <summary>
        /// Creates a new application object that servers as a proxy for
        /// using .Net framework functionality.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public BaseObject(ScriptObject prototype)
            : base(prototype.Env, prototype.Env.Maps.Base, prototype)
        {
            
        }
        #endregion



    }


}
