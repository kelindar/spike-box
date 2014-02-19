#region Copyright (c) 2009-2013 Misakai Ltd.
/*************************************************************************
 * 
 * ROMAN ATACHIANTS - CONFIDENTIAL
 * ===============================
 * 
 * THIS PROGRAM IS CONFIDENTIAL  AND PROPRIETARY TO  ROMAN  ATACHIANTS AND 
 * MAY  NOT  BE  REPRODUCED,  PUBLISHED  OR  DISCLOSED TO  OTHERS  WITHOUT 
 * ROMAN ATACHIANTS' WRITTEN AUTHORIZATION.
 *
 * COPYRIGHT (c) 2009 - 2012. THIS WORK IS UNPUBLISHED.
 * All Rights Reserved.
 * 
 * NOTICE:  All information contained herein is,  and remains the property 
 * of Roman Atachiants  and its  suppliers,  if any. The  intellectual and 
 * technical concepts contained herein are proprietary to Roman Atachiants
 * and  its suppliers and may be  covered  by U.S.  and  Foreign  Patents, 
 * patents in process, and are protected by trade secret or copyright law.
 * 
 * Dissemination of this information  or reproduction  of this material is 
 * strictly  forbidden  unless prior  written permission  is obtained from 
 * Roman Atachiants.
*************************************************************************/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using Spike.Network;
using Spike.Network.Http;
using Spike.Box;
using Spike;
using Spike.Scripting.Runtime;

namespace System.Linq
{
    /// <summary>
    /// Represents a script extension methods.
    /// </summary>
    internal static class ScriptExtension
    {
        #region Extension: References

        /// <summary>
        /// Boxes the object inside a weak reference acessible from JavaScript.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static ReferenceObject<T> AsReference<T>(this T source, Spike.Scripting.Runtime.Environment environment)
            where T : class
        {
            return new ReferenceObject<T>(environment, source);
        }

        /// <summary>
        /// Boxes the object inside a weak reference acessible from JavaScript.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static ClientObject AsReference(this IClient source, Spike.Scripting.Runtime.Environment environment)
        {
            return new ClientObject(environment, source);
        }

        /// <summary>
        /// Boxes the object inside a weak reference acessible from JavaScript.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static ClientObject AsReference(this IClient source, ScriptContext context)
        {
            return new ClientObject(context, source);
        }
        #endregion

        #region Extension: Cryptography
        private const UInt32 Seed = 37;

        /// <summary>
        /// Computes MurmurHash3 on this set of bytes and returns the calculated hash value.
        /// </summary>
        /// <param name="data">The data to compute the hash of.</param>
        /// <returns>A 32bit hash value.</returns>
        public static byte[] GetMurmurBytes(this string data)
        {
            // Compute the hash
            return Encoding.UTF8
                .GetBytes(data)
                .GetMurmurBytes();
        }

        /// <summary>
        /// Computes MurmurHash3 on this set of bytes and returns the calculated hash value.
        /// </summary>
        /// <param name="data">The data to compute the hash of.</param>
        /// <returns>A 32bit hash value.</returns>
        public static uint GetMurmurInt(this string data)
        {
            return BitConverter.ToUInt32(GetMurmurBytes(data), 0);
        }

        /// <summary>
        /// Computes MurmurHash3 on this set of bytes and returns the calculated hash value.
        /// </summary>
        /// <param name="data">The data to compute the hash of.</param>
        /// <returns>A 32bit hash value.</returns>
        public static string GetMurmurHash(this byte[] data)
        {
            // Compute the hash
            var bytes = data.GetMurmurBytes();

            // Convert to string
            char[] chars = new char[bytes.Length * 2];
            byte current;
            for (int y = 0, x = 0; y < bytes.Length; ++y, ++x)
            {
                current = ((byte)(bytes[y] >> 4));
                chars[x] = (char)(current > 9 ? current + 0x37 : current + 0x30);
                current = ((byte)(bytes[y] & 0xF));
                chars[++x] = (char)(current > 9 ? current + 0x37 : current + 0x30);
            }

            // Get the hash of the string representation
            return new string(chars);
        }

        /// <summary>
        /// Computes MurmurHash3 on this set of bytes and returns the calculated hash value.
        /// </summary>
        /// <param name="data">The data to compute the hash of.</param>
        /// <returns>A 32bit hash value.</returns>
        public static byte[] GetMurmurBytes(this byte[] data)
        {
            const UInt32 c1 = 0xcc9e2d51;
            const UInt32 c2 = 0x1b873593;


            int curLength = data.Length; /* Current position in byte array */
            int length = curLength;   /* the const length we need to fix tail */
            UInt32 h1 = Seed;
            UInt32 k1 = 0;

            /* body, eat stream a 32-bit int at a time */
            Int32 currentIndex = 0;
            while (curLength >= 4)
            {
                /* Get four bytes from the input into an UInt32 */
                k1 = (UInt32)(data[currentIndex++]
                  | data[currentIndex++] << 8
                  | data[currentIndex++] << 16
                  | data[currentIndex++] << 24);

                /* bitmagic hash */
                k1 *= c1;
                k1 = Rotl32(k1, 15);
                k1 *= c2;

                h1 ^= k1;
                h1 = Rotl32(h1, 13);
                h1 = h1 * 5 + 0xe6546b64;
                curLength -= 4;
            }

            /* tail, the reminder bytes that did not make it to a full int */
            /* (this switch is slightly more ugly than the C++ implementation 
             * because we can't fall through) */
            switch (curLength)
            {
                case 3:
                    k1 = (UInt32)(data[currentIndex++]
                      | data[currentIndex++] << 8
                      | data[currentIndex++] << 16);
                    k1 *= c1;
                    k1 = Rotl32(k1, 15);
                    k1 *= c2;
                    h1 ^= k1;
                    break;
                case 2:
                    k1 = (UInt32)(data[currentIndex++]
                      | data[currentIndex++] << 8);
                    k1 *= c1;
                    k1 = Rotl32(k1, 15);
                    k1 *= c2;
                    h1 ^= k1;
                    break;
                case 1:
                    k1 = (UInt32)(data[currentIndex++]);
                    k1 *= c1;
                    k1 = Rotl32(k1, 15);
                    k1 *= c2;
                    h1 ^= k1;
                    break;
            };

            // finalization, magic chants to wrap it all up
            h1 ^= (UInt32)length;
            h1 = Mix(h1);

            // convert back to 4 bytes
            byte[] key = new byte[4];
            key[0] = (byte)(h1);
            key[1] = (byte)(h1 >> 8);
            key[2] = (byte)(h1 >> 16);
            key[3] = (byte)(h1 >> 24);
            return key;
        }

