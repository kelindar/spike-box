using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spike.Scripting.Runtime
{
    /// <summary>
    /// Represents a script object.
    /// </summary>
    public partial class ScriptObject
    {
    
        /// <summary>
        /// Occurs when the object is disposing.
        /// </summary>
        public void Dispose()
        {
            this.OnDispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Occurs when a object is finalizing.
        /// </summary>
        ~ScriptObject()
        {
            this.OnDispose(false);
        }

        /// <summary>
        /// Occurs when the object is disposing.
        /// </summary>
        protected virtual void OnDispose(bool disposing)
        {
            try
            {
                // Invoke the ignored event
                if (ScriptObject.Ignored != null)
                    ScriptObject.Ignored(this);
            }
            catch { }
        }

    }



}
