using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.FSharp.Collections;
#if !CLR2
using System.Dynamic;
#endif

namespace Spike.Scripting.Runtime
{
    using DynamicScope = FSharpList<Tuple<int, ScriptObject>>;

    public class FunctionObject : ScriptObject
    {
        public FunctionMetaData MetaData;
        public DynamicScope DynamicScope;
        public BoxedValue[] SharedScope;
        public readonly BoxedValue[] ReusablePrivateScope;

        public FunctionObject(Environment env, ulong id, BoxedValue[] sharedScope, DynamicScope dynamicScope)
            : base(env, env.Maps.Function, env.Prototypes.Function)
        {
            MetaData = env.GetFunctionMetaData(id);
            SharedScope = sharedScope;
            DynamicScope = dynamicScope;
        }

        public FunctionObject(Environment env, FunctionMetaData metaData, Schema schema) :
            base(env, schema, env.Prototypes.Function)
        {
            MetaData = metaData;
            SharedScope = new BoxedValue[0];
			DynamicScope = FSharpList<Tuple<int, ScriptObject>>.Empty;
        }

        public FunctionObject(Environment env)
            : base(env)
        {
            MetaData = env.GetFunctionMetaData(0UL);
            SharedScope = null;
			DynamicScope = FSharpList<Tuple<int, ScriptObject>>.Empty;
        }

        public override string ClassName
        {
            get
            {
                return "Function";
            }
        }

        public string Name
        {
            get
            {
                return MetaData.Name;
            }
        }

        #region Public Members (Optimization)
        public sealed override void Put(string name, BoxedValue value)
        {
            int index = 0;
            if (this.CanPut(name, out index))
            {
                this.Properties[index].Value = value;
                this.Properties[index].HasValue = true;
            }
        }

        public sealed override void Put(string name, object value, uint tag)
        {
            int index = 0;
            if (this.CanPut(name, out index))
            {
                this.Properties[index].Value.Clr = value;
                this.Properties[index].Value.Tag = tag;
                this.Properties[index].HasValue = true;
            }
        }

        public sealed override void Put(string name, double value)
        {
            int index = 0;
            if (this.CanPut(name, out index))
            {
                this.Properties[index].Value.Number = value;
                this.Properties[index].HasValue = true;
            }
        }

        public sealed override bool Delete(string name)
        {
            int index = 0;
            if (!this.PropertySchema.IndexMap.TryGetValue(name, out index))
            {
                return true;
            }

            if ((this.Properties[index].Attributes & DescriptorAttrs.DontDelete) == DescriptorAttrs.None)
            {
                this.PropertySchema = this.PropertySchema.Delete(name);
                this.Properties[index] = new Descriptor();
                return true;
            }

            return false;
        }
        #endregion

        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            var boxedArgs = args.Select(x => BoxedValue.Box(x)).ToArray();
            result = Call(Env.Globals, boxedArgs).UnboxObject();
            return true;
        }

        public ScriptObject InstancePrototype
        {
            get
            {
                var prototype = Get("prototype");
                switch (prototype.Tag)
                {
                    case TypeTags.Function:
                    case TypeTags.Object:
                        return prototype.Object;

                    default:
                        return Env.Prototypes.Object;
                }
            }
        }

        public ScriptObject NewInstance()
        {
            ScriptObject o = Env.NewObject();
            o.Prototype = InstancePrototype;
            return o;
        }

        public bool HasInstance(ScriptObject v)
        {
            var o = Get("prototype");

            if (!o.IsObject)
                return Env.RaiseTypeError<bool>("prototype property is not an object");

            v = (v != null) ? v.Prototype : null;

            while (v != null)
            {
                if (Object.ReferenceEquals(o.Object, v))
                    return true;

                v = v.Prototype;
            }

            return false;
        }

        public BoxedValue Call(ScriptObject @this)
        {
            return
                MetaData
                    .GetDelegate<Func<FunctionObject, ScriptObject, BoxedValue>>(this)
                    .Invoke(this, @this);
        }

        public BoxedValue Call<T0>(ScriptObject @this, T0 a0)
        {
            return
                MetaData
                    .GetDelegate<Func<FunctionObject, ScriptObject, T0, BoxedValue>>(this)
                    .Invoke(this, @this, a0);
        }

