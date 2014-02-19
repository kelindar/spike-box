using System;
using System.Linq;
using Spike.Network;
using Spike.Scripting;
using Spike.Scripting.Runtime;
using Spike.Scripting.Compiler;
using Spike.Scripting.Hosting;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.IO;
using Spike.Scripting.Native;


namespace Spike.Box
{
    /// <summary>
    /// Represents an <see cref="IClient"/> reference object.
    /// </summary>
    public class ClientObject : ReferenceObject<IClient>
    {
        #region Constructors

        /// <summary>
        /// Creates a new application object that servers as a proxy for
        /// using .Net framework functionality.
        /// </summary>
        /// <param name="prototype">The prototype to fetch the environment from.</param>
        /// <param name="target">The target to wrap.</param>
        public ClientObject(ScriptObject prototype, IClient target)
            : base(prototype, target)
        {

        }

        /// <summary>
        /// Creates a new application object that servers as a proxy for
        /// using .Net framework functionality.
        /// </summary>
        /// <param name="environment">The scripting environment to use.</param>
        /// <param name="target">The target to wrap.</param>
        public ClientObject(Spike.Scripting.Runtime.Environment environment, IClient target)
            : base(environment, target)
        {

        }

        /// <summary>
        /// Creates a new application object that servers as a proxy for
        /// using .Net framework functionality.
        /// </summary>
        /// <param name="context">The scripting context to use.</param>
        /// <param name="target">The target to wrap.</param>
        public ClientObject(ScriptContext context, IClient target)
            : base(context, target)
        {

        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the class name for building the javascript name.
        /// </summary>
        public override string ClassName
        {
            get { return "Client"; }
        }
        #endregion

    }

}


