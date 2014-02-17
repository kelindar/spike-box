using System;

namespace Spike.Scripting.Runtime
{
    public abstract class ValueObject : ScriptObject
    {
        public Descriptor Value;

        public ValueObject(Environment env, Schema map, ScriptObject prototype)
            : base(env, map, prototype)
        {
        }

        public static BoxedValue GetValue(ScriptObject o)
        {
            var vo = o as ValueObject;

            if (vo == null)
                return o.Env.RaiseTypeError<BoxedValue>("Cannot read the value of a non-value object.");

            return vo.Value.Value;
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

    }
}
