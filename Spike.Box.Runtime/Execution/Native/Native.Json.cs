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
        /// <param name="withOid">Whether we should add $i property to the serialized JSON.</param>
        /// <returns>Serialized value.</returns>
        public static BoxedValue Serialize(Env environment, BoxedValue value, bool withOid)
        {
            // If undefined or null, return a boxed null value
            if (value.IsUndefined || value.IsNull)
                return Env.BoxedNull;

            try
            {
                // Serialize 
                var json = JsonConvert.SerializeObject(
                    Native.JsonWrite(value, 0, withOid)
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
        private static object JsonWrite(BoxedValue value, int depth, bool withOid)
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

            // If it's an array, serialize the array and add $i at the very end
            if (value.IsArray)
            {
                // Write all the elements of the array first
                var obj = new List<object>();
                for (uint i = 0; i < value.Array.Length; ++i)
                    obj.Add(Native.JsonWrite(value.Array.Get(i), ++depth, withOid));

                // Do we have to add the id?
                if (withOid && value.Array.Oid != 0)
                    obj.Add(value.Array.Oid);
                return obj;
            }

            // If it's an object
            if(value.IsStrictlyObject)
            {
                // A date should be serialized differently
                if (value.Object is DateObject)
                    return (value.Object as DateObject).Date;

                // An object can be simply serialized as a collection of fields
                var obj = new Dictionary<string, object>();
                foreach (var propertyName in value.Object.Members.Keys)
                {
                    if(withOid || propertyName != "$i")
                        obj.Add(propertyName, Native.JsonWrite(value.Object.Get(propertyName), ++depth, withOid));
                }
                return obj;
            }


            return null;

        }


    }
}
