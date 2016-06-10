﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using VBspViewer.Importing.VBsp.Structures;

namespace VBspViewer.Importing.VBsp
{
    internal class ReadLumpWrapper<T>
        where T : struct
    {
        public static T[] ReadLump(byte[] src, int offset, int length)
        {
            var size = Marshal.SizeOf(typeof(T));
            var count = length/size;
            var array = new T[count];

            var tempPtr = Marshal.AllocHGlobal(size);
            
            for (var i = 0; i < count; ++i)
            {
                Marshal.Copy(src, offset + i * size, tempPtr, size);
                array[i] = (T) Marshal.PtrToStructure(tempPtr, typeof (T));
            }

            Marshal.FreeHGlobal(tempPtr);

            return array;
        }

        public static void ReadLumpFromStream(Stream stream, int count, Action<T> handler)
        {
            var size = Marshal.SizeOf(typeof(T));
            var tempPtr = Marshal.AllocHGlobal(size);
            var buffer = new byte[size];

            for (var i = 0; i < count; ++i)
            {
                var start = stream.Position;
                stream.Read(buffer, 0, size);
                Marshal.Copy(buffer, 0, tempPtr, size);
                var val = (T) Marshal.PtrToStructure(tempPtr, typeof(T));

                stream.Seek(start, SeekOrigin.Begin);
                handler(val);
                stream.Seek(start + size, SeekOrigin.Begin);
            }

            Marshal.FreeHGlobal(tempPtr);
        }

        public static T[] ReadLumpFromStream(Stream stream, int count)
        {
            var arr = new T[count];
            var index = 0;
            ReadLumpFromStream(stream, count, val => arr[index++] = val);
            return arr;
        }
    }

    public partial class VBspFile
    {
        [MeansImplicitUse, AttributeUsage(AttributeTargets.Property)]
        private class LumpAttribute : Attribute
        {
            public LumpType Type { get; set; }
            public int StartOffset { get; set; }
        }

        private delegate void ReadLumpDelegate(VBspFile file, byte[] src, int length);

        private static readonly Dictionary<Type, MethodInfo> _sReadLumpMethods = new Dictionary<Type, MethodInfo>();
        private static MethodInfo FindReadLumpMethod(Type type)
        {
            MethodInfo readLumpMethod;
            if (_sReadLumpMethods.TryGetValue(type, out readLumpMethod)) return readLumpMethod;

            const BindingFlags bFlags = BindingFlags.Static | BindingFlags.Public;

            var readLumpWrapper = typeof (ReadLumpWrapper<>).MakeGenericType(type);
            var readLump = readLumpWrapper.GetMethod("ReadLump", bFlags);

            _sReadLumpMethods.Add(type, readLump);
            return readLump;
        }

        private static Dictionary<LumpType, ReadLumpDelegate> _sReadLumpDelegates;
        private static Dictionary<LumpType, ReadLumpDelegate> GetReadLumpDelegates()
        {
            if (_sReadLumpDelegates != null) return _sReadLumpDelegates;

            _sReadLumpDelegates = new Dictionary<LumpType, ReadLumpDelegate>();

            var fileParam = Expression.Parameter(typeof (VBspFile), "file");
            var srcParam = Expression.Parameter(typeof (byte[]), "src");
            var lengthParam = Expression.Parameter(typeof (int), "length");

            const BindingFlags bFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            foreach (var prop in typeof(VBspFile).GetProperties(bFlags))
            {
                var attrib = (LumpAttribute) prop.GetCustomAttributes(typeof(LumpAttribute), true).FirstOrDefault();
                if (attrib == null) continue;

                var type = prop.PropertyType.GetElementType();
                var readLumpMethod = FindReadLumpMethod(type);

                var offsetConst = Expression.Constant(attrib.StartOffset);
                var lengthVal = Expression.Subtract(lengthParam, offsetConst);
                var call = Expression.Call(readLumpMethod, srcParam, offsetConst, lengthVal);
                var set = Expression.Call(fileParam, prop.GetSetMethod(true), call);
                var lambda = Expression.Lambda<ReadLumpDelegate>(set, fileParam, srcParam, lengthParam);

                _sReadLumpDelegates.Add(attrib.Type, lambda.Compile());
            }

            return _sReadLumpDelegates;
        }
    }
}
