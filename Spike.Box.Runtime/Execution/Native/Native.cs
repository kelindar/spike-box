using System;
using System.Linq;
using System.Text;
using Spike.Scripting.Native;
using Spike.Scripting.Runtime;

namespace Spike.Box
{
    /// <summary>
    /// Represents a class that contains native interop.
    /// </summary>
    public partial class Native : IModule
    {
        /// <summary>
        /// Gets the name of the module.
        /// </summary>
        public string Name
        {
            get { return "native"; }
        }

        /// <summary>
        /// Registers the module inside a script context.
        /// </summary>
        /// <param name="context">The context to register the module to.</param>
        public void Register(ScriptContext context)
        {
            // Contains in F#: NaN
            // Contains in F#: Infinity
            // Contains in F#: undefined
            // Contains in F#: eval
            // Contains in F#: parseFloat
            // Contains in F#: parseInt
            // Contains in F#: isNaN
            // Contains in F#: isFinite
            // Contains in F#: decodeURI
            // Contains in F#: decodeURIComponent
            // Contains in F#: encodeURI
            // Contains in F#: encodeURIComponent

            // Create system types
            context.CreateType<AppScope>("Application");
            context.CreateType<SessionScope>("Session");
            context.CreateType<PageScope>("Page");
            
            context.CreateType<ConsoleObject>("Console", (prototype) => new ConsoleObject(prototype),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue>("info", ConsoleObject.Info),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue>("debug", ConsoleObject.Debug),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue>("log", ConsoleObject.Log),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue>("group", ConsoleObject.Group),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue>("groupCollapsed", ConsoleObject.GroupCollapsed),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue>("dir", ConsoleObject.Dir),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue>("time", ConsoleObject.Time),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue>("timeEnd", ConsoleObject.TimeEnd),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue>("warn", ConsoleObject.Warn),
                context.CreateFunction<FunctionObject, ScriptObject>("groupEnd", ConsoleObject.GroupEnd)
                );

            context.CreateType<FileObject>("File", (prototype) => new FileObject(prototype),
                context.CreateFunction<FunctionObject, ScriptObject, string, string>("writeText", FileObject.WriteText),
                context.CreateFunction<FunctionObject, ScriptObject, string, BoxedValue>("readText", FileObject.ReadText),
                context.CreateFunction<FunctionObject, ScriptObject, string, BoxedValue>("writeJson", FileObject.WriteJson),
                context.CreateFunction<FunctionObject, ScriptObject, string, BoxedValue>("readJson", FileObject.ReadJson)
                );

            // Timer: Delay Call
            context.AttachFunction<FunctionObject, double, BoxedValue, BoxedValue, BoxedValue, ScriptObject>("setTimeout", Native.SetTimeout);
            context.AttachFunction<BoxedValue>("clearTimeout", Native.ClearTimeout);

            // Timer: Interval Call
            context.AttachFunction<FunctionObject, double, BoxedValue, BoxedValue, BoxedValue, ScriptObject>("setInterval", Native.SetInterval);
            context.AttachFunction<BoxedValue>("clearInterval", Native.ClearTimeout);

            // Encoding: Base64
            context.AttachFunction<BoxedValue, BoxedValue>("btoa", Native.BtoA);
            context.AttachFunction<BoxedValue, BoxedValue>("atob", Native.AtoB);

            // Network: Core
            context.AttachFunction<BoxedValue, double, BoxedValue, string, BoxedValue>("transmitEvent", Native.TransmitEvent);
            context.AttachFunction<BoxedValue, BoxedValue, string, BoxedValue>("transmitProperty", Native.TransmitProperty);
            context.AttachFunction<BoxedValue, string, BoxedValue>("transmitConsole", Native.TransmitConsole);

            // Buffer
            context.CreateType<BufferObject, BoxedValue, BoxedValue>("Buffer", (o, param, encoding) => new BufferObject(o, param, encoding),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue, BoxedValue, string>("toString", BufferObject.ToString),
                context.CreateFunction<FunctionObject, ScriptObject, string>("toJSON", BufferObject.ToJSON),
                context.CreateFunction<FunctionObject, ScriptObject, string, BoxedValue, BoxedValue, BoxedValue, int>("write", BufferObject.Write),
                context.CreateFunction<FunctionObject, ScriptObject, BufferObject, BoxedValue, BoxedValue, BoxedValue>("copy", BufferObject.Copy),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue, BufferObject>("slice", BufferObject.Slice),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue, BoxedValue>("fill", BufferObject.Fill),

                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("writeUInt8", BufferObject.WriteUInt8),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("writeInt8", BufferObject.WriteInt8),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("writeUInt16BE", BufferObject.WriteUInt16BE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("writeUInt32BE", BufferObject.WriteUInt32BE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("writeUInt64BE", BufferObject.WriteUInt64BE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("writeInt16BE", BufferObject.WriteInt16BE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("writeInt32BE", BufferObject.WriteInt32BE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("writeInt64BE", BufferObject.WriteInt64BE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("writeFloatBE", BufferObject.WriteFloatBE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("writeDoubleBE", BufferObject.WriteDoubleBE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("writeUInt16LE", BufferObject.WriteUInt16LE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("writeUInt32LE", BufferObject.WriteUInt32LE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("writeUInt64LE", BufferObject.WriteUInt64LE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("writeInt16LE", BufferObject.WriteInt16LE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("writeInt32LE", BufferObject.WriteInt32LE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("writeInt64LE", BufferObject.WriteInt64LE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("writeFloatLE", BufferObject.WriteFloatLE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("writeDoubleLE", BufferObject.WriteDoubleLE),

                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("readUInt8", BufferObject.ReadUInt8),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("readInt8", BufferObject.ReadInt8),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("readUInt16BE", BufferObject.ReadUInt16BE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("readUInt32BE", BufferObject.ReadUInt32BE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("readUInt64BE", BufferObject.ReadUInt64BE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("readInt16BE", BufferObject.ReadInt16BE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("readInt32BE", BufferObject.ReadInt32BE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("readInt64BE", BufferObject.ReadInt64BE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("readFloatBE", BufferObject.ReadFloatBE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("readDoubleBE", BufferObject.ReadDoubleBE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("readUInt16LE", BufferObject.ReadUInt16LE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("readUInt32LE", BufferObject.ReadUInt32LE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("readUInt64LE", BufferObject.ReadUInt64LE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("readInt16LE", BufferObject.ReadInt16LE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("readInt32LE", BufferObject.ReadInt32LE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("readInt64LE", BufferObject.ReadInt64LE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("readFloatLE", BufferObject.ReadFloatLE),
                context.CreateFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue>("readDoubleLE", BufferObject.ReadDoubleLE)
                );

            // Extend Native
            context.AttachFunction<FunctionObject, ScriptObject, ScriptObject, string, ScriptObject>(context.Environment.Prototypes.Object, "defineProperty", Native.DefineProperty);
            context.AttachFunction<FunctionObject, ScriptObject, FunctionObject, BoxedValue>(context.Environment.Prototypes.Array, "forEach", Native.Array_ForEach);
            context.AttachFunction<FunctionObject, ScriptObject, FunctionObject, BoxedValue>(context.Environment.Prototypes.Array, "removeAll", Native.Array_RemoveAll);
            context.AttachFunction<FunctionObject, ScriptObject, FunctionObject, BoxedValue, BoxedValue>(context.Environment.Prototypes.Array, "every", Native.Array_Every);
            context.AttachFunction<FunctionObject, ScriptObject, BoxedValue, BoxedValue, BoxedValue>(context.Environment.Prototypes.Array, "swap", Native.Array_Swap);
            context.AttachFunction<FunctionObject, ScriptObject>(context.Environment.Prototypes.Array, "clear", Native.Array_Clear);

            // Create global instances
            context.AttachGlobal("console", new ConsoleObject(context));
            context.AttachGlobal("file", new FileObject(context));

            // Hook the observation
            ScriptObject.PropertyChange += Native.OnPropertyChange;
        }

    }
}