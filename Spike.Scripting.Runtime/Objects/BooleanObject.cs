using System;

namespace Spike.Scripting.Runtime
{
    public sealed class BooleanObject : ValueObject
    {
        public BooleanObject(Environment env)
            : base(env, env.Maps.Boolean, env.Prototypes.Boolean)
        {
        }

        public override string ClassName
        {
            get { return "Boolean"; }
        }
    }
}
