using System;
using System.Linq;
using Spike.Network;
using Spike.Scripting;
using Spike.Scripting.Runtime;
using Spike.Scripting.Compiler;
using Spike.Scripting.Hosting;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.IO;
using Spike.Scripting.Native;
using Spike.Text;


namespace Spike.Box
{
    /// <summary>
    /// The Buffer class is a global type for dealing with binary data directly. It can be constructed in a variety of ways.
    /// </summary>
    public class BufferObject : BaseObject
    {
        #region Constructors
        /// <summary>
        /// The array segment buffer.
        /// </summary>
        private readonly ArraySegment<byte> Buffer;

        /// <summary>
        /// Temporary static buffer, for formatting.
        /// </summary>
        [ThreadStatic]
        private static byte[] Temp = null;

        /// <summary>
        /// Creates a new instance of an object.
        /// </summary>
        /// <param name="prototype">The prototype to use.</param>
        /// <param name="param">Either the size of the buffer, a string or the array of octets.</param>
        /// <param name="encodingName">The encoding of a string.</param>
        public BufferObject(ScriptObject prototype, BoxedValue param, BoxedValue encodingName)
            : base(prototype)
        {
            if (param.IsNumber)
            {
                // Create a new array with the provided size
                this.Buffer = new ArraySegment<byte>(
                    new byte[(int)param.Number]
                    );
                this.Put("length", this.Buffer.Count, DescriptorAttrs.ReadOnly);
                return;
            }
            
            if (param.IsString)
            {
                // The encoding to use
                Encoding encoding = encodingName.IsString 
                    ? TextEncoding.GetEncoding(encodingName.String)
                    : null;

                // Defaults to UTF8
                if (encoding == null)
                    encoding = TextEncoding.UTF8;

                // Decode
                this.Buffer = new ArraySegment<byte>(encoding.GetBytes(param.String));
                this.Put("length", this.Buffer.Count, DescriptorAttrs.ReadOnly);
                return;
            }
            
            if (param.IsStrictlyObject && param.Object is ArrayObject)
            {
                // Allocate a new array
                var bytes = (param.Object as ArrayObject);
                this.Buffer = new ArraySegment<byte>(new byte[bytes.Length]);
                this.Put("length", this.Buffer.Count, DescriptorAttrs.ReadOnly);

                // Iterate through the array and convert each integer to a byte
                for (int i = 0; i < bytes.Length; ++i)
                {
                    // Get the number and convert to byte
                    var item = bytes.Get(i);
                    if (item.IsNumber)
                        this.Buffer.Array[i] = Convert.ToByte(item.Number);
                }
            }
        }

        /// <summary>
        /// Creates a new instance of an object.
        /// </summary>
        /// <param name="prototypeName">The name of the prototype to use.</param>
        /// <param name="param">Either the size of the buffer, a string or the array of octets.</param>
        /// <param name="encoding">The encoding of a string.</param>
        public BufferObject(ScriptContext context, BoxedValue param, BoxedValue encoding)
            : this(context.GetPrototype("Buffer"), param, encoding)
        {

        }

