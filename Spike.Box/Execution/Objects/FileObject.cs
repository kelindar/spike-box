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
    /// <summary>
    /// Represents a file manipulator.
    /// </summary>
    public sealed class FileObject : BaseObject
    {
        #region Constructors
        /// <summary>
        /// Creates a new object.
        /// </summary>
        /// <param name="prototype">The prototype to use.</param>
        public FileObject(ScriptObject prototype)
            : base(prototype)
        {

        }

        /// <summary>
        /// Creates a new object.
        /// </summary>
        /// <param name="prototypeName">The name of the prototype to use.</param>
        public FileObject(ScriptContext context)
            : this(context.GetPrototype("File"))
        {

        }
        #endregion

        #region Exposed Members

        internal static void WriteText(FunctionObject ctx, ScriptObject instance, string fileName, string value)
        {
            File.WriteAllText(fileName, value);
        }

        internal static void WriteJson(FunctionObject ctx, ScriptObject instance, string fileName, BoxedValue value)
        {
            // Check if the passed value is an object
            if (!value.IsStrictlyObject)
                return;

            // Serialize the value as a string
            var serialized = Native.Serialize(instance.Env, value);
            if (!serialized.IsString)
                return;

            // Write to disk
            var textValue = serialized.String.ToString();
            File.WriteAllText(fileName, textValue);
        }


        internal static BoxedValue ReadText(FunctionObject ctx, ScriptObject instance, string fileName)
        {
            // Read from disk
            var text = File.ReadAllText(fileName);
            if (String.IsNullOrEmpty(text))
                return Undefined.Boxed;

            // Return boxed
            return BoxedValue.Box(text);
        }


        internal static BoxedValue ReadJson(FunctionObject ctx, ScriptObject instance, string fileName)
        {
            // Read from disk
            var text = File.ReadAllText(fileName);
            if (String.IsNullOrEmpty(text))
                return Undefined.Boxed;

            // Return deserialized value
            return Native.Deserialize(instance.Env, text);
        }
        #endregion
    }
}