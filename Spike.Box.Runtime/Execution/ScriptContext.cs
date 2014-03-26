using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spike.Scripting.Hosting;
using Spike.Scripting.Native;
using Spike.Scripting.Runtime;
using Spike.Box.Properties;
using Env = Spike.Scripting.Runtime.Environment;
using Microsoft.FSharp.Core;
using System.Reflection;
using System.Threading.Tasks;
using Spike.Box.Threading;

namespace Spike.Box
{
    /// <summary>
    /// Represents an execution context.
    /// </summary>
    public class ScriptContext : DisposableObject
    {
        #region Constructor
        /// <summary>
        /// Gets the scripting context associated with this <see cref="ScriptContext"/>.
        /// </summary>
        private readonly CSharp.Context Context;

        /// <summary>
        /// Gets the runtime object.
        /// </summary>
        private readonly ScriptObject Runtime;

        /// <summary>
        /// Gets the task scheduler.
        /// </summary>
        private readonly TaskScheduler Scheduler;

        /// <summary>
        /// Constructs a new instance of a <see cref="ScriptContext"/>.
        /// </summary>
        /// <param name="app">The owner application.</param>
        public ScriptContext()
        {
            // Create a specific scheduler
            var scheduler = new QueuedTaskScheduler(1, "JS Thread");

            // Activate a new scheduling queue
            this.Scheduler = scheduler.ActivateNewQueue();

            // Create new script context
            this.Context = new CSharp.Context();
            this.Context.CreatePrintFunction();
            this.AttachGlobal("global", this.Context.Globals);

            // Include all the modules
            Modules.IncludeIn(this);

            // Set the runtime
            this.Runtime = this.Context.Globals.Get("Runtime").Object;
        }

        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the scripting environment.
        /// </summary>
        public Env Environment
        {
            get { return this.Context.Environment; }
        }
        #endregion

        #region Public Members (Threading)
        /// <summary>
        /// Invokes a function within this context.
        /// </summary>
        /// <typeparam name="T">The expected result of the function.</typeparam>
        /// <param name="script">The function to execute.</param>
        /// <param name="channel">The channel to execute on.</param>
        /// <returns>The awaitable asynchrounous task.</returns>
        public async Task DispatchAsync(Action script, Channel channel)
        {
            // Create a new task. This 
            var task = new Task(() =>
            {
                try
                {
                    // Make sure we have a thread static session set
                    Channel.Current = channel;

                    // Execute the action
                    script();
                }
                catch (Exception ex)
                {
                    // Send the exception
                    try
                    {
                        // Log to the remote channel.
                        Channel.Current.SendException(ex);
                    }
                    catch (Exception ex2)
                    {
                        // Log to the console.
                        Service.Logger.Log(ex2);
                    }
                }
                finally
                {
                    // Reset the scope back to null
                    Channel.Current = null;
                }
            });

            // Start the task on the specific scheduler
            task.Start(this.Scheduler);

            // Return the task for awaiting
            await task;
        }

        /// <summary>
        /// Invokes a function within this context.
        /// </summary>
        /// <typeparam name="T">The expected result of the function.</typeparam>
        /// <param name="script">The function to execute.</param>
        /// <param name="channel">The channel to execute on.</param>
        /// <returns>The result of the invoke.</returns>
        public void Dispatch(Action script, Channel channel)
        {
            // Invoke on the script context, natively
            var task = this.DispatchAsync(script, channel);

            // Wait for the script to complete
            task.Wait();
        }

        /// <summary>
        /// Invokes a function within this context.
        /// </summary>
        /// <typeparam name="T">The expected result of the function.</typeparam>
        /// <param name="script">The function to execute.</param>
        /// <param name="channel">The channel to execute on.</param>
        /// <returns>The awaitable asynchrounous task.</returns>
        public async Task<T> DispatchAsync<T>(Func<T> script, Channel channel)
        {
            // Create a new task. This 
            var task = new Task<T>(() =>
            {
                try
                {
                    // Make sure we have a thread static session set
                    Channel.Current = channel;

                    // Execute the function
                    return script();
                }
                catch (Exception ex)
                {
                    // Send the exception
                    try
                    {
                        // Log to the remote channel.
                        Channel.Current.SendException(ex);
                    }
                    catch(Exception ex2)
                    {
                        // Log to the console.
                        Service.Logger.Log(ex2);
                    }
                }
                finally
                {
                    // Reset the scope back to null
                    Channel.Current = null;
                }

                // On error, always return empty
                return default(T);
            });

            // Start the task on the specific scheduler
            task.Start(this.Scheduler);

            // Return the task 
            return await task;
        }

        /// <summary>
        /// Invokes a function within this context.
        /// </summary>
        /// <typeparam name="T">The expected result of the function.</typeparam>
        /// <param name="script">The function to execute.</param>
        /// <param name="channel">The channel to execute on.</param>
        /// <returns>The result of the invoke.</returns>
        public T Dispatch<T>(Func<T> script, Channel channel)
        {
            // Invoke on the script context, natively
            var task = this.DispatchAsync<T>(script, channel);

            // Wait for the script to complete
            task.Wait();

            // Return the result
            return task.Result;
        }
        #endregion

        #region Public Members (Import)

        /// <summary>
        /// Imports a javascript library to this context.
        /// </summary>
        /// <param name="name">The name of the library to import.</param>
        /// <param name="import">The action that does the importing.</param>
        public void Import(string name, Action<ScriptContext> import)
        {
            try
            {
                // Measure the time before importing
                var start = DateTime.Now;

                // Start importing
                import(this);

                // We're done importing, successfully
                var timeTaken = (DateTime.Now - start);
                Console.WriteLine("Scripting: Imported {0} library, {1}ms.", name, timeTaken.TotalMilliseconds);
            }
            catch(Exception ex)
            {
                // Write why we were unable to import.
                Service.Logger.Log(LogLevel.Error, 
                    String.Format("Scripting: Unable to import {0} library. Error: {1}", name, ex.Message)
                    );
            }
            
        }