        /// <summary>
        /// Creates a new instance of an object.
        /// </summary>
        /// <param name="segment">The array segment to wrap.</param>
        /// <param name="env">The environment to fetch the prototype from.</param>
        public BufferObject(ArraySegment<byte> segment, Scripting.Runtime.Environment env)
            : base(env.Globals.GetT<ScriptObject>("Buffer").GetT<ScriptObject>("prototype"))
        {
            this.Buffer = segment;
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the class name for building the javascript name.
        /// </summary>
        public override string ClassName
        {
            get { return "Buffer"; }
        }

        /// <summary>
        /// Gets the underlying byte array.
        /// </summary>
        public byte[] Array
        {
            get { return this.Buffer.Array; }
        }

        /// <summary>
        /// Gets the length of the buffer.
        /// </summary>
        public int Count
        {
            get { return this.Buffer.Count; }
        }

        /// <summary>
        /// Gets the offset of the buffer.
        /// </summary>
        public int Offset
        {
            get { return this.Buffer.Offset; }
        }
        #endregion

        #region Public Indexer
        public override void Put(uint index, BoxedValue value)
        {
            if (value.IsNumber)
            {
                this.Put(index, value.Number);
            }
            else
            {
                base.Put(index, value);
            }
        }

        public override void Put(uint index, double value)
        {
            // If index is outside of bounds, throw an exception
            if (index < 0 || index > this.Count)
                throw new ArgumentOutOfRangeException("Index was out of range. Must be non-negative and less than the size of the Buffer.");

            // Simply assign to the buffer
            this.Buffer.Array[this.Offset + index] = (byte)value;
        }

        public override void Put(uint index, object value, uint tag)
        {
            base.Put(index, value, tag);
        }
        #endregion

        #region Public Members

        /// <summary>
        /// Decodes and returns a string from buffer data encoded with encoding (defaults to 'utf8') beginning at start (defaults to 0) and ending at end (defaults to buffer.length).
        /// </summary>
        /// <param name="ctx">The function context.</param>
        /// <param name="instance">The object instance.</param>
        /// <param name="encodingName">The encoding for the to string conversion.</param>
        /// <param name="start">The starting offset for the to string conversion.</param>
        /// <param name="end">The ending offset for the to string conversion.</param>
        internal static string ToString(FunctionObject ctx, ScriptObject instance, BoxedValue encodingName, BoxedValue start, BoxedValue end)
        {
            // Get the buffer
            var buffer = instance.CastTo<BufferObject>();

            // Attempt to get the encoding
            Encoding encoding = encodingName.IsString
                ? TextEncoding.GetEncoding(encodingName.String)
                : null;

            // Default the encoding to utf8
            if (encoding == null)
                encoding = TextEncoding.UTF8;

            // Start offset defaults at zero
            int startOffset = start.IsNumber
                ? (int)start.Number
                : 0;

            // End offset is calculated from start offset
            int endOffset = end.IsNumber
                ? (int)end.Number
                : buffer.Count - startOffset;

            // Validate the range
            if (startOffset < 0 || endOffset > buffer.Count)
                throw new ArgumentOutOfRangeException("Index was out of range. Must be non-negative and less than the size of the Buffer.");

            // As we are using a segment, we need to use the appropriate offset
            startOffset += buffer.Offset;
            endOffset += buffer.Offset;

            // Size that needs to be converted
            var size = endOffset - startOffset;
            var chars = new char[size];


            // Copy bytes to a char array
            System.Array.Copy(buffer.Array, startOffset, chars, 0, size);
            
            // Return a string
            return TextEncoding.UTF8.GetString(
                encoding.GetBytes(chars)
                );
        }

        /// <summary>
        /// Returns a JSON-representation of the Buffer instance, which is identical to the output for JSON Arrays. JSON.stringify implicitly calls this function when stringifying a Buffer instance.
        /// </summary>
        /// <param name="ctx">The function context.</param>
        /// <param name="instance">The object instance.</param>
        /// <returns>Returns a JSON-representation of the Buffer instance.</returns>
        internal static string ToJSON(FunctionObject ctx, ScriptObject instance)
        {
            // Get the buffer
            var buffer = instance.CastTo<BufferObject>();
            var array  = buffer.Array;
            var offset = buffer.Offset;
            var end    = buffer.Offset + buffer.Count;

            var writer = new StringBuilder();
            writer.Append('[');
            try
            {
                for (int i = offset; i < end; ++i)
                {
                    writer.Append(array[i]);
                    if (i < (array.Length - 1))
                        writer.Append(',');
                }
            }
            catch { }
            writer.Append(']');
            return writer.ToString();
        }


        /// <summary>
        /// ReturnsWrites string to the buffer at offset using the given encoding. offset defaults to 0, encoding 
        /// defaults to 'utf8'. length is the number of bytes to write. Returns number of octets written. If buffer
        /// did not contain enough space to fit the entire string, it will write a partial amount of the string. 
        /// length defaults to buffer.length - offset. The method will not write partial characters.
        /// </summary>
        /// <param name="ctx">The function context.</param>
        /// <param name="instance">The object instance.</param>
        /// <param name="text">Data to be written to buffer</param>
        /// <param name="offset">The starting offset.</param>
        /// <param name="length">The amount of characters to write.</param>
        /// <param name="encoding">The encoding to use.</param>
        /// <returns>The number of characters written.</returns>
        internal static int Write(FunctionObject ctx, ScriptObject instance, string text, BoxedValue offset, BoxedValue length, BoxedValue encodingName)
        {
            // Get the buffer
            var buffer = instance.CastTo<BufferObject>();
            var array = buffer.Array;

            // Start offset defaults at zero
            int startOffset = offset.IsNumber
                ? (int)offset.Number
                : 0;

            // End offset is calculated from start offset
            int endOffset = length.IsNumber
                ? (int)length.Number
                : buffer.Count - startOffset;

            // Attempt to get the encoding
            Encoding encoding = encodingName.IsString
                ? TextEncoding.GetEncoding(encodingName.String)
                : null;

            // Default the encoding to utf8
            if (encoding == null)
                encoding = TextEncoding.UTF8;

            // As we are using a segment, we need to use the appropriate offset
            startOffset += buffer.Offset;
            endOffset += buffer.Offset;

            // Write to the array
            int count = 0;
            for (int i = startOffset; i < endOffset; ++i, ++count)
                array[i] = Convert.ToByte(text[count]);

            // Put the amount of characters written
            buffer.Put("_charsWritten", count);

            // Return the amount of written characters
            return count;
        }


        /// <summary>
        /// Does copy between buffers. The source and target regions can be overlapped. targetStart and sourceStart default to 0. sourceEnd defaults to buffer.length.
        /// </summary>
        /// <param name="ctx">The function context.</param>
        /// <param name="instance">The object instance.</param>
        internal static void Copy(FunctionObject ctx, ScriptObject instance, BufferObject targetBuffer, BoxedValue targetStart, BoxedValue sourceStart, BoxedValue sourceEnd)
        {
            // Get the buffer
            var buffer = instance.CastTo<BufferObject>();

            // Get source and target arrays
            var source = buffer.Array;
            var target = targetBuffer.Array;

            // Start offset defaults to zero
            int targetStartOffset = targetStart.IsNumber
                ? (int)targetStart.Number
                : 0;

            // Start offset defaults to zero
            int sourceStartOffset = sourceStart.IsNumber
                ? (int)sourceStart.Number
                : 0;

            // End offset defaults to buffer.length
            int sourceEndOffset = sourceEnd.IsNumber
                ? (int)sourceEnd.Number
                : buffer.Count;

            // As we are using a segment, we need to use the appropriate offset
            sourceStartOffset += buffer.Offset;
            sourceEndOffset += buffer.Offset;
            targetStartOffset += targetBuffer.Offset;

            // Get the length
            var size = sourceEndOffset - sourceStartOffset;

            // Block copy from one array to another
            System.Buffer.BlockCopy(source, sourceStartOffset, target, targetStartOffset, size);
        }

        /// <summary>
        /// Returns a new buffer which references the same memory as the old, but offset and cropped by the start (defaults to 0) and end (defaults to buffer.length) indexes. Negative indexes start from the end of the buffer.
        /// </summary>
        /// <param name="ctx">The function context.</param>
        /// <param name="instance">The object instance.</param>
        internal static BufferObject Slice(FunctionObject ctx, ScriptObject instance,  BoxedValue start, BoxedValue end)
        {
            // Get the buffer
            var buffer = instance.CastTo<BufferObject>();
            var source = buffer.Array;

            // Start offset defaults to zero
            int startOffset = start.IsNumber
                ? (int)start.Number
                : 0;

            // End offset defaults to buffer.length
            int endOffset = end.IsNumber
                ? (int)end.Number
                : buffer.Count;

            // Validate the range
            if (startOffset < 0 || endOffset > buffer.Count)
                throw new ArgumentOutOfRangeException("Index was out of range. Must be non-negative and less than the size of the Buffer.");

            // As we are using a segment, we need to use the appropriate offset
            startOffset += buffer.Offset;
            endOffset += buffer.Offset;

            // Create the segment & get the prototype
            var segment = new ArraySegment<byte>(source, startOffset, endOffset - startOffset);

            // Return a new instance of a buffer
            return new BufferObject(segment, ctx.Env);
        }



        /// <summary>
        /// Decodes and returns a string from buffer data encoded with encoding (defaults to 'utf8') beginning at start (defaults to 0) and ending at end (defaults to buffer.length).
        /// </summary>
        /// <param name="ctx">The function context.</param>
        /// <param name="instance">The object instance.</param>
        /// <param name="fillValue">The value to fill.</param>
        /// <param name="start">The starting offset for the to string conversion.</param>
        /// <param name="end">The ending offset for the to string conversion.</param>
        internal static void Fill(FunctionObject ctx, ScriptObject instance, BoxedValue fillValue, BoxedValue start, BoxedValue end)
        {
            // Get the buffer
            var buffer = instance.CastTo<BufferObject>();

            // Start offset defaults at zero
            byte fill = fillValue.IsNumber
                ? (byte)fillValue.Number
                : (byte)0;

            // Start offset defaults at zero
            int startOffset = start.IsNumber
                ? (int)start.Number
                : 0;

            // End offset is calculated from start offset
            int endOffset = end.IsNumber
                ? (int)end.Number
                : buffer.Count - startOffset;

            // Validate the range
            if (startOffset < 0 || endOffset > buffer.Count)
                throw new ArgumentOutOfRangeException("Index was out of range. Must be non-negative and less than the size of the Buffer.");

            // As we are using a segment, we need to use the appropriate offset
            startOffset += buffer.Offset;
            endOffset += buffer.Offset;

            // Fill
            for (int i = startOffset; i < endOffset; ++i)
                buffer.Buffer.Array[i] = fill;
        }

        #endregion

        #region Write - Big Endian

        /// <summary>
        /// Writes a 1-byte unsigned integer value to the underlying stream.
        /// </summary>
        internal static void WriteUInt8(FunctionObject ctx, ScriptObject instance, BoxedValue number, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!number.IsNumber || !offset.IsNumber)
                throw new ArgumentException("Buffer attempted to write NaN value or on an unspecified offset.");

            // Convert
            var value = Convert.ToByte(number.Number);
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            // Write to the offset
            array[index] = value;
        }

