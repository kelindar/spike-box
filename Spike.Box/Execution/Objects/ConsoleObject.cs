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
    /// Represents a remote console.
    /// </summary>
    public sealed class ConsoleObject : BaseObject
    {
        #region Constructors
        /// <summary>
        /// Creates a new container that servers as an application container for a scope.
        /// </summary>
        /// <param name="prototype">The prototype to use.</param>
        public ConsoleObject(ScriptObject prototype)
            : base(prototype)
        {

        }

        /// <summary>
        /// Creates a new container that servers as an application container for a scope.
        /// </summary>
        /// <param name="prototypeName">The name of the prototype to use.</param>
        public ConsoleObject(ScriptContext context)
            : this(context.GetPrototype("Console"))
        {

        }
        #endregion

        #region Exposed Members
        /// <summary>
        /// Sends the event through the current session scope.
        /// </summary>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="eventValue">The value of the event.</param>
        private static void SendEvent(string methodName, BoxedValue eventValue)
        {
            Channel.Current.DispatchConsole(methodName, eventValue);
        }

        /// <summary>
        /// Informative logging information. You may use string substitution and additional arguments 
        /// with this method. 
        /// </summary>
        /// <param name="ctx">The function context.</param>
        /// <param name="instance">The console object instance.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="eventValue">The value of the event.</param>
        internal static void Info(FunctionObject ctx, ScriptObject instance, BoxedValue eventValue)
        {
            ConsoleObject.SendEvent("info", eventValue);
        }

        /// <summary>
        /// An alias for log(); this was added to improve compatibility with existing sites already 
        /// using debug(). However, you should use console.log() instead.
        /// </summary>
        /// <param name="ctx">The function context.</param>
        /// <param name="instance">The console object instance.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="eventValue">The value of the event.</param>
        internal static void Debug(FunctionObject ctx, ScriptObject instance,  BoxedValue eventValue)
        {
            ConsoleObject.SendEvent("debug", eventValue);
        }


        /// <summary>
        /// For general output of logging information. You may use string substitution and additional 
        /// arguments with this method
        /// </summary>
        /// <param name="ctx">The function context.</param>
        /// <param name="instance">The console object instance.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="eventValue">The value of the event.</param>
        internal static void Log(FunctionObject ctx, ScriptObject instance, BoxedValue eventValue)
        {
            ConsoleObject.SendEvent("log", eventValue);
        }


        /// <summary>
        /// Creates a new inline group, indenting all following output by another level. To move 
        /// back out a level, call groupEnd().
        /// </summary>
        /// <param name="ctx">The function context.</param>
        /// <param name="instance">The console object instance.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="eventValue">The value of the event.</param>
        internal static void Group(FunctionObject ctx, ScriptObject instance, BoxedValue eventValue)
        {
            ConsoleObject.SendEvent("group", eventValue);
        }


        /// <summary>
        /// Displays an interactive listing of the properties of a specified JavaScript object. This 
        /// listing lets you use disclosure triangles to examine the contents of child objects.
        /// </summary>
        /// <param name="ctx">The function context.</param>
        /// <param name="instance">The console object instance.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="eventValue">The value of the event.</param>
        internal static void Dir(FunctionObject ctx, ScriptObject instance, BoxedValue eventValue)
        {
            ConsoleObject.SendEvent("dir", eventValue);
        }


        /// <summary>
        /// Creates a new inline group, indenting all following output by another level; unlike group(),
        /// this starts with the inline group collapsed, requiring the use of a disclosure button to
        /// expand it. To move back out a level, call groupEnd()
        /// </summary>
        /// <param name="ctx">The function context.</param>
        /// <param name="instance">The console object instance.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="eventValue">The value of the event.</param>
        internal static void GroupCollapsed(FunctionObject ctx, ScriptObject instance, BoxedValue eventValue)
        {
            ConsoleObject.SendEvent("groupCollapsed", eventValue);
        }


        /// <summary>
        /// Exits the current inline group. 
        /// </summary>
        /// <param name="ctx">The function context.</param>
        /// <param name="instance">The console object instance.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="eventValue">The value of the event.</param>
        internal static void GroupEnd(FunctionObject ctx, ScriptObject instance)
        {
            ConsoleObject.SendEvent("groupEnd", Undefined.Boxed);
        }

        /// <summary>
        /// Starts a timer with a name specified as an input parameter. Up to 10,000 simultaneous 
        /// timers can run on a given page
        /// </summary>
        /// <param name="ctx">The function context.</param>
        /// <param name="instance">The console object instance.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="eventValue">The value of the event.</param>
        internal static void Time(FunctionObject ctx, ScriptObject instance, BoxedValue eventValue)
        {
            ConsoleObject.SendEvent("time", eventValue);
        }

        /// <summary>
        /// Stops the specified timer and logs the elapsed time in seconds since its start.
        /// </summary>
        /// <param name="ctx">The function context.</param>
        /// <param name="instance">The console object instance.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="eventValue">The value of the event.</param>
        internal static void TimeEnd(FunctionObject ctx, ScriptObject instance, BoxedValue eventValue)
        {
            ConsoleObject.SendEvent("timeEnd", eventValue);
        }

        /// <summary>
        /// Outputs a warning message. You may use string substitution and additional arguments with this method.
        /// </summary>
        /// <param name="ctx">The function context.</param>
        /// <param name="instance">The console object instance.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="eventValue">The value of the event.</param>
        internal static void Warn(FunctionObject ctx, ScriptObject instance, BoxedValue eventValue)
        {
            ConsoleObject.SendEvent("warn", eventValue);
        }


        #endregion
    }
}