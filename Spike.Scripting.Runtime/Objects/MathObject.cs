using System;

namespace Spike.Scripting.Runtime
{
    public sealed class MathObject : ScriptObject
    {
        public MathObject(Environment env)
            : base(env, env.Maps.Base, env.Prototypes.Object)
        {

        }

        public override string ClassName
        {
            get { return "Math"; }
        }
    }
}
