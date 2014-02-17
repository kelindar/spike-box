using System;
using Spike.Scripting.Runtime;
using Microsoft.FSharp.Collections;

namespace Spike.Scripting.Runtime
{
    using DynamicScope = FSharpList<Tuple<int, ScriptObject>>;

    public delegate Delegate FunctionCompiler(FunctionObject self, Type type);
    public delegate Object GlobalCode(FunctionObject self, ScriptObject @this);
    public delegate Object EvalCode(
        FunctionObject self,
        ScriptObject @this,
        BoxedValue[] privateScope,
        BoxedValue[] sharedScope,
        DynamicScope scope
    );
}
