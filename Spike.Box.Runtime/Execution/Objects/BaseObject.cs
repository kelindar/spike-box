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
        /// Creates a new instance of a base object with the specified type 
        /// name to retrieve and the environment to retrieve from
        /// </summary>
        /// <param name="typeName">The type name to retrieve the prototype.</param>
        /// <param name="context">The context to retrieve the prototype from</param>
        public BaseObject(ScriptContext context, string prototypeName)
            : this(context.GetPrototype(prototypeName)) { }

        /// <summary>
        /// Creates a new instance of a base object with the specified prototype.
        /// </summary>
        /// <param name="prototype">The prototype to attach to the object.</param>
        public BaseObject(ScriptObject prototype)
            : base(prototype.Env, prototype.Env.Maps.Base, prototype)
        {
            
        }

        /// <summary>
        /// Creates a new instance of a base object with the specified type 
        /// name to retrieve and the environment to retrieve from
        /// </summary>
        /// <param name="prototypeName">The type name to retrieve the prototype.</param>
        /// <param name="env">The environment to retrieve the prototype from</param>
        public BaseObject(Spike.Scripting.Runtime.Environment env, string prototypeName)
            : base(env, env.Maps.Base, env.Globals.GetT<ScriptObject>(prototypeName).GetT<ScriptObject>("prototype"))
        {
            
        }


        #endregion



    }


}
