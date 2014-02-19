using System;
using System.Linq;
using Spike.Scripting;
using Spike.Scripting.Runtime;
using Spike.Scripting.Compiler;
using Spike.Scripting.Hosting;
using System.Collections.Generic;
using System.Text;

namespace Spike.Box
{

    /// <summary>
    /// Represents the modifier of a member.
    /// </summary>
    internal enum SurrogateMemberModifier
    {
        /// <summary>
        /// Defines that the proxy member can be accessed privately only.
        /// </summary>
        Private,

        /// <summary>
        /// Defines that the proxy member can be accessed publicly.
        /// </summary>
        Public
    }

}
