﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using System.Threading;
using NodeDotnet.Core;
using System.Runtime.Serialization.Json;
using System.IO;

namespace NodeDotnet
{
    public class Bridge
    {
        [DllImport("user32.dll")]
        static extern int MessageBox(IntPtr hWnd, string text, string caption, int type);

        [return: MarshalAs(UnmanagedType.LPWStr)]
        public static string Initialize([MarshalAs(UnmanagedType.LPWStr)] string assemblyName)
        {
            // TODO: in managedBridge, read all assemblies in root (TestModule) and add those to tpaList
            // TODO: return MethodInfos as json to calling javascript. Create javascript classes in C++
            // TODO: At the moment creater d.ts file manually
            // TODO: Constructor of C++-Proxy -> Invoke new Object in TestModule

            //MessageBox(IntPtr.Zero, "Haha", "Huhu", 0);
            assembly = Assembly.Load(assemblyName);
            var objectTypes = from n in assembly.GetExportedTypes()
                                where n.GetCustomAttribute(typeof(JavascriptObjectAttribute)) != null
                                select n;

            objectNames = objectTypes.ToDictionary(m => m.Name, m => new ObjectInfo(m));
            var objects = 
                from n in objectTypes
                select new JSClass(n.Name,
                    from m in n.GetMethods()
                    where m.GetCustomAttribute(typeof(JavascriptMethodAttribute)) != null
                    select new Method(m));
            var seri = new DataContractJsonSerializer(typeof(JSClass[]));
            var ms = new MemoryStream();
            seri.WriteObject(ms, objects.ToArray());
            ms.Capacity = (int)ms.Length;
            var buff = ms.GetBuffer();
            var result = Encoding.UTF8.GetString(buff);
            return result;
        }

        public static void ConstructObject(int objectId, [MarshalAs(UnmanagedType.LPWStr)] string name)
        {
            var info = objectNames[name];
            var t = assembly.GetType(info.FullName);
            var o = Activator.CreateInstance(t);
            objects[objectId] = new ObjectHolder(o, info);
        }

        public static void DeleteObject(int objectId)
            => objects.Remove(objectId);

        [return: MarshalAs(UnmanagedType.LPWStr)]
        public static string ExecuteSync(int objectId, [MarshalAs(UnmanagedType.LPWStr)] string method, [MarshalAs(UnmanagedType.LPWStr)] string input)
            =>"Retörning from Mänaged Cöde: " + input;

        [return: MarshalAs(UnmanagedType.LPWStr)]
        public static string Execute(int objectId, [MarshalAs(UnmanagedType.LPWStr)] string method,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)][In] byte[] payload, int size)
        {
            var info = objects[objectId];
            var methodInfo = info.Info.Methods[method];
            var position = 0;

            object GetParameter(Parameter parameterInfo)
            {
                switch (parameterInfo.InternalType)
                {
                    case InternalParameterType.Int:
                        return ReadInt(payload, ref position);
                    case InternalParameterType.Double:
                        ReadInt(payload, ref position);
                        return 9.9;
                    case InternalParameterType.String:
                        return ReadString(payload, ref position);
                    case InternalParameterType.Date:
                        return DateTime.Now;
                    default:
                        return null;
                }
            }

            var parameters = methodInfo.Parameters.Select(n => GetParameter(n)).ToArray();
            var result = methodInfo.Info.Invoke(info.Object, parameters);
            switch (result)
            {
                case string s:
                    return s;
                case int i:
                    return i.ToString();
                default:
                    return "";
            }
        }

        static int ReadInt(byte[] payload, ref int position)
        {
            var length = BitConverter.ToInt32(payload, position);
            position += 4;
            var result = BitConverter.ToInt32(payload, position);
            position += length;
            return result;
        }

        static string ReadString(byte[] payload, ref int position)
        {
            var length = BitConverter.ToInt32(payload, position);
            position += 4;
            var result = Encoding.Unicode.GetString(payload, position, length);
            position += length;
            return result;
        }

        static Dictionary<string, ObjectInfo> objectNames = new Dictionary<string, ObjectInfo>();
        static Dictionary<int, ObjectHolder> objects = new Dictionary<int, ObjectHolder>();
        static Assembly assembly;
    }
}