        #endregion

        #region Public Members (Helpers)
        /// <summary>
        /// Evaluates a javascript within this context.
        /// </summary>
        /// <param name="source">The source to execute.</param>
        public dynamic Eval(string source)
        {
            // Import within this context by simply executing it
            return this.Context.Execute(source);
        }

        /// <summary>
        /// Gets the property of the scope object attached to the script.
        /// </summary>
        /// <typeparam name="T">The type of the object attached.</typeparam>
        /// <param name="name">The name of the object.</param>
        /// <returns>The retrieved object.</returns>
        public T Get<T>(Scope scope, string name)
            where T : ScriptObject
        {
            return scope.GetT<T>(name);
        }

        /// <summary>
        /// Gets the prototype by the name.
        /// </summary>
        /// <param name="typeName">The name of the type to get.</param>
        /// <returns>Returns the type.</returns>
        public ScriptObject GetPrototype(string typeName)
        {
            return this.Context.Globals
                .GetT<ScriptObject>(typeName)
                .GetT<ScriptObject>("prototype");
        }


        /// <summary>
        /// Attaches an instance to the global scope.
        /// </summary>
        /// <param name="instanceName">The name of the instance to attach.</param>
        /// <param name="instance">The instance value.</param>
        public void AttachGlobal(string instanceName, ScriptObject instance)
        {
            this.Context.SetGlobal(instanceName, instance);
        }

        /// <summary>
        /// Executes and includes a script within this context.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <returns>The proxy tree that can be used to generate the client script.</returns>
        internal Surrogate Include(MetaScript script)
        {
            // Count the globals before
            var typesOffset = this.Context.Globals.Members.Count;
            var sourceFunc = this.Context.Execute(script.CodeBehind) as FunctionObject;
            // TODO: detect the name
            /*if(sourceFunc != null)
            {
                Console.WriteLine(sourceFunc.Name);

            }*/

            // A list that contains proxies to generate
            var proxies = new List<Surrogate>();

            // Loop through all new types that the script has introduced
            var typesCount = this.Context.Globals.Members.Count;

            // No new types!
            if (typesCount == typesOffset)
                throw new ApplicationException("No new types were detected in " + script.Key + ". Make sure you have a global variable assigned and it haven't been declared already.");

            // Loop through new types
            for (int i = typesOffset; i < typesCount; ++i)
            {
                var name = this.Context.Globals.Members.Keys.ElementAt(i);
                var type = this.Context.Globals.Members.Values.ElementAt(i) as FunctionObject;

                // Get the properties
                var properties = type.InstancePrototype.Members["properties"] as ScriptPropertiesInfo;
                if (properties == null)
                    properties = new ScriptPropertiesInfo();

                // Only functions (classes)
                if (type == null)
                    continue;

                // Get the methods
                var methods = type.InstancePrototype.Members
                    .Where(f => f.Value is FunctionObject && f.Key != "constructor")
                    .Select(f => (properties.Any(p => p.Name == f.Key)
                        ? (SurrogateMember)(new SurrogateProperty(properties.Where(p => p.Name == f.Key).FirstOrDefault()))
                        : (SurrogateMember)(new SurrogateFunction(f.Key))
                        ))
                    .ToArray();

                // Create a proxy, only the first class in the file is used for client proxy generation
                return new Surrogate(script.Key, name, methods);

            }

            return null;
        }

        #endregion

        #region Public Members (CreateFunction)

        /// <summary>
        /// Creates a FunctionObject.
        /// </summary>
        /// <typeparam name="T">The delegate definition.</typeparam>
        /// <param name="length">Number of parameters</param>
        /// <param name="function">The delegate.</param>
        private FunctionObject CreateDelegate<T>(int length, T function)
        {
            // Calls CreateFunction dynamically
            // => Utils.CreateFunction<T>(this.Environment, length, function);
            var method = typeof(Utils).GetMethod("CreateFunction");
            var generic = method.MakeGenericMethod(typeof(T));
            var functionObject = generic.Invoke(null, new object[]{
                this.Environment, length, function
            }) as FunctionObject;
            return functionObject;
        }

