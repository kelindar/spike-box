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
using Spike.Text;
using Spike.Text.Json;
using System.Threading;
using System.Threading.Tasks;


namespace Spike.Box
{
    /// <summary>
    /// Represents a provider that connects with the SWARM model.
    /// </summary>
    public class FileObject : BaseObject
    {
        #region Constructors
        public const string TypeName = "File";

        /// <summary>
        /// Creates a new instance of an object.
        /// </summary>
        /// <param name="prototype">The prototype to use.</param>
        public FileObject(ScriptObject prototype)
            : base(prototype)
        {
            
        }
        /// <summary>
        /// Creates a new instance of an object.
        /// </summary>
        /// <param name="prototypeName">The name of the prototype to use.</param>
        public FileObject(ScriptContext context)
            : this(context.GetPrototype(TypeName))
        {

        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the class name for building the javascript name.
        /// </summary>
        public override string ClassName
        {
            get { return TypeName; }
        }
        #endregion

        /// <summary>
        /// Appends lines to a file by using a specified encoding, and then closes the file. If the specified file does not exist, this method creates a file, writes the specified lines to the file, and then closes the file.
        /// </summary>
        internal static void AppendLines(FunctionObject ctx, ScriptObject instance, BoxedValue path, BoxedValue contents, BoxedValue onComplete, BoxedValue encodingName)
        {
            if(!path.IsString)
                throw new ArgumentException("[appendLines] First parameter should be defined and be a string.");
            if (!contents.IsArray)
                throw new ArgumentException("[appendLines] Second parameter should be defined and be an array.");
            if (!onComplete.IsUndefined && !onComplete.IsFunction)
                throw new ArgumentException("[appendLines] Third parameter should be an onComplete function.");

            // Get the curent channel
            var channel = Channel.Current;

            // Dispatch the task
            channel.Async(() =>
            {
                // The encoding to use
                Encoding encoding = encodingName.IsString
                    ? TextEncoding.GetEncoding(encodingName.String)
                    : null;

                // Defaults to UTF8
                if (encoding == null)
                    encoding = TextEncoding.UTF8;
            
                // Unbox the array of lines and execute the append
                File.AppendAllLines(
                    path.String,
                    contents.Array.ToArray<string>(),
                    encoding
                    );

                // Dispatch the on complete asynchronously
                channel.DispatchCallback(onComplete, instance);
            });
        }

        /// <summary>
        /// Opens a file, appends the specified string to the file, and then closes the file. If the file does not exist, this method creates a file, writes the specified string to the file, then closes the file.
        /// </summary>
        internal static void AppendText(FunctionObject ctx, ScriptObject instance, BoxedValue path, BoxedValue contents, BoxedValue onComplete, BoxedValue encodingName)
        {
            if (!path.IsString)
                throw new ArgumentException("[appendText] First parameter should be defined and be a string.");
            if (!contents.IsString)
                throw new ArgumentException("[appendText] Second parameter should be defined and be a string.");
            if (!onComplete.IsUndefined && !onComplete.IsFunction)
                throw new ArgumentException("[appendText] Third parameter should be an onComplete function.");

            // Get the curent channel
            var channel = Channel.Current;

            // Dispatch the task
            channel.Async(() =>
            {
                // The encoding to use
                Encoding encoding = encodingName.IsString
                    ? TextEncoding.GetEncoding(encodingName.String)
                    : null;

                // Defaults to UTF8
                if (encoding == null)
                    encoding = TextEncoding.UTF8;

                // Unbox the array of lines and execute the append
                File.AppendAllText(
                    path.String,
                    contents.String,
                    encoding
                    );

                // Dispatch the on complete asynchronously
                channel.DispatchCallback(onComplete, instance);
            });
        }

        /// <summary>
        /// Opens a file, appends the specified string to the file, and then closes the file. If the file does not exist, this method creates a file, writes the specified string to the file, then closes the file.
        /// </summary>
        internal static void AppendJson(FunctionObject ctx, ScriptObject instance, BoxedValue path, BoxedValue contents, BoxedValue onComplete, BoxedValue encodingName)
        {
            if (!path.IsString)
                throw new ArgumentException("[appendJson] First parameter should be defined and be a string.");
            if (!contents.IsStrictlyObject)
                throw new ArgumentException("[appendJson] Second parameter should be defined and be an object.");
            if (!onComplete.IsUndefined && !onComplete.IsFunction)
                throw new ArgumentException("[appendJson] Third parameter should be an onComplete function.");

            // Get the curent channel
            var channel = Channel.Current;

            // Dispatch the task
            channel.Async(() =>
            {
                // The encoding to use
                Encoding encoding = encodingName.IsString
                    ? TextEncoding.GetEncoding(encodingName.String)
                    : null;

                // Defaults to UTF8
                if (encoding == null)
                    encoding = TextEncoding.UTF8;

                // Unbox the array of lines and execute the append
                File.AppendAllText(
                    path.String,
                    JsonConvert.SerializeObject(contents.Object),
                    encoding
                    );

                // Dispatch the on complete asynchronously
                channel.DispatchCallback(onComplete, instance);
            });
        }

        /// <summary>
        /// Creates a new file by using the specified encoding, writes a collection of strings to the file, and then closes the file.
        /// </summary>
        internal static void WriteLines(FunctionObject ctx, ScriptObject instance, BoxedValue path, BoxedValue contents, BoxedValue onComplete, BoxedValue encodingName)
        {
            if (!path.IsString)
                throw new ArgumentException("[writeLines] TFirst parameter should be defined and be a string.");
            if (!contents.IsArray)
                throw new ArgumentException("[writeLines] TSecond parameter should be defined and be an array.");
            if (!onComplete.IsUndefined && !onComplete.IsFunction)
                throw new ArgumentException("[writeLines] Third parameter should be an onComplete function.");

            // Get the curent channel
            var channel = Channel.Current;

            // Dispatch the task
            channel.Async(() =>
            {
                // The encoding to use
                Encoding encoding = encodingName.IsString
                    ? TextEncoding.GetEncoding(encodingName.String)
                    : null;

                // Defaults to UTF8
                if (encoding == null)
                    encoding = TextEncoding.UTF8;

                // Unbox the array of lines and execute the append
                File.WriteAllLines(
                    path.String,
                    contents.Array.ToArray<string>(),
                    encoding
                    );

                // Dispatch the on complete asynchronously
                channel.DispatchCallback(onComplete, instance);
            });
        }

        /// <summary>
        /// Creates a new file, writes the specified string to the file using the specified encoding, and then closes the file. If the target file already exists, it is overwritten.
        /// </summary>
        internal static void WriteText(FunctionObject ctx, ScriptObject instance, BoxedValue path, BoxedValue contents, BoxedValue onComplete, BoxedValue encodingName)
        {
            if (!path.IsString)
                throw new ArgumentException("[writeText] First parameter should be defined and be a string.");
            if (!contents.IsString)
                throw new ArgumentException("[writeText] Second parameter should be defined and be a string.");
            if (!onComplete.IsUndefined && !onComplete.IsFunction)
                throw new ArgumentException("[writeText] Third parameter should be an onComplete function.");

            // Get the curent channel
            var channel = Channel.Current;

            // Dispatch the task
            channel.Async(() =>
            {
                // The encoding to use
                Encoding encoding = encodingName.IsString
                    ? TextEncoding.GetEncoding(encodingName.String)
                    : null;

                // Defaults to UTF8
                if (encoding == null)
                    encoding = TextEncoding.UTF8;

                // Unbox the array of lines and execute the append
                File.WriteAllText(
                    path.String,
                    contents.String,
                    encoding
                    );

                // Dispatch the on complete asynchronously
                channel.DispatchCallback(onComplete, instance);
            });
        }

        /// <summary>
        /// Creates a new file, writes the specified string to the file using the specified encoding, and then closes the file. If the target file already exists, it is overwritten.
        /// </summary>
        internal static void WriteJson(FunctionObject ctx, ScriptObject instance, BoxedValue path, BoxedValue contents, BoxedValue onComplete, BoxedValue encodingName)
        {
            if (!path.IsString)
                throw new ArgumentException("[writeJson] First parameter should be defined and be a string.");
            if (!contents.IsStrictlyObject)
                throw new ArgumentException("[writeJson] Second parameter should be defined and be an object.");
            if (!onComplete.IsUndefined && !onComplete.IsFunction)
                throw new ArgumentException("[writeJson] Third parameter should be an onComplete function.");

            // Get the curent channel
            var channel = Channel.Current;

            // Dispatch the task
            channel.Async(() =>
            {
                // The encoding to use
                Encoding encoding = encodingName.IsString
                    ? TextEncoding.GetEncoding(encodingName.String)
                    : null;

                // Defaults to UTF8
                if (encoding == null)
                    encoding = TextEncoding.UTF8;

                // Unbox the array of lines and execute the append
                File.WriteAllText(
                    path.String,
                    JsonConvert.SerializeObject(contents.Object),
                    encoding
                    );

                // Dispatch the on complete asynchronously
                channel.DispatchCallback(onComplete, instance);
            });
        }


        /// <summary>
        /// Opens a file, reads all lines of the file with the specified encoding, and then closes the file.
        /// </summary>
        internal static void ReadLines(FunctionObject ctx, ScriptObject instance, BoxedValue path, BoxedValue onComplete, BoxedValue encodingName)
        {
            if (!path.IsString)
                throw new ArgumentException("[readLines] First parameter should be defined and be a string.");
            if (!onComplete.IsFunction)
                throw new ArgumentException("[readLines] Second parameter should be an onComplete function.");

            // Get the curent channel
            var channel = Channel.Current;

            // Dispatch the task
            channel.Async(() =>
            {
                // The encoding to use
                Encoding encoding = encodingName.IsString
                    ? TextEncoding.GetEncoding(encodingName.String)
                    : null;

                // Defaults to UTF8
                if (encoding == null)
                    encoding = TextEncoding.UTF8;

                // Unbox the array of lines and execute the append
                var lines = File.ReadAllLines(
                    path.String,
                    encoding
                    );

                // Create a new array
                var array = new ArrayObject(instance.Env, (uint)lines.Length);
                for (uint i = 0; i < lines.Length; ++i)
                {
                    // Put a boxed string inside for each line
                    array.Put(i, BoxedValue.Box(lines[i]));
                }

                // Dispatch the on complete asynchronously
                channel.DispatchCallback(onComplete, instance, BoxedValue.Box(array));

            });
        }


        /// <summary>
        /// Opens a file, reads all lines of the file with the specified encoding, and then closes the file.
        /// </summary>
        internal static void ReadText(FunctionObject ctx, ScriptObject instance, BoxedValue path, BoxedValue onComplete, BoxedValue encodingName)
        {
            if (!path.IsString)
                throw new ArgumentException("[readText] First parameter should be defined and be a string.");
            if (!onComplete.IsFunction)
                throw new ArgumentException("[readText] Second parameter should be an onComplete function.");

            // Get the curent channel
            var channel = Channel.Current;

            // Dispatch the task
            channel.Async(() =>
            {
                // The encoding to use
                Encoding encoding = encodingName.IsString
                    ? TextEncoding.GetEncoding(encodingName.String)
                    : null;

                // Defaults to UTF8
                if (encoding == null)
                    encoding = TextEncoding.UTF8;

                // Read the text
                var text = BoxedValue.Box(
                        File.ReadAllText(path.String, encoding)
                    );

                // Dispatch the on complete asynchronously
                channel.DispatchCallback(onComplete, instance, text);
            });
        }

        /// <summary>
        /// Opens a file, reads all lines of the file with the specified encoding, and then closes the file.
        /// </summary>
        internal static void ReadJson(FunctionObject ctx, ScriptObject instance, BoxedValue path, BoxedValue onComplete, BoxedValue encodingName)
        {
            if (!path.IsString)
                throw new ArgumentException("[readJson] First parameter should be defined and be a string.");
            if (!onComplete.IsFunction)
                throw new ArgumentException("[readJson] Second parameter should be an onComplete function.");

            // Get the curent channel
            var channel = Channel.Current;

            // Dispatch the task
            channel.Async(() =>
            {
                // The encoding to use
                Encoding encoding = encodingName.IsString
                    ? TextEncoding.GetEncoding(encodingName.String)
                    : null;

                // Defaults to UTF8
                if (encoding == null)
                    encoding = TextEncoding.UTF8;

                // Deserialize json
                var json = Native.Deserialize(
                    instance.Env,
                    File.ReadAllText(path.String, encoding)
                    );

                // Dispatch the on complete asynchronously
                channel.DispatchCallback(onComplete, instance, json);

            });
        }

        /// <summary>
        /// Copies an existing file to a new file. Overwriting a file of the same name is allowed.
        /// </summary>
        internal static void Copy(FunctionObject ctx, ScriptObject instance, BoxedValue sourceFileName, BoxedValue destFileName, BoxedValue onComplete, BoxedValue overwrite)
        {
            if (!sourceFileName.IsString)
                throw new ArgumentException("[copy] First parameter should be defined and be a string.");
            if (!destFileName.IsString)
                throw new ArgumentException("[copy] Second parameter should be defined and be a string.");
            if (!onComplete.IsUndefined && !onComplete.IsFunction)
                throw new ArgumentException("[copy] Third parameter should be an onComplete function.");

            // The encoding to use
            bool replace = overwrite.IsBoolean
                ? overwrite.Bool
                : false;

            // Get the curent channel
            var channel = Channel.Current;

            // Dispatch the task
            channel.Async(() =>
            {
                // Copy the file
                File.Copy(sourceFileName.String, destFileName.String, replace);

                // Dispatch the on complete asynchronously
                channel.DispatchCallback(onComplete, instance);
            });
        }

        /// <summary>
        /// Replaces an existing file with a new file.
        /// </summary>
        internal static void Replace(FunctionObject ctx, ScriptObject instance, BoxedValue sourceFileName, BoxedValue destFileName, BoxedValue onComplete)
        {
            if (!sourceFileName.IsString)
                throw new ArgumentException("[replace] First parameter should be defined and be a string.");
            if (!destFileName.IsString)
                throw new ArgumentException("[replace] Second parameter should be defined and be a string.");
            if (!onComplete.IsUndefined && !onComplete.IsFunction)
                throw new ArgumentException("[replace] Third parameter should be an onComplete function.");

            // Get the curent channel
            var channel = Channel.Current;

            // Dispatch the task
            channel.Async(() =>
            {
                // Copy the file
                if (File.Exists(destFileName.String))
                    File.Copy(sourceFileName.String, destFileName.String, true);

                // Dispatch the on complete asynchronously
                channel.DispatchCallback(onComplete, instance);
            });
        }

        /// <summary>
        /// Moves a specified file to a new location, providing the option to specify a new file name.
        /// </summary>
        internal static void Move(FunctionObject ctx, ScriptObject instance, BoxedValue sourceFileName, BoxedValue destFileName, BoxedValue onComplete)
        {
            if (!sourceFileName.IsString)
                throw new ArgumentException("[move] First parameter should be defined and be a string.");
            if (!destFileName.IsString)
                throw new ArgumentException("[move] Second parameter should be defined and be a string.");
            if (!onComplete.IsUndefined && !onComplete.IsFunction)
                throw new ArgumentException("[move] Third parameter should be an onComplete function.");

            // Get the curent channel
            var channel = Channel.Current;

            // Dispatch the task
            channel.Async(() =>
            {
                // move the file
                File.Move(sourceFileName.String, destFileName.String);

                // Dispatch the on complete asynchronously
                channel.DispatchCallback(onComplete, instance);
            });
        }

        /// <summary>
        /// Determines whether the specified file exists.
        /// </summary>
        internal static BoxedValue Exists(FunctionObject ctx, ScriptObject instance, BoxedValue path)
        {
            if (!path.IsString)
                throw new ArgumentException("[exists] First parameter should be defined and be a string.");

            // Check the file
            return BoxedValue.Box(
                File.Exists(path.String)
                );
        }

        /// <summary>
        /// Deletes the specified file.
        /// </summary>
        internal static void Delete(FunctionObject ctx, ScriptObject instance, BoxedValue path, BoxedValue onComplete)
        {
            if (!path.IsString)
                throw new ArgumentException("[delete] First parameter should be defined and be a string.");
            if (!onComplete.IsUndefined && !onComplete.IsFunction)
                throw new ArgumentException("[delete] Second parameter should be an onComplete function.");

            // Get the curent channel
            var channel = Channel.Current;

            // Dispatch the task
            channel.Async(() =>
            {
                // Delete the file
                File.Delete(path.String);

                // Dispatch the on complete asynchronously
                channel.DispatchCallback(onComplete, instance);
            });
        }

        /// <summary>
        /// Creates a new file, writes the specified byte array to the file, and then closes the file. If the target file already exists, it is overwritten.
        /// </summary>
        internal static void WriteBuffer(FunctionObject ctx, ScriptObject instance, BoxedValue path, BoxedValue contents, BoxedValue onComplete)
        {
            if (!path.IsString)
                throw new ArgumentException("[writeBuffer] First parameter should be defined and be a string.");
            if (!contents.IsObject || !(contents.Object is BufferObject))
                throw new ArgumentException("[writeBuffer] Second parameter should be defined and be a Buffer.");
            if (!onComplete.IsUndefined && !onComplete.IsFunction)
                throw new ArgumentException("[writeBuffer] Third parameter should be an onComplete function.");

            // Get the curent channel
            var channel = Channel.Current;

            // Dispatch the task
            channel.Async(() =>
            {
                // Get the buffer
                var buffer = contents.Object as BufferObject;

                // Write the contents
                File.WriteAllBytes(
                    path.String,
                    buffer.Array
                    );

                // Dispatch the on complete asynchronously
                channel.DispatchCallback(onComplete, instance);
            });
        }

        /// <summary>
        /// Opens a binary file, reads the contents of the file into a byte array, and then closes the file.
        /// </summary>
        internal static void ReadBuffer(FunctionObject ctx, ScriptObject instance, BoxedValue path, BoxedValue onComplete)
        {
            if (!path.IsString)
                throw new ArgumentException("[readBuffer] First parameter should be defined and be a string.");
            if (!onComplete.IsFunction)
                throw new ArgumentException("[readBuffer] Second parameter should be an onComplete function.");

            // Get the curent channel
            var channel = Channel.Current;

            // Dispatch the task
            channel.Async(() =>
            {
                // Read the file
                var arr = File.ReadAllBytes(path.String);
                var seg = new ArraySegment<byte>(arr);

                // Unbox the array of lines and execute the append
                var buffer = BoxedValue.Box(
                    new BufferObject(seg, instance.Env)
                    );

                // Dispatch the on complete asynchronously
                channel.DispatchCallback(onComplete, instance, buffer);
            });
        }
    }

}