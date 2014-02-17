using System;

namespace Spike.Scripting.Runtime
{
    public sealed class DateObject : ScriptObject
    {
        public DateObject(Environment env, DateTime date)
            : base(env, env.Maps.Base, env.Prototypes.Date)
        {
            this.Date = date;
        }

        public DateObject(Environment env, long ticks)
            : this(env, TicksToDateTime(ticks))
        {
        }

        public DateObject(Environment env, double ticks)
            : this(env, TicksToDateTime(ticks))
        {
        }

        private static long offset = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks;
        private static long tickScale = 10000L;

        public override string ClassName
        {
            get { return "Date"; }
        }

        public DateTime Date { get; set; }

        public bool HasValidDate
        {
            get { return this.Date != DateTime.MinValue; }
        }

        public override BoxedValue DefaultValue(DefaultValueHint hint)
        {
            if (hint == DefaultValueHint.None)
            {
                hint = DefaultValueHint.String;
            }

            return base.DefaultValue(hint);
        }

        public static DateTime TicksToDateTime(long ticks)
        {
            return new DateTime(ticks * tickScale + offset, DateTimeKind.Utc);
        }

        public static DateTime TicksToDateTime(double ticks)
        {
            if (double.IsNaN(ticks) || double.IsInfinity(ticks))
            {
                return DateTime.MinValue;
            }

            return TicksToDateTime((long)ticks);
        }

        public static long DateTimeToTicks(DateTime date)
        {
            return (date.ToUniversalTime().Ticks - offset) / tickScale;
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