        /// <summary>
        /// Creates a FunctionObject.
        /// </summary>
        /// <typeparam name="T">The delegate definition.</typeparam>
        /// <param name="name">The name of the function.</param>
        /// <param name="length">Number of parameters</param>
        /// <param name="function">The delegate.</param>
        private NamedFunctionObject CreateDelegate<T>(string name, int length, T function)
        {
            // Calls CreateFunction dynamically
            // => Utils.CreateFunction<T>(this.Environment, length, function);
            var method = typeof(Utils).GetMethod("CreateFunction");
            var generic = method.MakeGenericMethod(typeof(T));
            var functionObject = generic.Invoke(null, new object[]{
                this.Environment, length, function
            }) as FunctionObject;
            return new NamedFunctionObject(name, functionObject);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public FunctionObject CreateFunction<TResult>(Func<TResult> function)
        {
            return this.CreateDelegate<Func<TResult>>(0, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public FunctionObject CreateFunction<T, TResult>(Func<T, TResult> function)
        {
            return this.CreateDelegate<Func<T, TResult>>(1, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public FunctionObject CreateFunction<T1, T2, TResult>(Func<T1, T2, TResult> function)
        {
            return this.CreateDelegate<Func<T1, T2, TResult>>(2, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public FunctionObject CreateFunction<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> function)
        {
            return this.CreateDelegate<Func<T1, T2, T3, TResult>>(3, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public FunctionObject CreateFunction<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> function)
        {
            return this.CreateDelegate<Func<T1, T2, T3, T4, TResult>>(4, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T5">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public FunctionObject CreateFunction<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> function)
        {
            return this.CreateDelegate<Func<T1, T2, T3, T4, T5, TResult>>(5, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T5">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T6">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public FunctionObject CreateFunction<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, TResult> function)
        {
            return this.CreateDelegate<Func<T1, T2, T3, T4, T5, T6, TResult>>(6, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T5">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T6">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T7">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public FunctionObject CreateFunction<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, TResult> function)
        {
            return this.CreateDelegate<Func<T1, T2, T3, T4, T5, T6, T7, TResult>>(7, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T5">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T6">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T7">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T8">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public FunctionObject CreateFunction<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> function)
        {
            return this.CreateDelegate<Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>>(8, function);
        }


        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <param name="function">The native function to execute.</param>
        public FunctionObject CreateFunction(Action function)
        {
            return this.CreateDelegate<Action>(0, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public FunctionObject CreateFunction<T>(Action<T> function)
        {
            return this.CreateDelegate<Action<T>>(1, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public FunctionObject CreateFunction<T1, T2>(Action<T1, T2> function)
        {
            return this.CreateDelegate<Action<T1, T2>>(2, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public FunctionObject CreateFunction<T1, T2, T3>(Action<T1, T2, T3> function)
        {
            return this.CreateDelegate<Action<T1, T2, T3>>(3, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public FunctionObject CreateFunction<T1, T2, T3, T4>(Action<T1, T2, T3, T4> function)
        {
            return this.CreateDelegate<Action<T1, T2, T3, T4>>(4, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T5">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public FunctionObject CreateFunction<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> function)
        {
            return this.CreateDelegate<Action<T1, T2, T3, T4, T5>>(5, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T5">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T6">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public FunctionObject CreateFunction<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> function)
        {
            return this.CreateDelegate<Action<T1, T2, T3, T4, T5, T6>>(6, function);
        }


        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T5">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T6">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T7">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public FunctionObject CreateFunction<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> function)
        {
            return this.CreateDelegate<Action<T1, T2, T3, T4, T5, T6, T7>>(7, function);
        }


        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T5">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T6">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T7">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T8">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public FunctionObject CreateFunction<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> function)
        {
            return this.CreateDelegate<Action<T1, T2, T3, T4, T5, T6, T7, T8>>(8, function);
        }


        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public NamedFunctionObject CreateFunction<TResult>(string name, Func<TResult> function)
        {
            return this.CreateDelegate<Func<TResult>>(name, 0, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public NamedFunctionObject CreateFunction<T, TResult>(string name, Func<T, TResult> function)
        {
            return this.CreateDelegate<Func<T, TResult>>(name, 1, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public NamedFunctionObject CreateFunction<T1, T2, TResult>(string name, Func<T1, T2, TResult> function)
        {
            return this.CreateDelegate<Func<T1, T2, TResult>>(name, 2, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public NamedFunctionObject CreateFunction<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> function)
        {
            return this.CreateDelegate<Func<T1, T2, T3, TResult>>(name, 3, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public NamedFunctionObject CreateFunction<T1, T2, T3, T4, TResult>(string name, Func<T1, T2, T3, T4, TResult> function)
        {
            return this.CreateDelegate<Func<T1, T2, T3, T4, TResult>>(name, 4, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T5">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public NamedFunctionObject CreateFunction<T1, T2, T3, T4, T5, TResult>(string name, Func<T1, T2, T3, T4, T5, TResult> function)
        {
            return this.CreateDelegate<Func<T1, T2, T3, T4, T5, TResult>>(name, 5, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T5">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T6">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public NamedFunctionObject CreateFunction<T1, T2, T3, T4, T5, T6, TResult>(string name, Func<T1, T2, T3, T4, T5, T6, TResult> function)
        {
            return this.CreateDelegate<Func<T1, T2, T3, T4, T5, T6, TResult>>(name, 6, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T5">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T6">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T7">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public NamedFunctionObject CreateFunction<T1, T2, T3, T4, T5, T6, T7, TResult>(string name, Func<T1, T2, T3, T4, T5, T6, T7, TResult> function)
        {
            return this.CreateDelegate<Func<T1, T2, T3, T4, T5, T6, T7, TResult>>(name, 7, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T5">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T6">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T7">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T8">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public NamedFunctionObject CreateFunction<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(string name, Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> function)
        {
            return this.CreateDelegate<Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>>(name, 8, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <param name="function">The native function to execute.</param>
        public NamedFunctionObject CreateFunction(string name, Action function)
        {
            return this.CreateDelegate<Action>(name, 0, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public NamedFunctionObject CreateFunction<T>(string name, Action<T> function)
        {
            return this.CreateDelegate<Action<T>>(name, 1, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public NamedFunctionObject CreateFunction<T1, T2>(string name, Action<T1, T2> function)
        {
            return this.CreateDelegate<Action<T1, T2>>(name, 2, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public NamedFunctionObject CreateFunction<T1, T2, T3>(string name, Action<T1, T2, T3> function)
        {
            return this.CreateDelegate<Action<T1, T2, T3>>(name, 3, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public NamedFunctionObject CreateFunction<T1, T2, T3, T4>(string name, Action<T1, T2, T3, T4> function)
        {
            return this.CreateDelegate<Action<T1, T2, T3, T4>>(name, 4, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T5">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public NamedFunctionObject CreateFunction<T1, T2, T3, T4, T5>(string name, Action<T1, T2, T3, T4, T5> function)
        {
            return this.CreateDelegate<Action<T1, T2, T3, T4, T5>>(name, 5, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T5">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T6">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public NamedFunctionObject CreateFunction<T1, T2, T3, T4, T5, T6>(string name, Action<T1, T2, T3, T4, T5, T6> function)
        {
            return this.CreateDelegate<Action<T1, T2, T3, T4, T5, T6>>(name, 6, function);
        }

        #endregion

        #region Public Members (AttachDelegate)
        /// <summary>
        /// Creates a FunctionObject and attaches it to the prototype.
        /// </summary>
        /// <typeparam name="T">The delegate definition.</typeparam>
        /// <param name="length">Number of parameters</param>
        /// <param name="function">The delegate.</param>
        private void AttachDelegate<T>(ScriptObject prototype, string name, int length, T function)
        {
            // Make the object
            var functionObject = CreateDelegate<T>(length, function);

            // Attach to the prototype
            prototype.Put(name, functionObject);
        }

        /// <summary>
        /// Creates a FunctionObject and attaches it to the global.
        /// </summary>
        /// <typeparam name="T">The delegate definition.</typeparam>
        /// <param name="length">Number of parameters</param>
        /// <param name="function">The delegate.</param>
        private void AttachDelegate<T>(string name, int length, T function)
        {
            // Make the object
            var functionObject = CreateDelegate<T>(length, function);

            // Attach to the prototype
            this.Context.Globals.Put(name, functionObject);
        }


        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<TResult>(string name, Func<TResult> function)
        {
            this.AttachDelegate<Func<TResult>>(name, 0, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<T, TResult>(string name, Func<T, TResult> function)
        {
            this.AttachDelegate<Func<T, TResult>>(name, 1, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<T1, T2, TResult>(string name, Func<T1, T2, TResult> function)
        {
            this.AttachDelegate<Func<T1, T2, TResult>>(name, 2, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> function)
        {
            this.AttachDelegate<Func<T1, T2, T3, TResult>>(name, 3, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<T1, T2, T3, T4, TResult>(string name, Func<T1, T2, T3, T4, TResult> function)
        {
            this.AttachDelegate<Func<T1, T2, T3, T4, TResult>>(name, 4, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T5">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<T1, T2, T3, T4, T5, TResult>(string name, Func<T1, T2, T3, T4, T5, TResult> function)
        {
            this.AttachDelegate<Func<T1, T2, T3, T4, T5, TResult>>(name, 5, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T5">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T6">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<T1, T2, T3, T4, T5, T6, TResult>(string name, Func<T1, T2, T3, T4, T5, T6, TResult> function)
        {
            this.AttachDelegate<Func<T1, T2, T3, T4, T5, T6, TResult>>(name, 6, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction(string name, Action function)
        {
            this.AttachDelegate<Action>(name, 0, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<T>(string name, Action<T> function)
        {
            this.AttachDelegate<Action<T>>(name, 1, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<T1, T2>(string name, Action<T1, T2> function)
        {
            this.AttachDelegate<Action<T1, T2>>(name, 2, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<T1, T2, T3>(string name, Action<T1, T2, T3> function)
        {
            this.AttachDelegate<Action<T1, T2, T3>>(name, 3, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<T1, T2, T3, T4>(string name, Action<T1, T2, T3, T4> function)
        {
            this.AttachDelegate<Action<T1, T2, T3, T4>>(name, 4, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T5">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<T1, T2, T3, T4, T5>(string name, Action<T1, T2, T3, T4, T5> function)
        {
            this.AttachDelegate<Action<T1, T2, T3, T4, T5>>(name, 5, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T5">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T6">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<T1, T2, T3, T4, T5, T6>(string name, Action<T1, T2, T3, T4, T5, T6> function)
        {
            this.AttachDelegate<Action<T1, T2, T3, T4, T5, T6>>(name, 6, function);
        }


        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<TResult>(ScriptObject prototype, string name, Func<TResult> function)
        {
            this.AttachDelegate<Func<TResult>>(prototype, name, 0, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<T, TResult>(ScriptObject prototype, string name, Func<T, TResult> function)
        {
            this.AttachDelegate<Func<T, TResult>>(prototype, name, 1, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<T1, T2, TResult>(ScriptObject prototype, string name, Func<T1, T2, TResult> function)
        {
            this.AttachDelegate<Func<T1, T2, TResult>>(prototype, name, 2, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<T1, T2, T3, TResult>(ScriptObject prototype, string name, Func<T1, T2, T3, TResult> function)
        {
            this.AttachDelegate<Func<T1, T2, T3, TResult>>(prototype, name, 3, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<T1, T2, T3, T4, TResult>(ScriptObject prototype, string name, Func<T1, T2, T3, T4, TResult> function)
        {
            this.AttachDelegate<Func<T1, T2, T3, T4, TResult>>(prototype, name, 4, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T5">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<T1, T2, T3, T4, T5, TResult>(ScriptObject prototype, string name, Func<T1, T2, T3, T4, T5, TResult> function)
        {
            this.AttachDelegate<Func<T1, T2, T3, T4, T5, TResult>>(prototype, name, 5, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T5">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T6">Type of the parameter for this function.</typeparam>
        /// <typeparam name="TResult">Type of the result for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<T1, T2, T3, T4, T5, T6, TResult>(ScriptObject prototype, string name, Func<T1, T2, T3, T4, T5, T6, TResult> function)
        {
            this.AttachDelegate<Func<T1, T2, T3, T4, T5, T6, TResult>>(prototype, name, 6, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction(ScriptObject prototype, string name, Action function)
        {
            this.AttachDelegate<Action>(prototype, name, 0, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<T>(ScriptObject prototype, string name, Action<T> function)
        {
            this.AttachDelegate<Action<T>>(prototype, name, 1, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<T1, T2>(ScriptObject prototype, string name, Action<T1, T2> function)
        {
            this.AttachDelegate<Action<T1, T2>>(prototype, name, 2, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<T1, T2, T3>(ScriptObject prototype, string name, Action<T1, T2, T3> function)
        {
            this.AttachDelegate<Action<T1, T2, T3>>(prototype, name, 3, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<T1, T2, T3, T4>(ScriptObject prototype, string name, Action<T1, T2, T3, T4> function)
        {
            this.AttachDelegate<Action<T1, T2, T3, T4>>(prototype, name, 4, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T5">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<T1, T2, T3, T4, T5>(ScriptObject prototype, string name, Action<T1, T2, T3, T4, T5> function)
        {
            this.AttachDelegate<Action<T1, T2, T3, T4, T5>>(prototype, name, 5, function);
        }

        /// <summary>
        /// Creates a function within ghis <see cref="ScriptContext"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T2">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T3">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T4">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T5">Type of the parameter for this function.</typeparam>
        /// <typeparam name="T6">Type of the parameter for this function.</typeparam>
        /// <param name="function">The native function to execute.</param>
        public void AttachFunction<T1, T2, T3, T4, T5, T6>(ScriptObject prototype, string name, Action<T1, T2, T3, T4, T5, T6> function)
        {
            this.AttachDelegate<Action<T1, T2, T3, T4, T5, T6>>(prototype, name, 6, function);
        }
        #endregion

        #region Public Members (CreateConstructor)
        /// <summary>
        /// Creates a constructor.
        /// </summary>
        /// <typeparam name="T">The delegate definition.</typeparam>
        /// <param name="length">Number of parameters</param>
        /// <param name="function">The delegate.</param>
        private FunctionObject CreateConstructor<T>(int length, T function)
        {
            // Calls CreateFunction dynamically
            // => Utils.CreateConstructor<T>(this.Environment, length, function);
            var method = typeof(Utils).GetMethod("CreateConstructor");
            var generic = method.MakeGenericMethod(typeof(T));
            var functionObject = generic.Invoke(null, new object[]{
                this.Environment, length, function
            }) as FunctionObject;
            return functionObject;
        }

        /// <summary>
        /// Creates a constructor for a native object.
        /// </summary>
        /// <param name="constructor">The constructor delegate that constructs the object.</param>
        /// <returns>The constructor function.</returns>
        public FunctionObject CreateConstructor(Func<FunctionObject, ScriptObject, ScriptObject> constructor)
        {
            return CreateConstructor<Func<FunctionObject, ScriptObject, ScriptObject>>(0, constructor);
        }

        /// <summary>
        /// Creates a constructor for a native object.
        /// </summary>
        /// <typeparam name="T1">The parameter to pass to the constructor.</typeparam>
        /// <param name="constructor">The constructor delegate that constructs the object.</param>
        /// <returns>The constructor function.</returns>
        public FunctionObject CreateConstructor<T>(Func<FunctionObject, ScriptObject, T, ScriptObject> constructor)
        {
            return CreateConstructor<Func<FunctionObject, ScriptObject, T, ScriptObject>>(1, constructor);
        }

        /// <summary>
        /// Creates a constructor for a native object.
        /// </summary>
        /// <typeparam name="T1">The parameter to pass to the constructor.</typeparam>
        /// <typeparam name="T2">The parameter to pass to the constructor.</typeparam>
        /// <param name="constructor">The constructor delegate that constructs the object.</param>
        /// <returns>The constructor function.</returns>
        public FunctionObject CreateConstructor<T1, T2>(Func<FunctionObject, ScriptObject, T1, T2, ScriptObject> constructor)
        {
            return CreateConstructor<Func<FunctionObject, ScriptObject, T1, T2, ScriptObject>>(2, constructor);
        }

        /// <summary>
        /// Creates a constructor for a native object.
        /// </summary>
        /// <typeparam name="T1">The parameter to pass to the constructor.</typeparam>
        /// <typeparam name="T2">The parameter to pass to the constructor.</typeparam>
        /// <typeparam name="T3">The parameter to pass to the constructor.</typeparam>
        /// <param name="constructor">The constructor delegate that constructs the object.</param>
        /// <returns>The constructor function.</returns>
        public FunctionObject CreateConstructor<T1, T2, T3>(Func<FunctionObject, ScriptObject, T1, T2, T3, ScriptObject> constructor)
        {
            return CreateConstructor<Func<FunctionObject, ScriptObject, T1, T2, T3, ScriptObject>>(3, constructor);
        }

        /// <summary>
        /// Creates a constructor for a native object.
        /// </summary>
        /// <typeparam name="T1">The parameter to pass to the constructor.</typeparam>
        /// <typeparam name="T2">The parameter to pass to the constructor.</typeparam>
        /// <typeparam name="T3">The parameter to pass to the constructor.</typeparam>
        /// <typeparam name="T4">The parameter to pass to the constructor.</typeparam>
        /// <param name="constructor">The constructor delegate that constructs the object.</param>
        /// <returns>The constructor function.</returns>
        public FunctionObject CreateConstructor<T1, T2, T3, T4>(Func<FunctionObject, ScriptObject, T1, T2, T3, T4, ScriptObject> constructor)
        {
            return CreateConstructor<Func<FunctionObject, ScriptObject, T1, T2, T3, T4, ScriptObject>>(4, constructor);
        }

        /// <summary>
        /// Creates a constructor for a native object.
        /// </summary>
        /// <typeparam name="T1">The parameter to pass to the constructor.</typeparam>
        /// <typeparam name="T2">The parameter to pass to the constructor.</typeparam>
        /// <typeparam name="T3">The parameter to pass to the constructor.</typeparam>
        /// <typeparam name="T4">The parameter to pass to the constructor.</typeparam>
        /// <typeparam name="T5">The parameter to pass to the constructor.</typeparam>
        /// <param name="constructor">The constructor delegate that constructs the object.</param>
        /// <returns>The constructor function.</returns>
        public FunctionObject CreateConstructor<T1, T2, T3, T4, T5>(Func<FunctionObject, ScriptObject, T1, T2, T3, T4, T5, ScriptObject> constructor)
        {
            return CreateConstructor<Func<FunctionObject, ScriptObject, T1, T2, T3, T4, T5, ScriptObject>>(5, constructor);
        }

        /// <summary>
        /// Creates a constructor for a native object.
        /// </summary>
        /// <typeparam name="T1">The parameter to pass to the constructor.</typeparam>
        /// <typeparam name="T2">The parameter to pass to the constructor.</typeparam>
        /// <typeparam name="T3">The parameter to pass to the constructor.</typeparam>
        /// <typeparam name="T4">The parameter to pass to the constructor.</typeparam>
        /// <typeparam name="T5">The parameter to pass to the constructor.</typeparam>
        /// <typeparam name="T6">The parameter to pass to the constructor.</typeparam>
        /// <param name="constructor">The constructor delegate that constructs the object.</param>
        /// <returns>The constructor function.</returns>
        public FunctionObject CreateConstructor<T1, T2, T3, T4, T5, T6>(Func<FunctionObject, ScriptObject, T1, T2, T3, T4, T5, T6, ScriptObject> constructor)
        {
            return CreateConstructor<Func<FunctionObject, ScriptObject, T1, T2, T3, T4, T5, T6, ScriptObject>>(6, constructor);
        }
        #endregion

        #region Public Members (CreateType)
        /// <summary>
        /// Creates a native class.
        /// </summary>
        /// <typeparam name="T">The delegate definition.</typeparam>
        /// <param name="className">The name of the type to create.</param>
        /// <param name="length">Number of parameters</param>
        /// <param name="function">The delegate.</param>
        private void CreateType<T>(string className, int length, T function, params NamedFunctionObject[] functions)
        {
            // Create a constructor
            var prototype = this.Environment.NewObject();
            var constructor = CreateConstructor(length, function);

            // Put every function on the prototype
            for (int i = 0; i < functions.Length; ++i)
            {
                var name = functions[i].Name;
                var func = functions[i].Func;

                // Also, mark the function as immutable
                prototype.Put(name, func, DescriptorAttrs.Immutable);
            }

            // Put the prototype on the constructor and attach
            constructor.Put("prototype", prototype, DescriptorAttrs.Immutable);
            this.Context.SetGlobal(className, constructor);
        }

        /// <summary>
        /// Creates a native type and attaches it to this context.
        /// </summary>
        /// <param name="className">The name of the class to create.</param>
        /// <param name="constructor">The constructor to create.</param>
        /// <param name="functions">The named functions to attach.</param>
        private void CreateType(string className, Func<FunctionObject, ScriptObject, ScriptObject> constructor, params NamedFunctionObject[] functions)
        {
            CreateType<Func<FunctionObject, ScriptObject, ScriptObject>>(className, 0, constructor, functions);
        }


        /// <summary>
        /// Creates a native type and attaches it to this context.
        /// </summary>
        /// <typeparam name="T1">The parameter to pass to the constructor of the type.</typeparam>
        /// <param name="className">The name of the class to create.</param>
        /// <param name="constructor">The constructor to create.</param>
        /// <param name="functions">The named functions to attach.</param>
        private void CreateType<T>(string className, Func<FunctionObject, ScriptObject, T, ScriptObject> constructor, params NamedFunctionObject[] functions)
        {
            CreateType<Func<FunctionObject, ScriptObject, T, ScriptObject>>(className, 1, constructor, functions);
        }


        /// <summary>
        /// Creates a native type and attaches it to this context.
        /// </summary>
        /// <typeparam name="T1">The parameter to pass to the constructor of the type.</typeparam>
        /// <typeparam name="T2">The parameter to pass to the constructor of the type.</typeparam>
        /// <param name="className">The name of the class to create.</param>
        /// <param name="constructor">The constructor to create.</param>
        /// <param name="functions">The named functions to attach.</param>
        private void CreateType<T1, T2>(string className, Func<FunctionObject, ScriptObject, T1, T2, ScriptObject> constructor, params NamedFunctionObject[] functions)
        {
            CreateType<Func<FunctionObject, ScriptObject, T1, T2, ScriptObject>>(className, 2, constructor, functions);
        }


        /// <summary>
        /// Creates a native type and attaches it to this context.
        /// </summary>
        /// <typeparam name="T1">The parameter to pass to the constructor of the type.</typeparam>
        /// <typeparam name="T2">The parameter to pass to the constructor of the type.</typeparam>
        /// <typeparam name="T3">The parameter to pass to the constructor of the type.</typeparam>
        /// <param name="className">The name of the class to create.</param>
        /// <param name="constructor">The constructor to create.</param>
        /// <param name="functions">The named functions to attach.</param>
        private void CreateType<T1, T2, T3>(string className, Func<FunctionObject, ScriptObject, T1, T2, T3, ScriptObject> constructor, params NamedFunctionObject[] functions)
        {
            CreateType<Func<FunctionObject, ScriptObject, T1, T2, T3, ScriptObject>>(className, 3, constructor, functions);
        }


        /// <summary>
        /// Creates a native type and attaches it to this context.
        /// </summary>
        /// <typeparam name="T1">The parameter to pass to the constructor of the type.</typeparam>
        /// <typeparam name="T2">The parameter to pass to the constructor of the type.</typeparam>
        /// <typeparam name="T3">The parameter to pass to the constructor of the type.</typeparam>
        /// <typeparam name="T4">The parameter to pass to the constructor of the type.</typeparam>
        /// <param name="className">The name of the class to create.</param>
        /// <param name="constructor">The constructor to create.</param>
        /// <param name="functions">The named functions to attach.</param>
        private void CreateType<T1, T2, T3, T4>(string className, Func<FunctionObject, ScriptObject, T1, T2, T3, T4, ScriptObject> constructor, params NamedFunctionObject[] functions)
        {
            CreateType<Func<FunctionObject, ScriptObject, T1, T2, T3, T4, ScriptObject>>(className, 4, constructor, functions);
        }


        /// <summary>
        /// Creates a native type and attaches it to this context.
        /// </summary>
        /// <typeparam name="T1">The parameter to pass to the constructor of the type.</typeparam>
        /// <typeparam name="T2">The parameter to pass to the constructor of the type.</typeparam>
        /// <typeparam name="T3">The parameter to pass to the constructor of the type.</typeparam>
        /// <typeparam name="T4">The parameter to pass to the constructor of the type.</typeparam>
        /// <typeparam name="T5">The parameter to pass to the constructor of the type.</typeparam>
        /// <param name="className">The name of the class to create.</param>
        /// <param name="constructor">The constructor to create.</param>
        /// <param name="functions">The named functions to attach.</param>
        private void CreateType<T1, T2, T3, T4, T5>(string className, Func<FunctionObject, ScriptObject, T1, T2, T3, T4, T5, ScriptObject> constructor, params NamedFunctionObject[] functions)
        {
            CreateType<Func<FunctionObject, ScriptObject, T1, T2, T3, T4, T5, ScriptObject>>(className, 5, constructor, functions);
        }

        /// <summary>
        /// Creates a native type and attaches it to this context.
        /// </summary>
        /// <typeparam name="T1">The parameter to pass to the constructor of the type.</typeparam>
        /// <typeparam name="T2">The parameter to pass to the constructor of the type.</typeparam>
        /// <typeparam name="T3">The parameter to pass to the constructor of the type.</typeparam>
        /// <typeparam name="T4">The parameter to pass to the constructor of the type.</typeparam>
        /// <typeparam name="T5">The parameter to pass to the constructor of the type.</typeparam>
        /// <typeparam name="T6">The parameter to pass to the constructor of the type.</typeparam>
        /// <param name="className">The name of the class to create.</param>
        /// <param name="constructor">The constructor to create.</param>
        /// <param name="functions">The named functions to attach.</param>
        private void CreateType<T1, T2, T3, T4, T5, T6>(string className, Func<FunctionObject, ScriptObject, T1, T2, T3, T4, T5, T6, ScriptObject> constructor, params NamedFunctionObject[] functions)
        {
            CreateType<Func<FunctionObject, ScriptObject, T1, T2, T3, T4, T5, T6, ScriptObject>>(className, 6, constructor, functions);
        }


        /// <summary>
        /// Creates a native type and attaches it to this context.
        /// </summary>
        /// <param name="className">The name of the class to create.</param>
        /// <param name="constructor">The constructor to use for object creation.</param>
        /// <param name="functions">The named functions to attach.</param>
        public void CreateType<TType>(string className, Func<ScriptObject, TType> constructor, params NamedFunctionObject[] functions)
            where TType : ScriptObject
        {
            CreateType<Func<FunctionObject, ScriptObject, TType>>(className, 0,
                (type, instance) => constructor(type.GetT<ScriptObject>("prototype")),
                functions);
        }

        /// <summary>
        /// Creates a native type and attaches it to this context.
        /// </summary>
        /// <param name="className">The name of the class to create.</param>
        /// <param name="constructor">The constructor to use for object creation.</param>
        /// <param name="functions">The named functions to attach.</param>
        public void CreateType<TType>(string className, params NamedFunctionObject[] functions)
            where TType : ScriptObject
        {
            CreateType<Func<FunctionObject, ScriptObject, TType>>(className, 0,
                (type, instance) => null,
                functions);
        }


        /// Creates a native type and attaches it to this context.
        /// </summary>
        /// <typeparam name="TType">The type of the object to create.</typeparam>
        /// <typeparam name="P1">The type of the parameter to pass to the constructor.</typeparam>
        /// <typeparam name="P2">The type of the parameter to pass to the constructor.</typeparam>
        /// <param name="className">The name of the class to create</param>
        /// <param name="constructor">The constructor to use for object creation</param>
        /// <param name="functions">The named functions to attach</param>
        public void CreateType<TType, P1>(string className,
            Func<ScriptObject, P1, TType> constructor,
            params NamedFunctionObject[] functions)
            where TType : ScriptObject
        {
            CreateType<Func<FunctionObject, ScriptObject, P1, TType>>(className, 0,
                (type, instance, p1) => constructor(type.GetT<ScriptObject>("prototype"), p1),
                functions);
        }

        /// Creates a native type and attaches it to this context.
        /// </summary>
        /// <typeparam name="TType">The type of the object to create.</typeparam>
        /// <typeparam name="P1">The type of the parameter to pass to the constructor.</typeparam>
        /// <typeparam name="P2">The type of the parameter to pass to the constructor.</typeparam>
        /// <param name="className">The name of the class to create</param>
        /// <param name="constructor">The constructor to use for object creation</param>
        /// <param name="functions">The named functions to attach</param>
        public void CreateType<TType, P1, P2>(string className,
            Func<ScriptObject, P1, P2, TType> constructor,
            params NamedFunctionObject[] functions)
            where TType : ScriptObject
        {
            CreateType<Func<FunctionObject, ScriptObject, P1, P2, TType>>(className, 0,
                (type, instance, p1, p2) => constructor(type.GetT<ScriptObject>("prototype"), p1, p2),
                functions);
        }

        /// Creates a native type and attaches it to this context.
        /// </summary>
        /// <typeparam name="TType">The type of the object to create.</typeparam>
        /// <typeparam name="P1">The type of the parameter to pass to the constructor.</typeparam>
        /// <typeparam name="P2">The type of the parameter to pass to the constructor.</typeparam>
        /// <typeparam name="P3">The type of the parameter to pass to the constructor.</typeparam>
        /// <param name="className">The name of the class to create</param>
        /// <param name="constructor">The constructor to use for object creation</param>
        /// <param name="functions">The named functions to attach</param>
        public void CreateType<TType, P1, P2, P3>(string className,
            Func<ScriptObject, P1, P2, P3, TType> constructor,
            params NamedFunctionObject[] functions)
            where TType : ScriptObject
        {
            CreateType<Func<FunctionObject, ScriptObject, P1, P2, P3, TType>>(className, 0,
                (type, instance, p1, p2, p3) => constructor(type.GetT<ScriptObject>("prototype"), p1, p2, p3),
                functions);
        }

        /// Creates a native type and attaches it to this context.
        /// </summary>
        /// <typeparam name="TType">The type of the object to create.</typeparam>
        /// <typeparam name="P1">The type of the parameter to pass to the constructor.</typeparam>
        /// <typeparam name="P2">The type of the parameter to pass to the constructor.</typeparam>
        /// <typeparam name="P3">The type of the parameter to pass to the constructor.</typeparam>
        /// <typeparam name="P4">The type of the parameter to pass to the constructor.</typeparam>
        /// <param name="className">The name of the class to create</param>
        /// <param name="constructor">The constructor to use for object creation</param>
        /// <param name="functions">The named functions to attach</param>
        public void CreateType<TType, P1, P2, P3, P4>(string className,
            Func<ScriptObject, P1, P2, P3, P4, TType> constructor,
            params NamedFunctionObject[] functions)
            where TType : ScriptObject
        {
            CreateType<Func<FunctionObject, ScriptObject, P1, P2, P3, P4, TType>>(className, 0,
                (type, instance, p1, p2, p3, p4) => constructor(type.GetT<ScriptObject>("prototype"), p1, p2, p3, p4),
                functions);
        }

        /// Creates a native type and attaches it to this context.
        /// </summary>
        /// <typeparam name="TType">The type of the object to create.</typeparam>
        /// <typeparam name="P1">The type of the parameter to pass to the constructor.</typeparam>
        /// <typeparam name="P2">The type of the parameter to pass to the constructor.</typeparam>
        /// <typeparam name="P3">The type of the parameter to pass to the constructor.</typeparam>
        /// <typeparam name="P4">The type of the parameter to pass to the constructor.</typeparam>
        /// <typeparam name="P5">The type of the parameter to pass to the constructor.</typeparam>
        /// <param name="className">The name of the class to create</param>
        /// <param name="constructor">The constructor to use for object creation</param>
        /// <param name="functions">The named functions to attach</param>
        public void CreateType<TType, P1, P2, P3, P4, P5>(string className,
            Func<ScriptObject, P1, P2, P3, P4, P5, TType> constructor,
            params NamedFunctionObject[] functions)
            where TType : ScriptObject
        {
            CreateType<Func<FunctionObject, ScriptObject, P1, P2, P3, P4, P5, TType>>(className, 0,
                (type, instance, p1, p2, p3, p4, p5) => constructor(type.GetT<ScriptObject>("prototype"), p1, p2, p3, p4, p5),
                functions);
        }

        /// <summary>
        /// Creates a native type and attaches it to this context.
        /// </summary>
        /// <typeparam name="TType">The type of the object to create.</typeparam>
        /// <typeparam name="P1">The type of the parameter to pass to the constructor.</typeparam>
        /// <typeparam name="P2">The type of the parameter to pass to the constructor.</typeparam>
        /// <typeparam name="P3">The type of the parameter to pass to the constructor.</typeparam>
        /// <typeparam name="P4">The type of the parameter to pass to the constructor.</typeparam>
        /// <typeparam name="P5">The type of the parameter to pass to the constructor.</typeparam>
        /// <typeparam name="P6">The type of the parameter to pass to the constructor.</typeparam>
        /// <param name="className">The name of the class to create</param>
        /// <param name="constructor">The constructor to use for object creation</param>
        /// <param name="functions">The named functions to attach</param>
        public void CreateType<TType, P1, P2, P3, P4, P5, P6>(string className,
            Func<ScriptObject, P1, P2, P3, P4, P5, P6, TType> constructor,
            params NamedFunctionObject[] functions)
            where TType : ScriptObject
        {
            CreateType<Func<FunctionObject, ScriptObject, P1, P2, P3, P4, P5, P6, TType>>(className, 0,
                (type, instance, p1, p2, p3, p4, p5, p6) => constructor(type.GetT<ScriptObject>("prototype"), p1, p2, p3, p4, p5, p6),
                functions);
        }



        #endregion
    }

}
