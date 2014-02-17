using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spike.Box
{
    /// <summary>
    /// Represents the collection of extensions.
    /// </summary>
    public static class MetaExtension
    {
        public readonly static string Script   = ".js";
        public readonly static string Template = ".ng";

        /// <summary>
        /// Gets whether a meta extension is detected in the name.
        /// </summary>
        /// <param name="name">The file name to check.</param>
        /// <returns>Whether a meta extension is detected in the name</returns>
        public static bool In(string name)
        {
            if (name.EndsWith(MetaExtension.Script))
                return true;

            if (name.EndsWith(MetaExtension.Template))
                return true;

            return false;
        }
    }
}
