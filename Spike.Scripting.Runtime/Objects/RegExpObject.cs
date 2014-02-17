using System;
using System.Text.RegularExpressions;

namespace Spike.Scripting.Runtime
{
    public sealed class RegExpObject : ScriptObject
    {
        private Regex regExp;
        private bool global;

        public RegExpObject(Environment env, string pattern, RegexOptions options, bool global)
            : base(env, env.Maps.RegExp, env.Prototypes.RegExp)
        {
            this.global = global;
            try
            {
                options = (options | RegexOptions.ECMAScript) & ~RegexOptions.Compiled;
                var key = Tuple.Create(options, pattern);
                this.regExp = env.RegExpCache.Lookup(key, () => new Regex(pattern, options | RegexOptions.Compiled));
            }
            catch (ArgumentException ex)
            {
                env.RaiseSyntaxError<object>(ex.Message);
                return;
            }
        }

        public RegExpObject(Environment env, string pattern)
            : this(env, pattern, RegexOptions.None, false)
        {
        }

        public override string ClassName
        {
            get { return "RegExp"; }
        }

        public bool Global
        {
            get { return this.global; }
        }

        public bool IgnoreCase
        {
            get
            {
                return (this.regExp.Options & RegexOptions.IgnoreCase) == RegexOptions.IgnoreCase;
            }
        }

        public bool MultiLine
        {
            get
            {
                return (this.regExp.Options & RegexOptions.Multiline) == RegexOptions.Multiline;
            }
        }

        public Regex RegExp
        {
            get { return this.regExp; }
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
