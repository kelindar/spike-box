using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spike.Scripting.Runtime;
using Spike.Text.Json;
using Env = Spike.Scripting.Runtime.Environment;

namespace Spike.Box
{
    public partial class Native
    {

        /// <summary>
        /// Serializes a value in the JSON format.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <returns>Serialized value.</returns>
        public static BoxedValue Serialize(Env environment, BoxedValue value)
        {
            // If undefined or null, return a boxed null value
            if (value.IsUndefined || value.IsNull)
                return Env.BoxedNull;

            try
            {
                // Serialize 
                var json = JsonConvert.SerializeObject(
                    Native.JsonWrite(value, 0)
                    );

                // Box the value
                return BoxedValue.Box(json);
            }
            catch(Exception ex)
            {
                // Something went wrong during the serialize
                Service.Logger.Log(ex);

                // Return null
                return Env.BoxedNull;
            }
        }

        /// <summary>
        /// Write a JSON value to an appropriate object.
        /// </summary>
        private static object JsonWrite(BoxedValue value, int depth)
        {
            if (depth > 100)
                throw new StackOverflowException("Json serialization has exceeded the allowed depth, possibly due to a recursion.");

            if (value.IsNumber)
                return TypeConverter.ToNumber(value);
            if(value.IsString)
                return TypeConverter.ToString(value);
            if (value.IsBoolean)
                return TypeConverter.ToBoolean(value);
            if (value.IsNull || value.IsUndefined)
                return null;

            if (value.IsArray)
            {
                var obj = new List<object>();
                for (uint i = 0; i < value.Array.Length; ++i)
                    obj.Add(Native.JsonWrite(value.Array.Get(i), ++depth));
                if (value.Array.Oid != 0)
                    obj.Add(value.Array.Oid);
                return obj;
            }

            if(value.IsStrictlyObject)
            {
                var obj = new Dictionary<string, object>();
                foreach (var propertyName in value.Object.Members.Keys)
                    obj.Add(propertyName, Native.JsonWrite(value.Object.Get(propertyName), ++depth));
                return obj;
            }


            return null;

        }


    }
}