        /// <summary>
        /// Writes a 1-byte signed integer value to the underlying stream.
        /// </summary>
        internal static void WriteInt8(FunctionObject ctx, ScriptObject instance, BoxedValue number, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!number.IsNumber || !offset.IsNumber)
                throw new ArgumentException("Buffer attempted to write NaN value or on an unspecified offset.");

            // Convert
            var value = Convert.ToSByte(number.Number);
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            array[index] = ((byte)value);
        }

        /// <summary>
        /// Writes a 2-byte signed integer value to the underlying stream.
        /// </summary>
        internal static void WriteInt16BE(FunctionObject ctx, ScriptObject instance, BoxedValue number, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!number.IsNumber || !offset.IsNumber)
                throw new ArgumentException("Buffer attempted to write NaN value or on an unspecified offset.");

            // Convert
            var value = Convert.ToInt16(number.Number);
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            array[index] = (byte)(value >> 8);
            array[index + 1] = (byte)value;

        }

        /// <summary>
        /// Writes a 2-byte unsigned integer value to the underlying stream.
        /// </summary>
        internal static void WriteUInt16BE(FunctionObject ctx, ScriptObject instance, BoxedValue number, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!number.IsNumber || !offset.IsNumber)
                throw new ArgumentException("Buffer attempted to write NaN value or on an unspecified offset.");

            // Convert
            var value = Convert.ToUInt16(number.Number);
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            array[index] = (byte)(value >> 8);
            array[index + 1] = (byte)value;

        }

        /// <summary>
        /// Writes a 4-byte signed integer value to the underlying stream.
        /// </summary>
        internal static void WriteInt32BE(FunctionObject ctx, ScriptObject instance, BoxedValue number, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!number.IsNumber || !offset.IsNumber)
                throw new ArgumentException("Buffer attempted to write NaN value or on an unspecified offset.");

            // Convert
            var value = Convert.ToInt32(number.Number);
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            array[index] = (byte)(value >> 24);
            array[index + 1] = (byte)(value >> 16);
            array[index + 2] = (byte)(value >> 8);
            array[index + 3] = (byte)value;
        }

        /// <summary>
        /// Writes a 4-byte unsigned integer value to the underlying stream.
        /// </summary>
        internal static void WriteUInt32BE(FunctionObject ctx, ScriptObject instance, BoxedValue number, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!number.IsNumber || !offset.IsNumber)
                throw new ArgumentException("Buffer attempted to write NaN value or on an unspecified offset.");

            // Convert
            var value = Convert.ToUInt32(number.Number);
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            array[index ] = (byte)(value >> 24);
            array[index + 1] = (byte)(value >> 16);
            array[index + 2] = (byte)(value >> 8);
            array[index + 3] = (byte)value;
        }

        /// <summary>
        /// Writes a 8-byte signed integer value to the underlying stream.
        /// </summary>
        internal static void WriteInt64BE(FunctionObject ctx, ScriptObject instance, BoxedValue number, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!number.IsNumber || !offset.IsNumber)
                throw new ArgumentException("Buffer attempted to write NaN value or on an unspecified offset.");

            // Convert
            var value = Convert.ToInt64(number.Number);
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            array[index] = (byte)(value >> 56);
            array[index + 1] = (byte)(value >> 48);
            array[index + 2] = (byte)(value >> 40);
            array[index + 3] = (byte)(value >> 32);
            array[index + 4] = (byte)(value >> 24);
            array[index + 5] = (byte)(value >> 16);
            array[index + 6] = (byte)(value >> 8);
            array[index + 7] = (byte)value;
        }

        /// <summary>
        /// Writes a 8-byte unsigned integer value to the underlying stream.
        /// </summary>
        internal static void WriteUInt64BE(FunctionObject ctx, ScriptObject instance, BoxedValue number, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!number.IsNumber || !offset.IsNumber)
                throw new ArgumentException("Buffer attempted to write NaN value or on an unspecified offset.");

            // Convert
            var value = Convert.ToUInt64(number.Number);
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            array[index] = (byte)(value >> 56);
            array[index + 1] = (byte)(value >> 48);
            array[index + 2] = (byte)(value >> 40);
            array[index + 3] = (byte)(value >> 32);
            array[index + 4] = (byte)(value >> 24);
            array[index + 5] = (byte)(value >> 16);
            array[index + 6] = (byte)(value >> 8);
            array[index + 7] = (byte)value;
        }

        /// <summary>
        /// Writes an IEEE 754 single-precision (32-bit) floating-point number to the buffer
        /// </summary>
        internal static unsafe void WriteFloatBE(FunctionObject ctx, ScriptObject instance, BoxedValue number, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!number.IsNumber || !offset.IsNumber)
                throw new ArgumentException("Buffer attempted to write NaN value or on an unspecified offset.");

            // Convert
            var value = Convert.ToSingle(number.Number);
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            // Make sure we have a spare buffer
            if (BufferObject.Temp == null)
                BufferObject.Temp = new byte[20];

            // Write unsafely directly
            fixed (byte* pBuffer = BufferObject.Temp)
                *((float*)(pBuffer)) = value;

            // Which way should we write?
            if (BitConverter.IsLittleEndian)
            {
                // We have to reverse
                array[index] = BufferObject.Temp[3];
                array[index + 1] = BufferObject.Temp[2];
                array[index + 2] = BufferObject.Temp[1];
                array[index + 3] = BufferObject.Temp[0];
            }
            else
            {
                // We have it in order
                array[index] = BufferObject.Temp[0];
                array[index + 1] = BufferObject.Temp[1];
                array[index + 2] = BufferObject.Temp[2];
                array[index + 3] = BufferObject.Temp[3];
            }
        }

        /// <summary>
        /// Writes an IEEE 754 double-precision (64-bit) floating-point number to the buffer
        /// </summary>
        internal static unsafe void WriteDoubleBE(FunctionObject ctx, ScriptObject instance, BoxedValue number, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!number.IsNumber || !offset.IsNumber)
                throw new ArgumentException("Buffer attempted to write NaN value or on an unspecified offset.");

            // Convert
            var value = Convert.ToDouble(number.Number);
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            // Make sure we have a spare buffer
            if (BufferObject.Temp == null)
                BufferObject.Temp = new byte[20];

            // Write unsafely directly
            fixed (byte* pBuffer = BufferObject.Temp)
                *((double*)(pBuffer)) = value;

            // Which way should we write?
            if (BitConverter.IsLittleEndian)
            {
                // We have to reverse
                array[index] = BufferObject.Temp[7];
                array[index + 1] = BufferObject.Temp[6];
                array[index + 2] = BufferObject.Temp[5];
                array[index + 3] = BufferObject.Temp[4];
                array[index + 4] = BufferObject.Temp[3];
                array[index + 5] = BufferObject.Temp[2];
                array[index + 6] = BufferObject.Temp[1];
                array[index + 7] = BufferObject.Temp[0];
            }
            else
            {
                // We have it in order
                array[index] = BufferObject.Temp[0];
                array[index + 1] = BufferObject.Temp[1];
                array[index + 2] = BufferObject.Temp[2];
                array[index + 3] = BufferObject.Temp[3];
                array[index + 4] = BufferObject.Temp[4];
                array[index + 5] = BufferObject.Temp[5];
                array[index + 6] = BufferObject.Temp[6];
                array[index + 7] = BufferObject.Temp[7];
            }
        }


        #endregion

        #region Write - Little Endian

        /// <summary>
        /// Writes a 2-byte signed integer value to the underlying stream.
        /// </summary>
        internal static void WriteInt16LE(FunctionObject ctx, ScriptObject instance, BoxedValue number, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!number.IsNumber || !offset.IsNumber)
                throw new ArgumentException("Buffer attempted to write NaN value or on an unspecified offset.");

            // Convert
            var value = Convert.ToInt16(number.Number);
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            array[index]     = (byte)((value) & 0xFF);
            array[index + 1] = (byte)((value >> 8) & 0xFF);

        }

        /// <summary>
        /// Writes a 2-byte unsigned integer value to the underlying stream.
        /// </summary>
        internal static void WriteUInt16LE(FunctionObject ctx, ScriptObject instance, BoxedValue number, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!number.IsNumber || !offset.IsNumber)
                throw new ArgumentException("Buffer attempted to write NaN value or on an unspecified offset.");

            // Convert
            var value = Convert.ToUInt16(number.Number);
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            array[index] = (byte)((value) & 0xFF);
            array[index + 1] = (byte)((value >> 8) & 0xFF);

        }

        /// <summary>
        /// Writes a 4-byte signed integer value to the underlying stream.
        /// </summary>
        internal static void WriteInt32LE(FunctionObject ctx, ScriptObject instance, BoxedValue number, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!number.IsNumber || !offset.IsNumber)
                throw new ArgumentException("Buffer attempted to write NaN value or on an unspecified offset.");

            // Convert
            var value = Convert.ToInt32(number.Number);
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            array[index] = (byte)((value) & 0xFF);
            array[index + 1] = (byte)((value >> 8) & 0xFF);
            array[index + 2] = (byte)((value >> 16) & 0xFF);
            array[index + 3] = (byte)((value >> 24) & 0xFF);

        }

        /// <summary>
        /// Writes a 4-byte unsigned integer value to the underlying stream.
        /// </summary>
        internal static void WriteUInt32LE(FunctionObject ctx, ScriptObject instance, BoxedValue number, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!number.IsNumber || !offset.IsNumber)
                throw new ArgumentException("Buffer attempted to write NaN value or on an unspecified offset.");

            // Convert
            var value = Convert.ToUInt32(number.Number);
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            array[index] = (byte)((value) & 0xFF);
            array[index + 1] = (byte)((value >> 8) & 0xFF);
            array[index + 2] = (byte)((value >> 16) & 0xFF);
            array[index + 3] = (byte)((value >> 24) & 0xFF);
        }

        /// <summary>
        /// Writes a 8-byte signed integer value to the underlying stream.
        /// </summary>
        internal static void WriteInt64LE(FunctionObject ctx, ScriptObject instance, BoxedValue number, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!number.IsNumber || !offset.IsNumber)
                throw new ArgumentException("Buffer attempted to write NaN value or on an unspecified offset.");

            // Convert
            var value = Convert.ToInt64(number.Number);
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            array[index] = (byte)((value) & 0xFF);
            array[index + 1] = (byte)((value >> 8) & 0xFF);
            array[index + 2] = (byte)((value >> 16) & 0xFF);
            array[index + 3] = (byte)((value >> 24) & 0xFF);
            array[index + 4] = (byte)((value >> 32) & 0xFF);
            array[index + 5] = (byte)((value >> 40) & 0xFF);
            array[index + 6] = (byte)((value >> 48) & 0xFF);
            array[index + 7] = (byte)((value >> 56) & 0xFF);
        }

        /// <summary>
        /// Writes a 8-byte unsigned integer value to the underlying stream.
        /// </summary>
        internal static void WriteUInt64LE(FunctionObject ctx, ScriptObject instance, BoxedValue number, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!number.IsNumber || !offset.IsNumber)
                throw new ArgumentException("Buffer attempted to write NaN value or on an unspecified offset.");

            // Convert
            var value = Convert.ToUInt64(number.Number);
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            array[index] = (byte)((value) & 0xFF);
            array[index + 1] = (byte)((value >> 8) & 0xFF);
            array[index + 2] = (byte)((value >> 16) & 0xFF);
            array[index + 3] = (byte)((value >> 24) & 0xFF);
            array[index + 4] = (byte)((value >> 32) & 0xFF);
            array[index + 5] = (byte)((value >> 40) & 0xFF);
            array[index + 6] = (byte)((value >> 48) & 0xFF);
            array[index + 7] = (byte)((value >> 56) & 0xFF);
        }

        /// <summary>
        /// Writes an IEEE 754 single-precision (32-bit) floating-point number to the buffer
        /// </summary>
        internal static unsafe void WriteFloatLE(FunctionObject ctx, ScriptObject instance, BoxedValue number, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!number.IsNumber || !offset.IsNumber)
                throw new ArgumentException("Buffer attempted to write NaN value or on an unspecified offset.");

            // Convert
            var value = Convert.ToSingle(number.Number);
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            // Make sure we have a spare buffer
            if (BufferObject.Temp == null)
                BufferObject.Temp = new byte[20];

            // Write unsafely directly
            fixed (byte* pBuffer = BufferObject.Temp)
                *((float*)(pBuffer)) = value;

            // Which way should we write?
            if (!BitConverter.IsLittleEndian)
            {
                // We have to reverse
                array[index] = BufferObject.Temp[3];
                array[index + 1] = BufferObject.Temp[2];
                array[index + 2] = BufferObject.Temp[1];
                array[index + 3] = BufferObject.Temp[0];
            }
            else
            {
                // We have it in order
                array[index] = BufferObject.Temp[0];
                array[index + 1] = BufferObject.Temp[1];
                array[index + 2] = BufferObject.Temp[2];
                array[index + 3] = BufferObject.Temp[3];
            }
        }

        /// <summary>
        /// Writes an IEEE 754 double-precision (64-bit) floating-point number to the buffer
        /// </summary>
        internal static unsafe void WriteDoubleLE(FunctionObject ctx, ScriptObject instance, BoxedValue number, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!number.IsNumber || !offset.IsNumber)
                throw new ArgumentException("Buffer attempted to write NaN value or on an unspecified offset.");

            // Convert
            var value = Convert.ToDouble(number.Number);
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            // Make sure we have a spare buffer
            if (BufferObject.Temp == null)
                BufferObject.Temp = new byte[20];

            // Write unsafely directly
            fixed (byte* pBuffer = BufferObject.Temp)
                *((double*)(pBuffer)) = value;

            // Which way should we write?
            if (!BitConverter.IsLittleEndian)
            {
                // We have to reverse
                array[index] = BufferObject.Temp[7];
                array[index + 1] = BufferObject.Temp[6];
                array[index + 2] = BufferObject.Temp[5];
                array[index + 3] = BufferObject.Temp[4];
                array[index + 4] = BufferObject.Temp[3];
                array[index + 5] = BufferObject.Temp[2];
                array[index + 6] = BufferObject.Temp[1];
                array[index + 7] = BufferObject.Temp[0];
            }
            else
            {
                // We have it in order
                array[index] = BufferObject.Temp[0];
                array[index + 1] = BufferObject.Temp[1];
                array[index + 2] = BufferObject.Temp[2];
                array[index + 3] = BufferObject.Temp[3];
                array[index + 4] = BufferObject.Temp[4];
                array[index + 5] = BufferObject.Temp[5];
                array[index + 6] = BufferObject.Temp[6];
                array[index + 7] = BufferObject.Temp[7];
            }
        }


        #endregion

        #region Read - Big Endian

        /// <summary>
        /// Performs a read from the underlying stream.
        /// </summary>
        /// <returns>Returns value read.</returns>
        internal static BoxedValue ReadUInt8(FunctionObject ctx, ScriptObject instance, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!offset.IsNumber || (offset.Number + 1) > buffer.Count)
                throw new ArgumentException("Attempt to read on an invalid offset.");

            // Convert and check the limits
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            return BoxedValue.Box(array[index]);
        }

        /// <summary>
        /// Performs a read from the underlying stream.
        /// </summary>
        /// <returns>Returns value read.</returns>
        internal static BoxedValue ReadInt8(FunctionObject ctx, ScriptObject instance, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!offset.IsNumber || (offset.Number + 1) > buffer.Count)
                throw new ArgumentException("Attempt to read on an invalid offset.");

            // Convert and check the limits
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            return BoxedValue.Box((sbyte)array[index]);
        }


        /// <summary>
        /// Performs a read from the underlying stream.
        /// </summary>
        /// <returns>Returns value read.</returns>
        internal static BoxedValue ReadUInt16BE(FunctionObject ctx, ScriptObject instance, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!offset.IsNumber || (offset.Number + 2) > buffer.Count)
                throw new ArgumentException("Attempt to read on an invalid offset.");

            // Convert and check the limits
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            return BoxedValue.Box((ushort)(
                (array[index] << 8) |
                (array[index + 1])
                ));
        }


        /// <summary>
        /// Performs a read from the underlying stream.
        /// </summary>
        /// <returns>Returns value read.</returns>
        internal static BoxedValue ReadInt16BE(FunctionObject ctx, ScriptObject instance, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!offset.IsNumber || (offset.Number + 2) > buffer.Count)
                throw new ArgumentException("Attempt to read on an invalid offset.");

            // Convert and check the limits
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            return BoxedValue.Box((short)(
                (array[index] << 8) |
                (array[index + 1])
                ));
        }



        /// <summary>
        /// Performs a read from the underlying stream.
        /// </summary>
        /// <returns>Returns value read.</returns>
        internal static BoxedValue ReadUInt32BE(FunctionObject ctx, ScriptObject instance, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!offset.IsNumber || (offset.Number + 4) > buffer.Count)
                throw new ArgumentException("Attempt to read on an invalid offset.");

            // Convert and check the limits
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            return BoxedValue.Box((uint)(
                (array[index] << 24) |
                (array[index + 1] << 16) |
                (array[index + 2] << 8) |
                (array[index + 3])
                ));
        }


        /// <summary>
        /// Performs a read from the underlying stream.
        /// </summary>
        /// <returns>Returns value read.</returns>
        internal static BoxedValue ReadInt32BE(FunctionObject ctx, ScriptObject instance, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!offset.IsNumber || (offset.Number + 4) > buffer.Count)
                throw new ArgumentException("Attempt to read on an invalid offset.");

            // Convert and check the limits
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            return BoxedValue.Box((int)(
                (array[index] << 24) |
                (array[index + 1] << 16) |
                (array[index + 2] << 8) |
                (array[index + 3])
                ));
        }


        /// <summary>
        /// Performs a read from the underlying stream.
        /// </summary>
        /// <returns>Returns value read.</returns>
        internal static BoxedValue ReadUInt64BE(FunctionObject ctx, ScriptObject instance, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!offset.IsNumber || (offset.Number + 8) > buffer.Count)
                throw new ArgumentException("Attempt to read on an invalid offset.");

            // Convert and check the limits
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            return BoxedValue.Box((ulong)(
                (array[index] << 56) |
                (array[index + 1] << 48) |
                (array[index + 2] << 40) |
                (array[index + 3] << 32) |
                (array[index + 4] << 24) |
                (array[index + 5] << 16) |
                (array[index + 6] << 8) |
                (array[index + 7])
                ));
        }


        /// <summary>
        /// Performs a read from the underlying stream.
        /// </summary>
        /// <returns>Returns value read.</returns>
        internal static BoxedValue ReadInt64BE(FunctionObject ctx, ScriptObject instance, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!offset.IsNumber || (offset.Number + 8) > buffer.Count)
                throw new ArgumentException("Attempt to read on an invalid offset.");

            // Convert and check the limits
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            return BoxedValue.Box((long)(
                (array[index] << 56) |
                (array[index + 1] << 48) |
                (array[index + 2] << 40) |
                (array[index + 3] << 32) |
                (array[index + 4] << 24) |
                (array[index + 5] << 16) |
                (array[index + 6] << 8) |
                (array[index + 7])
                ));
        }



        /// <summary>
        /// Reads an IEEE 754 single-precision (32-bit) floating-point number from the buffer
        /// </summary>
        internal unsafe static BoxedValue ReadFloatBE(FunctionObject ctx, ScriptObject instance, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!offset.IsNumber || (offset.Number + 4) > buffer.Count)
                throw new ArgumentException("Attempt to read on an invalid offset.");

            // Convert and check the limits
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            // Make sure we have a spare buffer
            if (BufferObject.Temp == null)
                BufferObject.Temp = new byte[20];

            // Which way should we write?
            if (BitConverter.IsLittleEndian)
            {
                // We have to reverse
                BufferObject.Temp[3] = array[index];
                BufferObject.Temp[2] = array[index + 1];
                BufferObject.Temp[1] = array[index + 2];
                BufferObject.Temp[0] = array[index + 3];
            }
            else
            {
                // We have it in order
                BufferObject.Temp[0] = array[index];
                BufferObject.Temp[1] = array[index + 1];
                BufferObject.Temp[2] = array[index + 2];
                BufferObject.Temp[3] = array[index + 3];
            }

            // Read the value from the buffer
            float value = 0;
            fixed (byte* pBuffer = BufferObject.Temp)
                value = *((float*)(pBuffer));

            return BoxedValue.Box(value);
        }



        /// <summary>
        /// Reads an IEEE 754 double-precision (64-bit) floating-point number from the buffer
        /// </summary>
        internal unsafe static BoxedValue ReadDoubleBE(FunctionObject ctx, ScriptObject instance, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!offset.IsNumber || (offset.Number + 8) > buffer.Count)
                throw new ArgumentException("Attempt to read on an invalid offset.");

            // Convert and check the limits
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;


            // Make sure we have a spare buffer
            if (BufferObject.Temp == null)
                BufferObject.Temp = new byte[20];

            // Which way should we write?
            if (BitConverter.IsLittleEndian)
            {
                // We have to reverse
                BufferObject.Temp[7] = array[index];
                BufferObject.Temp[6] = array[index + 1];
                BufferObject.Temp[5] = array[index + 2];
                BufferObject.Temp[4] = array[index + 3];
                BufferObject.Temp[3] = array[index + 4];
                BufferObject.Temp[2] = array[index + 5];
                BufferObject.Temp[1] = array[index + 6];
                BufferObject.Temp[0] = array[index + 7];
            }
            else
            {
                // We have it in order
                BufferObject.Temp[0] = array[index];
                BufferObject.Temp[1] = array[index + 1];
                BufferObject.Temp[2] = array[index + 2];
                BufferObject.Temp[3] = array[index + 3];
                BufferObject.Temp[4] = array[index + 4];
                BufferObject.Temp[5] = array[index + 5];
                BufferObject.Temp[6] = array[index + 6];
                BufferObject.Temp[7] = array[index + 7];
            }

            // Read the value from the buffer
            double value = 0;
            fixed (byte* pBuffer = BufferObject.Temp)
                value = *((double*)(pBuffer));

            return BoxedValue.Box(value);
        }

        #endregion

        #region Read - Little Endian

        /// <summary>
        /// Performs a read from the underlying stream.
        /// </summary>
        /// <returns>Returns value read.</returns>
        internal static BoxedValue ReadUInt16LE(FunctionObject ctx, ScriptObject instance, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!offset.IsNumber || (offset.Number + 2) > buffer.Count)
                throw new ArgumentException("Attempt to read on an invalid offset.");

            // Convert and check the limits
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            return BoxedValue.Box((ushort)(
                (array[index + 1] << 8) |
                (array[index])
                ));
        }


        /// <summary>
        /// Performs a read from the underlying stream.
        /// </summary>
        /// <returns>Returns value read.</returns>
        internal static BoxedValue ReadInt16LE(FunctionObject ctx, ScriptObject instance, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!offset.IsNumber || (offset.Number + 2) > buffer.Count)
                throw new ArgumentException("Attempt to read on an invalid offset.");

            // Convert and check the limits
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            return BoxedValue.Box((short)(
                (array[index + 1] << 8) |
                (array[index ])
                ));
        }



        /// <summary>
        /// Performs a read from the underlying stream.
        /// </summary>
        /// <returns>Returns value read.</returns>
        internal static BoxedValue ReadUInt32LE(FunctionObject ctx, ScriptObject instance, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!offset.IsNumber || (offset.Number + 4) > buffer.Count)
                throw new ArgumentException("Attempt to read on an invalid offset.");

            // Convert and check the limits
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            return BoxedValue.Box((uint)(
                (array[index + 3] << 24) |
                (array[index + 2] << 16) |
                (array[index + 1] << 8) |
                (array[index])
                ));
        }


        /// <summary>
        /// Performs a read from the underlying stream.
        /// </summary>
        /// <returns>Returns value read.</returns>
        internal static BoxedValue ReadInt32LE(FunctionObject ctx, ScriptObject instance, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!offset.IsNumber || (offset.Number + 4) > buffer.Count)
                throw new ArgumentException("Attempt to read on an invalid offset.");

            // Convert and check the limits
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            return BoxedValue.Box((int)(
                (array[index + 3] << 24) |
                (array[index + 2] << 16) |
                (array[index + 1] << 8) |
                (array[index])
                ));
        }


        /// <summary>
        /// Performs a read from the underlying stream.
        /// </summary>
        /// <returns>Returns value read.</returns>
        internal static BoxedValue ReadUInt64LE(FunctionObject ctx, ScriptObject instance, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!offset.IsNumber || (offset.Number + 8) > buffer.Count)
                throw new ArgumentException("Attempt to read on an invalid offset.");

            // Convert and check the limits
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            return BoxedValue.Box((ulong)(
                (array[index + 7] << 56) |
                (array[index + 6] << 48) |
                (array[index + 5] << 40) |
                (array[index + 4] << 32) |
                (array[index + 3] << 24) |
                (array[index + 2] << 16) |
                (array[index + 1] << 8) |
                (array[index])
                ));
        }


        /// <summary>
        /// Performs a read from the underlying stream.
        /// </summary>
        /// <returns>Returns value read.</returns>
        internal static BoxedValue ReadInt64LE(FunctionObject ctx, ScriptObject instance, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!offset.IsNumber || (offset.Number + 8) > buffer.Count)
                throw new ArgumentException("Attempt to read on an invalid offset.");

            // Convert and check the limits
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            return BoxedValue.Box((long)(
                (array[index + 7] << 56) |
                (array[index + 6] << 48) |
                (array[index + 5] << 40) |
                (array[index + 4] << 32) |
                (array[index + 3] << 24) |
                (array[index + 2] << 16) |
                (array[index + 1] << 8) |
                (array[index])
                ));
        }



        /// <summary>
        /// Reads an IEEE 754 single-precision (32-bit) floating-point number from the buffer
        /// </summary>
        internal unsafe static BoxedValue ReadFloatLE(FunctionObject ctx, ScriptObject instance, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!offset.IsNumber || (offset.Number + 4) > buffer.Count)
                throw new ArgumentException("Attempt to read on an invalid offset.");

            // Convert and check the limits
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            // Make sure we have a spare buffer
            if (BufferObject.Temp == null)
                BufferObject.Temp = new byte[20];

            // Which way should we write?
            if (!BitConverter.IsLittleEndian)
            {
                // We have to reverse
                BufferObject.Temp[3] = array[index];
                BufferObject.Temp[2] = array[index + 1];
                BufferObject.Temp[1] = array[index + 2];
                BufferObject.Temp[0] = array[index + 3];
            }
            else
            {
                // We have it in order
                BufferObject.Temp[0] = array[index];
                BufferObject.Temp[1] = array[index + 1];
                BufferObject.Temp[2] = array[index + 2];
                BufferObject.Temp[3] = array[index + 3];
            }

            // Read the value from the buffer
            float value = 0;
            fixed (byte* pBuffer = BufferObject.Temp)
                value = *((float*)(pBuffer));

            return BoxedValue.Box(value);
        }



        /// <summary>
        /// Reads an IEEE 754 double-precision (64-bit) floating-point number from the buffer
        /// </summary>
        internal unsafe static BoxedValue ReadDoubleLE(FunctionObject ctx, ScriptObject instance, BoxedValue offset)
        {
            // Get the buffer and the appropriate offset
            var buffer = instance.CastTo<BufferObject>();
            if (!offset.IsNumber || (offset.Number + 8) > buffer.Count)
                throw new ArgumentException("Attempt to read on an invalid offset.");

            // Convert and check the limits
            var index = buffer.Offset + (int)offset.Number;
            var array = buffer.Array;

            // Make sure we have a spare buffer
            if (BufferObject.Temp == null)
                BufferObject.Temp = new byte[20];

            // Which way should we write?
            if (!BitConverter.IsLittleEndian)
            {
                // We have to reverse
                BufferObject.Temp[7] = array[index];
                BufferObject.Temp[6] = array[index + 1];
                BufferObject.Temp[5] = array[index + 2];
                BufferObject.Temp[4] = array[index + 3];
                BufferObject.Temp[3] = array[index + 4];
                BufferObject.Temp[2] = array[index + 5];
                BufferObject.Temp[1] = array[index + 6];
                BufferObject.Temp[0] = array[index + 7];
            }
            else
            {
                // We have it in order
                BufferObject.Temp[0] = array[index];
                BufferObject.Temp[1] = array[index + 1];
                BufferObject.Temp[2] = array[index + 2];
                BufferObject.Temp[3] = array[index + 3];
                BufferObject.Temp[4] = array[index + 4];
                BufferObject.Temp[5] = array[index + 5];
                BufferObject.Temp[6] = array[index + 6];
                BufferObject.Temp[7] = array[index + 7];
            }

            // Read the value from the buffer
            double value = 0;
            fixed (byte* pBuffer = BufferObject.Temp)
                value = *((double*)(pBuffer));

            return BoxedValue.Box(value);
        }

        #endregion
    }

}