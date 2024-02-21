using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace RedSaw.CommandLineInterface
{

    static class DefaultTypeParserDefination
    {

        /// <summary>default type parsers</summary>
        public static Dictionary<Type, ValueParser> DefaultTypeParser
        {
            get
            {
                return new Dictionary<Type, ValueParser>(){
                    {typeof(int), TryParseInt},
                    {typeof(float), TryParseFloat},
                    {typeof(double), TryParseDouble},
                    {typeof(bool), TryParseBool},
                    {typeof(string), TryParseString},
                    {typeof(char), TryParseChar},
                    {typeof(byte), TryParseByte},
                    {typeof(sbyte), TryParseSByte},
                    {typeof(short), TryParseShort},
                    {typeof(ushort), TryParseUShort},
                    {typeof(uint), TryParseUInt},
                    {typeof(long), TryParseLong},
                    {typeof(ulong), TryParseULong},
                    {typeof(decimal), TryParseDecimal},
                };
            }
        }

        /// <summary>default type alias</summary>
        public static Dictionary<string, Type> DefaultTypeAlias
        {
            get
            {
                return new Dictionary<string, Type>(){
                    {"int", typeof(int)},
                    {"float", typeof(float)},
                    {"double", typeof(double)},
                    {"bool", typeof(bool)},
                    {"string", typeof(string)},
                    {"char", typeof(char)},
                    {"byte", typeof(byte)},
                    {"sbyte", typeof(sbyte)},
                    {"short", typeof(short)},
                    {"ushort", typeof(ushort)},
                    {"uint", typeof(uint)},
                    {"long", typeof(long)},
                    {"ulong", typeof(ulong)},
                    {"decimal", typeof(decimal)},
                };
            }
        }

        static bool TryParseInt(string input, out object result)
        {
            if (int.TryParse(input, out int value))
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }
        static bool TryParseFloat(string input, out object result)
        {
            if (float.TryParse(input, out float value))
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }
        static bool TryParseDouble(string input, out object result)
        {
            if (double.TryParse(input, out double value))
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }
        static bool TryParseBool(string input, out object result)
        {

            if (bool.TryParse(input, out bool value))
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }
        static bool TryParseString(string input, out object result)
        {

            result = input;
            return true;
        }
        static bool TryParseChar(string input, out object result)
        {

            if (char.TryParse(input, out char value))
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }
        static bool TryParseByte(string input, out object result)
        {

            if (byte.TryParse(input, out byte value))
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }
        static bool TryParseSByte(string input, out object result)
        {

            if (sbyte.TryParse(input, out sbyte value))
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }
        static bool TryParseShort(string input, out object result)
        {

            if (short.TryParse(input, out short value))
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }
        static bool TryParseUShort(string input, out object result)
        {

            if (ushort.TryParse(input, out ushort value))
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }
        static bool TryParseUInt(string input, out object result)
        {

            if (uint.TryParse(input, out uint value))
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }
        static bool TryParseLong(string input, out object result)
        {

            if (long.TryParse(input, out long value))
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }
        static bool TryParseULong(string input, out object result)
        {

            if (ulong.TryParse(input, out ulong value))
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }
        static bool TryParseDecimal(string input, out object result)
        {

            if (decimal.TryParse(input, out decimal value))
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }

    }

    /// <summary>provide utils methods while execute AST</summary>
    public static class VirtualMachineUtils
    {

        /// <summary>try to get element of given instance like a Array or List</summary>
        /// <param name="arrlike">the array like instance</param>
        /// <param name="index">array index</param>
        /// <exception cref="CommandExecuteException"></exception>
        public static object GetElement(object arrlike, int index)
        {

            var type = arrlike.GetType();
            if (type.IsSZArray)
            {

                if (arrlike is not Array array) throw new CommandExecuteException("cannot index null object");
                if (index < 0 || index >= array.Length) throw new CommandExecuteException($"index out of range: {index}");
                return array.GetValue(index);
            }

            if (arrlike is IList list)
            {

                if (index < 0 || index >= list.Count) throw new CommandExecuteException($"index out of range: {index}");
                return list[index];
            }

            var method = type.GetGetterMethod<int>();
            if (method != null)
            {
                return method.Invoke(arrlike, new object[] { index });
            }

            throw new CommandExecuteException($"cannot index object of type {type}");
        }

        /// <summary>try to get element of given instance like a Dictionary</summary>
        /// <param name="dictlike">the Dictionary like instance</param>
        /// <param name="key">dictionary key</param>
        /// <exception cref="CommandExecuteException"></exception>
        public static object GetElement(object dictlike, object key)
        {

            if (key == null) throw new CommandExecuteException("cannot index object by null");

            var instanceType = dictlike.GetType();
            if (dictlike is IDictionary dict && instanceType.IsGenericType)
            {
                var keyType = instanceType.GenericTypeArguments[0];
                if (keyType.IsAssignableFrom(key.GetType()))
                {
                    return dict[key];
                }
                throw new CommandExecuteException($"cannot index object of type {instanceType} by {key.GetType()}");
            }

            var method = instanceType.GetGetterMethod(key.GetType());
            if (method != null)
            {
                return method.Invoke(dictlike, new object[] { key });
            }

            throw new CommandExecuteException($"cannot index object of type {instanceType}");
        }

        /// <summary>try to set element of given instance like a Array or List</summary>
        /// <param name="arrlike">the array like instance (must not be null) </param>
        /// <param name="index">array index</param>
        /// <param name="value">value to set</param>
        /// <exception cref="CommandExecuteException"></exception>
        public static void SetElement(object arrlike, int index, object value)
        {

            var type = arrlike.GetType();
            if (type.IsSZArray)
            {

                if (arrlike is not Array array) throw new CommandExecuteException($"cannot set element of \"{type}\" object");
                if (index < 0 || index >= array.Length) throw new CommandExecuteException($"index out of range: {index}");
                var elementType = type.GetElementType() ?? throw new CommandExecuteException($"invalid element type");

                if (value == null)
                {

                    if (elementType.IsNullable())
                    {
                        array.SetValue(null, index);
                        return;
                    }
                    throw new CommandExecuteException($"cannot set null to element of \"{type}\" object");
                }
                array.SetValue(value, index);
                return;
            }

            if (arrlike is IList list)
            {

                if (index < 0 || index >= list.Count) throw new CommandExecuteException($"index out of range: {index}");
                if (type.IsGenericType)
                {
                    var elementType = type.GenericTypeArguments[0];
                    if (value == null)
                    {
                        if (elementType.IsNullable())
                        {
                            list[index] = null;
                            return;
                        }
                        throw new CommandExecuteException($"cannot set null to element of \"{type}\" object");
                    }
                    var valueType = value.GetType();
                    if (elementType.IsAssignableFrom(valueType))
                    {
                        list[index] = value;
                        return;
                    }
                    throw new CommandExecuteException($"cannot set \"{valueType}\" to element of \"{type}\" object");
                }

                list[index] = value;
            }


            var method = type.GetGetterMethod<int>();
            if (method != null)
            {

                var parameters = method.GetParameters();
                if (value == null)
                {
                    if (parameters[1].ParameterType.IsNullable())
                    {
                        _ = method.Invoke(arrlike, new object[] { index, null });
                        return;
                    }
                    throw new CommandExecuteException($"cannot set null to element of \"{type}\" object");
                }

                var valueType = value.GetType();
                if (parameters[1].ParameterType.IsAssignableFrom(valueType))
                {
                    method.Invoke(arrlike, new object[] { index, value });
                    return;
                }
                throw new CommandExecuteException($"cannot set \"{valueType}\" to element of \"{type}\" object");
            }

            throw new CommandExecuteException($"cannot set element of \"{type}\" object");
        }

        /// <summary>try to set element of given instance like a Dictionary</summary>
        /// <param name="dictlike">the Dictionary like instance (must not be null) </param>
        /// <param name="key">dictionary key</param>
        /// <param name="value">value to set</param>
        /// <exception cref="CommandExecuteException"></exception>
        public static void SetElement(object dictlike, object key, object value)
        {

            var instanceType = dictlike.GetType();
            var indexType = key.GetType();
            if (instanceType is IDictionary dict && instanceType.IsGenericType)
            {
                var keyType = instanceType.GenericTypeArguments[0];
                if (!keyType.IsAssignableFrom(indexType))
                {
                    throw new CommandExecuteException($"\"{indexType}\" cannot be index of \"{instanceType}\" object");
                }
                var destValueType = instanceType.GenericTypeArguments[1];
                if (value == null)
                {
                    if (destValueType.IsNullable())
                    {
                        dict[key] = null;
                        return;
                    }
                    throw new CommandExecuteException($"cannot set null to element of \"{instanceType}\" object");
                }
                if (destValueType.IsAssignableFrom(value.GetType()))
                {
                    dict[key] = value;
                    return;
                }
                throw new CommandExecuteException($"cannot set \"{value.GetType()}\" to element of \"{instanceType}\" object");
            }

            var method = instanceType.GetSetterMethod(indexType);
            if (method != null)
            {

                var parameters = method.GetParameters();
                if (value == null)
                {
                    if (parameters[1].ParameterType.IsNullable())
                    {
                        _ = method.Invoke(dictlike, new object[] { key, null });
                        return;
                    }
                    throw new CommandExecuteException($"cannot set null to element of \"{instanceType}\" object");
                }

                if (parameters[1].ParameterType.IsAssignableFrom(value.GetType()))
                {
                    method.Invoke(dictlike, new object[] { key, value });
                    return;
                }
                throw new CommandExecuteException($"cannot set \"{value.GetType()}\" to element of \"{instanceType}\" object");
            }

            throw new CommandExecuteException($"cannot set element of \"{instanceType}\" object");
        }

        /// <summary>try to get element type of given instance like a Array or List</summary>
        /// <param name="instance">the array like instance</param>
        public static Type GetElementTypeOfArray(object instance)
        {

            var instanceType = instance.GetType();
            if (instanceType.IsSZArray)
            {
                return instanceType.GetElementType();
            }

            if (instance is IList && instanceType.IsGenericType)
            {
                return instanceType.GenericTypeArguments[0];
            }

            var method = instanceType.GetSetterMethod<int>();
            if (method != null)
            {
                return method.GetParameters()[1].ParameterType;
            }
            return null;
        }

        /// <summary>try to get element type of given instance like a Dictionary</summary>
        /// <param name="instance">the Dictionary like instance</param>
        /// <param name="indexType">dictionary key type</param>
        public static Type GetElementTypeOfDict(object instance, Type indexType)
        {

            var instanceType = instance.GetType();
            if (instance is IDictionary && instanceType.IsGenericType)
            {

                var keyType = instanceType.GenericTypeArguments[0];
                if (keyType.IsAssignableFrom(indexType))
                {
                    return instanceType.GenericTypeArguments[1];
                }
            }

            var method = instanceType.GetSetterMethod(indexType);
            if (method != null)
            {
                return method.GetParameters()[1].ParameterType;
            }
            return null;
        }
    }

    static class CSharpUtils
    {

        /// <summary>
        /// indicate that given type is a nullable type
        /// </summary>
        public static bool IsNullable(this Type type)
        {
            // return Nullable.GetUnderlyingType(type) != null;
            return !type.IsValueType;
        }

        public static MemberInfo GetDefaultMember(this Type type, string memberName)
        {

            return type.GetMember(memberName,
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance |
            BindingFlags.Static).FirstOrDefault();
        }
        public static MethodInfo GetGetterMethod<TKey>(this Type type) => GetGetterMethod(type, typeof(TKey));
        public static MethodInfo GetGetterMethod(this Type type, Type keyType)
        {

            var methodInfos = type.GetMethods(
                BindingFlags.NonPublic |
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.Static
            );
            foreach (var methodInfo in methodInfos)
            {
                if (methodInfo.Name != "get_Item") continue;
                var parameters = methodInfo.GetParameters();
                if (parameters.Length == 1 &&
                    parameters[0].ParameterType.IsAssignableFrom(keyType))
                {
                    return methodInfo;
                }
            }
            return null;
        }
        public static MethodInfo GetSetterMethod<TKey>(this Type type) => GetSetterMethod(type, typeof(TKey));
        public static MethodInfo GetSetterMethod(this Type type, Type keyType)
        {

            var methodInfos = type.GetMethods(
                BindingFlags.NonPublic |
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.Static
            );
            foreach (var methodInfo in methodInfos)
            {
                if (methodInfo.Name != "set_Item") continue;
                var parameters = methodInfo.GetParameters();
                if (parameters.Length == 2)
                {
                    if (parameters[0].ParameterType.IsAssignableFrom(keyType))
                    {
                        return methodInfo;
                    }
                }
            }
            return null;
        }

        public static string GetMemberTypeName(this MemberInfo memberInfo){

            return memberInfo.MemberType switch
            {
                MemberTypes.Property => ((PropertyInfo)memberInfo).PropertyType.Name,
                MemberTypes.Field => ((FieldInfo)memberInfo).FieldType.Name,
                MemberTypes.Method => $"(..) -> {((MethodInfo)memberInfo).ReturnType.Name}",
                _ => string.Empty,
            };
        }

        /// <summary>
        /// check if target propertyInfo is static 
        /// </summary>
        public static bool IsStatic(this PropertyInfo propertyInfo)
        {

            return propertyInfo.GetMethod?.IsStatic ?? propertyInfo.SetMethod?.IsStatic ?? false;
        }

        /// <summary>
        /// create a stack property for given propertyInfo
        /// </summary>
        public static StackProperty CreateStackProperty(this PropertyInfo propertyInfo, string name)
        {

            if (!propertyInfo.IsStatic()) return null;
            if (propertyInfo.GetIndexParameters().Length > 0) return null;
            if (propertyInfo.SetMethod == null)
            {
                if (propertyInfo.GetMethod == null) return null;
                return new StackPropertyPropertyOnlyGetter(name, null, propertyInfo);
            }
            if (propertyInfo.GetMethod == null)
            {
                return new StackPropertyPropertyOnlySetter(name, null, propertyInfo);
            }
            return new StackPropertyProperty(name, null, propertyInfo);
        }

        /// <summary>
        /// create a stack property for given propertyInfo
        /// </summary>
        public static StackProperty CreateStackProperty(this PropertyInfo propertyInfo, object instance, string name)
        {

            if (instance == null) return propertyInfo.CreateStackProperty(name);
            if (propertyInfo.GetIndexParameters().Length > 0) return null;
            if (propertyInfo.SetMethod == null)
            {
                if (propertyInfo.GetMethod == null) return null;
                return new StackPropertyPropertyOnlyGetter(name, instance, propertyInfo);
            }
            if (propertyInfo.GetMethod == null)
            {
                return new StackPropertyPropertyOnlySetter(name, instance, propertyInfo);
            }
            return new StackPropertyProperty(name, instance, propertyInfo);
        }

        /// <summary>
        /// create a stack property for given propertyInfo
        /// </summary>
        public static StackProperty CreateStackProperty(this PropertyInfo propertyInfo, string name, string description, string tag)
        {

            if (!propertyInfo.IsStatic()) return null;
            if (propertyInfo.GetIndexParameters().Length > 0) return null;
            if (propertyInfo.SetMethod == null)
            {
                if (propertyInfo.GetMethod == null) return null;
                return new StackPropertyPropertyOnlyGetter(name, description, tag, null, propertyInfo);
            }
            if (propertyInfo.GetMethod == null)
            {
                return new StackPropertyPropertyOnlySetter(name, description, tag, null, propertyInfo);
            }
            return new StackPropertyProperty(name, description, tag, null, propertyInfo);
        }

        /// <summary>
        /// create a stack property for given propertyInfo
        /// </summary>
        public static StackProperty CreateStackProperty(this PropertyInfo propertyInfo, object instance, string name, string description, string tag)
        {

            if (instance == null) return propertyInfo.CreateStackProperty(name, description, tag);
            if (propertyInfo.GetIndexParameters().Length > 0) return null;
            if (propertyInfo.SetMethod == null)
            {
                if (propertyInfo.GetMethod == null) return null;
                return new StackPropertyPropertyOnlyGetter(name, description, tag, instance, propertyInfo);
            }
            if (propertyInfo.GetMethod == null)
            {
                return new StackPropertyPropertyOnlySetter(name, description, tag, instance, propertyInfo);
            }
            return new StackPropertyProperty(name, description, tag, instance, propertyInfo);
        }

        /// <summary>
        /// create a stack property for given fieldInfo
        /// </summary>
        public static StackProperty CreateStackProperty(this FieldInfo fieldInfo, string name)
        {

            if (fieldInfo.IsStatic) return new StackPropertyField(name, null, fieldInfo);
            return null;
        }

        /// <summary>
        /// create a stack property for given fieldInfo
        /// </summary>
        public static StackProperty CreateStackProperty(this FieldInfo fieldInfo, object instance, string name)
        {

            if (instance == null) return fieldInfo.CreateStackProperty(name);
            return new StackPropertyField(name, instance, fieldInfo);
        }

        public static StackProperty CreateStackProperty(this FieldInfo fieldInfo, string name, string description, string tag)
        {

            if (fieldInfo.IsStatic) return new StackPropertyField(name, description, tag, null, fieldInfo);
            return null;
        }

        public static StackProperty CreateStackProperty(this FieldInfo fieldInfo, object instance, string name, string description, string tag)
        {

            if (instance == null) return fieldInfo.CreateStackProperty(name, description, tag);
            return new StackPropertyField(name, description, tag, instance, fieldInfo);
        }


        /// <summary>
        /// create a stack callable from given methodInfo
        /// </summary>
        public static StackCallable CreateStackCallable(this MethodInfo methodInfo)
        {

            if (methodInfo.IsStatic) return new StackMethod(null, methodInfo);
            return null;
        }

        /// <summary>
        /// create a stack callable from given methodInfo
        /// </summary>
        public static StackCallable CreateStackCallable(this MethodInfo methodInfo, object instance)
        {

            if (instance == null) return methodInfo.CreateStackCallable();
            return new StackMethod(instance, methodInfo);
        }
    }

}