        private static UInt32 Rotl32(UInt32 x, byte r)
        {
            return (x << r) | (x >> (32 - r));
        }

        private static UInt32 Mix(UInt32 h)
        {
            h ^= h >> 16;
            h *= 0x85ebca6b;
            h ^= h >> 13;
            h *= 0xc2b2ae35;
            h ^= h >> 16;
            return h;
        }
        

        #endregion

        #region Extension: String & Text
        /// <summary>
        /// Uppercase first letter.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string UppercaseFirst(this string source)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }
            // Return char and concat substring.
            return char.ToUpper(source[0]) + source.Substring(1);
        }

        /// <summary>
        /// Uppercase first letter.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string AsDirectiveName(this string source)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(source))
                return string.Empty;

            // Remove the extension
            source = source.Replace(MetaExtension.Template, String.Empty);

            var capitalize = false;
            var writer = new StringBuilder();
            for (int i = 0; i < source.Length; ++i)
            {
                var ch = source[i];
                if(ch == '-') 
                {
                    // If the character is a dash, we must capitalize the next one
                    capitalize = true; 
                    continue;
                }

                // If we ask to capitalize, do it and set back the flag
                if (capitalize)
                {
                    ch = Char.ToUpper(ch);
                    capitalize = false;
                }

                // Append to the string
                writer.Append(ch);
            }

            // Return char and concat substring.
            return writer.ToString();
        }


        /// <summary>
        /// Converts a string to a session name.
        /// </summary>
        /// <returns>Returns a session name string.</returns>
        public static string AsSessionName(this string source)
        {
            // Simply prefixes with a value
            return "sid_" + source;
        }

        /// <summary>
        /// Gets the session name from the context.
        /// </summary>
        /// <param name="context">The context containing the cookie.</param>
        /// <returns>Returns null if session name was not found.</returns>
        public static string GetSessionName(this HttpContext context)
        {
            // Get the cookie value
            var cookie = context.Request.Cookies.Get("spike-session");
            if (cookie == null)
                return null;

            // Get the session scope name
            return cookie.Value.AsSessionName();
        }

        /// <summary>
        /// Gets whether the name of the function is private or not.
        /// </summary>
        /// <param name="source">The string to check.</param>
        /// <returns>Whether it is private or not</returns>
        public static bool IsPrivateName(this string source)
        {
            if (String.IsNullOrEmpty(source))
                return true;
            return source[0] == '_';
        }
        #endregion

        #region Extension: Arrays
        /// <summary>
        /// Converts the values of an array object to a CLR array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static T[] ToArray<T>(this ArrayObject source)
        {
            var length = source.Length;
            var array  = new T[length];
            for (int j = 0; j < length; ++j)
                array[j] = (T)TypeConverter.ToClrObject(source.Get(j));
            return array;
        }

        /// <summary>
        /// Converts an array to an array of children, pushing only the specific type of the children to the array.
        /// </summary>
        /// <typeparam name="TBase">The base type of the array.</typeparam>
        /// <typeparam name="T">The child type of the array.</typeparam>
        /// <param name="array">The array instance.</param>
        /// <returns>The result</returns>
        public static T[] ToArray<TBase, T>(this TBase[] array)
            where T : TBase
        {
            var subArray = new List<T>();
            foreach (var item in array)
            {
                if (item is T)
                    subArray.Add((T)item);
            }
            return subArray.ToArray();
        }
        #endregion

        #region Extension: Network
        /// <summary>
        /// Transmits an event to the remote client.
        /// </summary>
        /// <param name="client">The client to transmit the event to.</param>
        /// <param name="type">The type of the event that occured.</param>
        /// <param name="target">The target object for that event.</param>
        /// <param name="name">The name of the event that occured</param>
        /// <param name="value">The value of the event to transmit.</param>
        internal static void TransmitEvent(this IClient client, AppEventType type, int target, string name, string value)
        {
            // Prepare and send the packet
            var packet = EventInform.Metadata.AcquireInform() as EventInform;
            packet.Type = (byte)type;
            packet.Name = name;
            packet.Target = target;
            packet.Value = value;
            client.Send(packet);
        }
        #endregion
    }
}