        public BoxedValue Call<T0, T1>(ScriptObject @this, T0 a0, T1 a1)
        {
            return
                MetaData
                    .GetDelegate<Func<FunctionObject, ScriptObject, T0, T1, BoxedValue>>(this)
                    .Invoke(this, @this, a0, a1);
        }

        public BoxedValue Call<T0, T1, T2>(ScriptObject @this, T0 a0, T1 a1, T2 a2)
        {
            return
                MetaData
                    .GetDelegate<Func<FunctionObject, ScriptObject, T0, T1, T2, BoxedValue>>(this)
                    .Invoke(this, @this, a0, a1, a2);
        }

        public BoxedValue Call<T0, T1, T2, T3>(ScriptObject @this, T0 a0, T1 a1, T2 a2, T3 a3)
        {
            return
                MetaData
                    .GetDelegate<Func<FunctionObject, ScriptObject, T0, T1, T2, T3, BoxedValue>>(this)
                    .Invoke(this, @this, a0, a1, a2, a3);
        }

        public BoxedValue Call(ScriptObject @this, BoxedValue[] args)
        {
            return
                MetaData
                    .GetDelegate<Func<FunctionObject, ScriptObject, BoxedValue[], BoxedValue>>(this)
                    .Invoke(this, @this, args);
        }

        public BoxedValue Construct()
        {
            switch (MetaData.FunctionType)
            {
                case FunctionType.NativeConstructor:
                    return Call(null);

                case FunctionType.UserDefined:
                    var o = NewInstance();
                    return PickReturnObject(Call(o), o);

                default:
                    return Env.RaiseTypeError<BoxedValue>();
            }
        }

        public BoxedValue Construct<T0>(T0 a0)
        {
            switch (MetaData.FunctionType)
            {
                case FunctionType.NativeConstructor:
                    return Call(null, a0);

                case FunctionType.UserDefined:
                    var o = NewInstance();
                    return PickReturnObject(Call(o, a0), o);

                default:
                    return Env.RaiseTypeError<BoxedValue>();
            }
        }

        public BoxedValue Construct<T0, T1>(T0 a0, T1 a1)
        {
            switch (MetaData.FunctionType)
            {
                case FunctionType.NativeConstructor:
                    return Call(null, a0, a1);

                case FunctionType.UserDefined:
                    var o = NewInstance();
                    return PickReturnObject(Call(o, a0, a1), o);

                default:
                    return Env.RaiseTypeError<BoxedValue>();
            }
        }

        public BoxedValue Construct<T0, T1, T2>(T0 a0, T1 a1, T2 a2)
        {
            switch (MetaData.FunctionType)
            {
                case FunctionType.NativeConstructor:
                    return Call(null, a0, a1, a2);

                case FunctionType.UserDefined:
                    var o = NewInstance();
                    return PickReturnObject(Call(o, a0, a1, a2), o);

                default:
                    return Env.RaiseTypeError<BoxedValue>();
            }
        }

        public BoxedValue Construct<T0, T1, T2, T3>(T0 a0, T1 a1, T2 a2, T3 a3)
        {
            switch (MetaData.FunctionType)
            {
                case FunctionType.NativeConstructor:
                    return Call(null, a0, a1, a2, a3);

                case FunctionType.UserDefined:
                    var o = NewInstance();
                    return PickReturnObject(Call(o, a0, a1, a2, a3), o);

                default:
                    return Env.RaiseTypeError<BoxedValue>();
            }
        }

        public BoxedValue Construct(BoxedValue[] args)
        {
            switch (MetaData.FunctionType)
            {
                case FunctionType.NativeConstructor:
                    return Call(null, args);

                case FunctionType.UserDefined:
                    var o = NewInstance();
                    return PickReturnObject(Call(o, args), o);

                default:
                    return Env.RaiseTypeError<BoxedValue>();
            }
        }

        public BoxedValue PickReturnObject(BoxedValue r, ScriptObject o)
        {
            switch (r.Tag)
            {
                case TypeTags.Function: return BoxedValue.Box(r.Func);
                case TypeTags.Object: return BoxedValue.Box(r.Object);
                default: return BoxedValue.Box(o);
            }
        }
    }
}
