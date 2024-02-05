/*
Name: VirtualMachine.cs
Time: @2024.01.26
Auth: Prince Biscuit/王子饼干
Desc: A simple virtual machine to execute syntax tree parsed by SyntaxAnalyzer.
      it use C#'s reflection to access properties and methods, no other dependencices.
      lightweight and easy to use. it highly decoupled with Console part, so it could be used in other projects singlely.
      also because using reflection, it's not fast enough, so it's usually used in console input to do 
      fast and simple operations, like set/get property, call method, etc.
      in this case, it's fast enough and very useful.
*/


using System;
using System.Reflection;
using System.Collections.Generic;

namespace RedSaw.CommandLineInterface
{

    /// <summary>
    /// mark any object with a value getter method
    /// </summary>
    public interface IValueGetter
    {

        /// <summary> type of target value </summary>
        Type ValueType { get; }

        /// <summary> get value </summary>
        object GetValue();
    }

    /// <summary>
    /// mark any object with a value setter method
    /// </summary>
    public interface IValueSetter
    {

        /// <summary> the decleared name of target field or property </summary>
        string Name { get; }

        /// <summary> type of target value </summary>
        Type ValueType { get; }

        /// <summary> set value </summary>
        void SetValue(object value);
    }

    public enum StackObjectType
    {

        /// <summary>
        /// an object which maintain a value and provide only getter
        /// </summary>
        ValueGetter,

        /// <summary>
        /// an object which maintain a value and provide only setter
        /// </summary>
        ValueSetter,

        /// <summary>
        /// an object maintance a value and provide both getter and setter
        /// </summary>
        ValueProperty,

        /// <summary>
        /// an object which can be called
        /// </summary>
        Callable,

        /// <summary>
        /// unknown type
        /// </summary>
        Any
    }

    /// <summary>
    /// represent the base value type of VirtualMachine stack, use to 
    /// wrap a value or a callable object
    /// </summary>
    public abstract class StackObject
    {

        /// <summary>
        /// used to identify the type of stack object
        /// </summary>
        public readonly StackObjectType stackType;

        public StackObject(StackObjectType stackType)
        {

            this.stackType = stackType;
        }
    }

    /// <summary>
    /// the stack object which maintain a value and provide both getter and setter,
    /// </summary>
    public abstract class StackProperty : StackObject, IValueGetter, IValueSetter
    {

        public readonly string name;
        public readonly string description;
        public readonly string tag;

        public StackProperty(string name, string description, string tag) : base(StackObjectType.ValueProperty)
        {

            this.name = name;
            this.description = description;
            this.tag = tag;
        }
        public StackProperty(string name) : base(StackObjectType.ValueProperty)
        {

            this.name = name;
            this.description = string.Empty;
            this.tag = null;
        }


        public virtual string Name => name;

        /// <summary> the type of target value </summary>
        public abstract Type ValueType { get; }

        /// <summary> value getter </summary>
        public abstract object GetValue();

        /// <summary> value setter </summary>
        public abstract void SetValue(object value);

        /// <summary>
        /// check if target tag is same as this tag, if this tag is null, then return true
        /// </summary>
        public bool CompareTag(string tag)
        {

            if (this.tag == null) return true;
            return this.tag == tag;
        }
    }

    /// <summary>
    /// StackProperty implementation based on Reflection.PropertyInfo,
    /// </summary>
    public class StackPropertyProperty : StackProperty
    {

        public readonly PropertyInfo propertyInfo;
        public readonly object instance;
        public override Type ValueType => propertyInfo.PropertyType;

        public StackPropertyProperty(string name, string description, string tag, object instance, PropertyInfo propertyInfo)
        : base(name, description, tag)
        {

            this.instance = instance;
            this.propertyInfo = propertyInfo;
        }
        public StackPropertyProperty(string name, object instance, PropertyInfo propertyInfo) : base(name)
        {

            this.instance = instance;
            this.propertyInfo = propertyInfo;
        }
        public override object GetValue() => propertyInfo.GetValue(instance);
        public override void SetValue(object value) => propertyInfo.SetValue(instance, value);
    }

    /// <summary>
    /// created from PropertyInfo which has only getter
    /// </summary>
    public class StackPropertyPropertyOnlyGetter : StackPropertyProperty
    {
        public StackPropertyPropertyOnlyGetter(string name, object instance, PropertyInfo propertyInfo) :
        base(name, instance, propertyInfo)
        { }

        public StackPropertyPropertyOnlyGetter(string name, string description, string tag, object instance, PropertyInfo propertyInfo) :
        base(name, description, tag, instance, propertyInfo)
        { }

        public override Type ValueType => propertyInfo.PropertyType;
        public override object GetValue() => propertyInfo.GetValue(instance);
        public override void SetValue(object value)
        {
            throw new CommandExecuteException($"property \"{propertyInfo.Name} ({propertyInfo.PropertyType})\" has no setter");
        }
    }

    /// <summary>
    /// created from PropertyInfo which has only setter
    /// </summary>
    public class StackPropertyPropertyOnlySetter : StackPropertyProperty
    {

        public StackPropertyPropertyOnlySetter(string name, object instance, PropertyInfo propertyInfo) :
        base(name, instance, propertyInfo)
        { }

        public StackPropertyPropertyOnlySetter(string name, string description, string tag, object instance, PropertyInfo propertyInfo) :
        base(name, description, tag, instance, propertyInfo)
        { }

        public override Type ValueType => propertyInfo.PropertyType;
        public override object GetValue()
        {
            throw new CommandExecuteException($"property \"{propertyInfo.Name} ({propertyInfo.PropertyType})\" has no getter");
        }
        public override void SetValue(object value) => propertyInfo.SetValue(instance, value);
    }



    /// <summary>
    /// StackProperty implementation based on Reflection.FieldInfo
    /// </summary>
    public class StackPropertyField : StackProperty
    {

        public readonly FieldInfo fieldInfo;
        public readonly object instance;

        public override Type ValueType => fieldInfo.FieldType;

        public StackPropertyField(string name, string description, string tag, object instance, FieldInfo fieldInfo) : base(name, description, tag)
        {

            this.instance = instance;
            this.fieldInfo = fieldInfo;
        }
        public StackPropertyField(string name, object instance, FieldInfo fieldInfo) : base(name)
        {
            this.instance = instance;
            this.fieldInfo = fieldInfo;
        }
        public override object GetValue()
        {
            return fieldInfo.GetValue(instance);
        }
        public override void SetValue(object value)
        {
            fieldInfo.SetValue(instance, value);
        }
    }

    /// <summary>
    /// a custom property which has no target, it only maintain a value, 
    /// use to store local variables
    /// </summary>
    public class StackPropertyCustom : StackProperty
    {
        private object value;
        public override Type ValueType
        {
            get
            {
                if (value == null) return typeof(void);
                return value.GetType();
            }
        }
        public StackPropertyCustom(string name) : base(name)
        {
            this.value = null;
        }
        public StackPropertyCustom(string name, object initValue) : base(name)
        {
            this.value = initValue;
        }
        public override object GetValue()
        {
            return value;
        }
        public override void SetValue(object value)
        {
            this.value = value;
        }
    }

    /// <summary>
    /// the object has only getter, it can be used to wrap a value
    /// it represent literal or return value which cannot assigned to
    /// </summary>
    public class StackValue : StackObject, IValueGetter
    {

        public static StackValue Wrap(object value)
        {

            if (value == null) return StackNull.Default;
            return value switch
            {
                int intValue => new StackInt(intValue),
                float floatValue => new StackFloat(floatValue),
                string strValue => new StackString(strValue),
                bool boolValue => boolValue ? StackBool.True : StackBool.False,
                _ => new StackValue(value),
            };
        }

        public readonly object value;
        public virtual Type ValueType => value?.GetType() ?? typeof(void);
        public StackValue(object value) : base(StackObjectType.ValueGetter)
        {
            this.value = value;
        }
        public object GetValue()
        {
            return value;
        }
    }
    public class StackFloat : StackValue
    {

        public override Type ValueType => typeof(float);
        public readonly float floatValue;
        public StackFloat(float value) : base(value)
        {
            floatValue = value;
        }
    }
    public class StackInt : StackValue
    {

        public override Type ValueType => typeof(int);
        public readonly int intValue;
        public StackInt(int value) : base(value)
        {
            intValue = value;
        }
    }
    public class StackString : StackValue
    {

        public override Type ValueType => typeof(string);
        public readonly string strValue;
        public StackString(string value) : base(value)
        {
            strValue = value;
        }
    }
    public class StackBool : StackValue
    {

        public static StackBool True => new(true);
        public static StackBool False => new(false);

        public override Type ValueType => typeof(bool);
        public readonly bool boolValue;
        public StackBool(bool value) : base(value)
        {
            boolValue = value;
        }
    }
    public class StackNull : StackValue
    {

        public override Type ValueType => typeof(void);
        public static StackNull Default => new();
        public StackNull() : base(null) { }
    }

    /// <summary>
    /// a source input without any decorator like "" or @
    /// usually, system would treat it as a command call,
    /// but if it's not a command, then it would be parsed to a normal value accorrding to 
    /// required type, if required type is null, then it would be treated as a string
    /// it string is not suitable for target type, then system would raise en error to tell user
    /// <para> example1 : @a = xxx </para>
    /// <para> in example1, 'xxx' is a source input without any decorators, then it would be treated as
    /// an command calling, but if vm cannot found an command named 'xxx', then try to find suitable
    /// value parser to parse it to Type 'a', if not found suitable ValueParser, then vm simplely check
    /// if 'a' is string type, if failed all, vm would raise en CommandExecuteException</para>
    /// </summary>
    public class StackSourceInput : StackObject
    {

        /// <summary>the input string</summary>
        public readonly string inputStr;

        public StackSourceInput(string inputStr) : base(StackObjectType.Any)
        {
            this.inputStr = inputStr;
        }
        public object GetValue()
        {

            return inputStr;
        }
    }

    public abstract class StackCallable : StackObject
    {

        /// <summary> callable identifier </summary>
        public abstract string Name { get; }

        /// <summary> the return type of callable </summary>
        public abstract Type ReturnType { get; }

        /// <summary> parameter count </summary>
        public abstract int ParameterCount { get; }

        public StackCallable() : base(StackObjectType.Callable) { }

        /// <summary> get parameter information </summary>
        public abstract ParameterInfo GetParameter(int index);

        /// <summary> 
        /// try get parameter default value if target parameter is optional
        /// </summary>
        public abstract bool TryGetParameterDefaultValue(int index, out object defaultValue);

        /// <summary> invoke callable </summary>
        public abstract object Invoke(object[] args);

        /// <summary> check if callable has return value </summary>
        public bool HasReturnValue => ReturnType != typeof(void);

        /// <summary>
        /// MethodInfo or Delegate
        /// </summary>
        public abstract object Instance { get; }
    }


    /// <summary>
    /// callable object based on MethodInfo, it could be static or instance method.
    /// </summary>
    public class StackMethod : StackCallable
    {

        public readonly object instance;
        public readonly MethodInfo methodInfo;
        public readonly ParameterInfo[] parameters;

        public override string Name => methodInfo.Name;
        public override int ParameterCount => parameters.Length;
        public override Type ReturnType => methodInfo.ReturnType;
        public override object Instance => methodInfo;

        /// <summary>command parameter information</summary>
        public string ParameterInfo
        {

            get
            {
                if (parameters.Length == 0) return "()";
                string result = string.Empty;
                foreach (var parameter in parameters)
                {
                    result += $"{parameter.ParameterType.Name} {parameter.Name}, ";
                }
                return $"({result[..^2]})";
            }
        }

        /// <summary>instance method</summary>
        public StackMethod(object instance, MethodInfo methodInfo) : base()
        {

            this.instance = instance;
            this.methodInfo = methodInfo;
            parameters = methodInfo.GetParameters();
        }
        /// <summary>static method</summary>
        public StackMethod(MethodInfo methodInfo) : base()
        {

            this.methodInfo = methodInfo;
            if (!methodInfo.IsStatic)
            {
                throw new CommandExecuteException($"require static method but get instance method \"{methodInfo.Name}\" in \"{methodInfo.DeclaringType}\"");
            }
            parameters = methodInfo.GetParameters();
        }

        public override ParameterInfo GetParameter(int idx) => parameters[idx];

        public override bool TryGetParameterDefaultValue(int index, out object defaultValue)
        {

            if (index < 0 || index >= parameters.Length || !parameters[index].HasDefaultValue)
            {
                defaultValue = default;
                return false;
            }
            defaultValue = parameters[index].DefaultValue;
            return true;
        }

        public override object Invoke(object[] args)
        {
            return methodInfo.Invoke(instance, args);
        }
    }

    /// <summary>
    /// a delegate which can parse string to a value
    /// </summary>
    public delegate bool ValueParser(string inputStr, out object data);

    /// <summary>
    /// manage a group of value parsers
    /// </summary>
    public class ValueParserManager
    {

        private readonly Dictionary<Type, ValueParser> parsers = new();
        private readonly Dictionary<string, Type> typeAlias = new();

        public ValueParserManager() { }
        public ValueParserManager(Dictionary<Type, ValueParser> parsers, Dictionary<string, Type> typeAlias)
        {

            this.parsers = parsers;
            this.typeAlias = typeAlias;
        }

        /// <summary>
        /// register new parser, if target type is already registered, then it will be replaced
        /// </summary>
        public void RegisterParser(Type type, ValueParser parser, string typeAlias = null)
        {

            if (parsers.ContainsKey(type))
            {
                parsers[type] = parser;
            }
            else
            {
                parsers.Add(type, parser);
            }

            string alias = typeAlias ?? type.Name;
            if (this.typeAlias.ContainsKey(alias))
            {
                this.typeAlias[alias] = type;
            }
            else
            {
                this.typeAlias.Add(alias, type);
            }
        }
        public bool TryParse(Type type, string inputStr, out object data)
        {

            if (parsers.TryGetValue(type, out var parser))
            {
                return parser(inputStr, out data);
            }
            data = null;
            return false;
        }
        public bool TryParseSafe(Type type, string inputStr, out object data)
        {

            if (parsers.TryGetValue(type, out var parser))
            {
                try
                {
                    return parser(inputStr, out data);
                }
                catch (Exception)
                {
                    data = null;
                    return false;
                }
            }
            data = null;
            return false;
        }
        public bool QueryType(string alias, out Type type)
        {
            return typeAlias.TryGetValue(alias, out type);
        }
    }


    public partial class VirtualMachine
    {

        /// <summary>
        /// indicate the target of execute one node, 
        /// it would affect the behavior of Execute method
        /// </summary>
        public enum CallTarget
        {

            Unset,
            RequireCallable,
            RequireValue,
        }

        /// <summary>
        /// pass to Execute method to indicate the target and required type of execute one node
        /// </summary>
        public struct ExecuteStatus
        {

            public Type requiredType;
            public CallTarget target;

            public ExecuteStatus(Type requiredType, CallTarget target)
            {

                this.target = target;
                this.requiredType = requiredType;
            }
        }

        /// <summary>
        /// <para>while execute command "func() = 999"</para>
        /// usually, we assgin value to object with a setter interface, but if 
        /// target value has no setter interface, the flag will determine if 
        /// vm should ignore this error or not
        /// </summary>
        private readonly bool IgnoreInvalidSetBehaviour;

        /// <summary>
        /// while execute command input "a = myfunc()"
        /// <para>
        /// if "myfunc" has no return value, 
        /// then we should set a 'null' value to 'a'
        /// </para>
        /// </summary>
        private readonly bool TreatVoidAsNull;

        /// <summary>
        /// <para>while execute command "@myValue = myFunc()"</para>
        /// if 'myValue' is string type and 'myFunc' return a non-string value,
        /// this flag will determine if vm should convert return value to string type
        /// </summary>
        private readonly bool ReceiveValueFromNonStringType;

        private readonly Stack<StackObject> _stack = new();

        /// <summary> readonly, you cannot custom this parser manager </summary>
        private readonly ValueParserManager parsersDefault = new(
            DefaultTypeParserDefination.DefaultTypeParser,
            DefaultTypeParserDefination.DefaultTypeAlias
        );
        private readonly ValueParserManager parsersCustom = new();

        private readonly Dictionary<string, StackProperty> properties = new();
        private readonly Dictionary<string, StackPropertyCustom> localVariables = new();
        private readonly Dictionary<string, StackCallable> callables = new();

        public IEnumerable<StackCallable> AllCallables => callables.Values;
        public IEnumerable<StackProperty> AllProperties
        {
            get
            {
                foreach (var p in properties.Values) yield return p;
                foreach (var p in localVariables.Values) yield return p;
            }
        }

        /// <summary> get top value of stack </summary>
        /// <exception cref="CommandExecuteException">if stack is empty, then raise an exception</exception>
        public StackObject TopValue
        {
            get
            {
                if (_stack.Count > 0) return _stack.Pop();
                throw new CommandExecuteException("fatal stack error");
            }
        }

        /// <summary>
        /// create a virtual machine
        /// </summary>

        /// <param name="IgnoreInvalidSetBehaviour">
        /// <para>while execute command "func() = 999"</para>
        /// usually, we assgin value to object with a setter interface, but if 
        /// target value has no setter interface, the flag will determine if 
        /// vm should ignore this error or not
        /// </param>

        /// <param name="ReceiveValueFromNonStringType">
        /// <para>while execute command "@myValue = myFunc()"</para>
        /// if 'myValue' is string type and 'myFunc' return a non-string value,
        /// this flag will determine if vm should convert return value to string type
        /// </param>

        /// <param name="TreatVoidAsNull">
        /// while execute command input "a = myfunc()"
        /// <para>
        /// if "myfunc" has no return value, 
        /// then we should set a 'null' value to 'a'
        /// </para>
        /// </param>
        public VirtualMachine(
            bool IgnoreInvalidSetBehaviour = true,
            bool ReceiveValueFromNonStringType = true,
            bool TreatVoidAsNull = true
        )
        {
            this.IgnoreInvalidSetBehaviour = IgnoreInvalidSetBehaviour;
            this.ReceiveValueFromNonStringType = ReceiveValueFromNonStringType;
            this.TreatVoidAsNull = TreatVoidAsNull;
        }

        /// <summary>
        /// register custom value parser to vm
        /// </summary>
        public void RegisterValueParser(Type type, ValueParser parser, string typeAlias = null)
        {

            parsersCustom.RegisterParser(type, parser, typeAlias);
        }

        /// <summary>
        /// register callable to vm
        /// </summary>
        /// <param name="callable">the callable object</param>
        /// <param name="shouldOverwrite">if true, vm will overwrite callable with same name</param>
        public bool RegisterCallable(StackCallable callable, bool shouldOverwrite = true)
        {

            if (callables.ContainsKey(callable.Name))
            {
                if (shouldOverwrite)
                {
                    callables[callable.Name] = callable;
                    return true;
                }
                return false;
            }
            callables.Add(callable.Name, callable);
            return true;
        }

        /// <summary>
        /// register property to vm
        /// </summary>
        /// <param name="property">the property object</param>
        /// <param name="shouldOverwrite">if true, vm will overwrite property with same name</param>
        public bool RegisterProperty(StackProperty property, bool shouldOverwrite = true)
        {

            if (properties.ContainsKey(property.Name))
            {
                if (shouldOverwrite)
                {
                    properties[property.Name] = property;
                    return true;
                }
                return false;
            }
            properties.Add(property.Name, property);
            return true;
        }
        /// <summary>
        /// get local variable value by name
        /// </summary>
        public object GetLocalVariable(string name)
        {

            if (localVariables.TryGetValue(name, out var property))
            {
                return property.GetValue();
            }
            return null;
        }

        /// <summary>
        /// set local variable value by name, if given name not existed, 
        /// then create a new one
        /// </summary>
        public void SetLocalVariable(string name, object value)
        {

            if (localVariables.TryGetValue(name, out var property))
            {
                property.SetValue(value);
                return;
            }
            var propertyHandle = new StackPropertyCustom(name, value);
            localVariables.Add(name, propertyHandle);
        }

        /// <summary></summary>
        public Type GetPropertyType(string propertyName)
        {

            if (localVariables.TryGetValue(propertyName, out var propertyLocal))
            {
                return propertyLocal.ValueType;
            }
            if (properties.TryGetValue(propertyName, out var property))
            {
                return property.ValueType;
            }
            return null;
        }

        public Type GetCallableType(string name)
        {

            if (callables.TryGetValue(name, out var callable))
            {
                return callable.Instance?.GetType();
            }
            return null;
        }

        void SetProperty(IValueSetter propertyHandle, object value, string DebugInfo)
        {

            /* in any situation, local variable would receive value */
            if (propertyHandle is StackPropertyCustom customProperty)
            {
                customProperty.SetValue(value);
                return;
            }

            /* check if target is nullable */
            if (value == null)
            {
                if (propertyHandle.ValueType.IsNullable())
                {
                    propertyHandle.SetValue(null);
                    return;
                }
                throw new CommandExecuteException($"cannot assign \"null\" to \"{propertyHandle.Name}\" near \"{DebugInfo}\"");
            }

            /* while value is compatible, just write in*/
            var valueType = value.GetType();
            if (propertyHandle.ValueType.IsAssignableFrom(valueType))
            {
                propertyHandle.SetValue(value);
                return;
            }

            // 当目标类型是字符串且允许从非字符串类型接受值时，将值转换为字符串
            if (propertyHandle.ValueType == typeof(string))
            {
                if (ReceiveValueFromNonStringType)
                {
                    propertyHandle.SetValue(value.ToString());
                    return;
                }
                throw new CommandExecuteException($"cannot assign \"{value}({value.GetType()})\" to \"{propertyHandle.Name} (string)\" near \"{DebugInfo}\"");
            }

            // 当值类型为字符串类型且目标类型不是字符串时，尝试将字符串解析为目标类型
            if (valueType == typeof(string))
            {
                string input = (string)value;
                if (TryParseInput(input, propertyHandle.ValueType, out value))
                {
                    propertyHandle.SetValue(value);
                    return;
                }
                throw new CommandExecuteException($"cannot parse \"{input}\" to type \"{propertyHandle.ValueType}\" near \"{DebugInfo}\"");
            }
            throw new CommandExecuteException($"cannot assign \"{value} ({value.GetType()})\" to {propertyHandle.Name} ({propertyHandle.ValueType}) near \"{DebugInfo}\"");
        }

        bool TryParseInput(string input, Type type, out object data)
        {

            if (parsersCustom.TryParseSafe(type, input, out data)) return true;
            return parsersDefault.TryParse(type, input, out data);
        }

        /// <summary>
        /// get type by alias, if not found, return null
        /// </summary>
        Type TryGetTypeByAlias(string alias)
        {

            if (parsersCustom.QueryType(alias, out var type)) return type;
            return parsersDefault.QueryType(alias, out type) ? type : null;
        }

        bool TryLoadVariable(string name)
        {

            if (properties.TryGetValue(name, out var property))
            {
                _stack.Push(property);
                return true;
            }
            if (localVariables.TryGetValue(name, out var propertyCustom))
            {
                _stack.Push(propertyCustom);
                return true;
            }
            return false;
        }

        /// <summary></summary>
        object UnwrapStackObjectNoRequire(StackObject container)
        {

            if (container is IValueGetter valueGetter)
            {

                return valueGetter.GetValue();
            }
            if (container is StackCallable callable)
            {

                return callable.Instance;
            }
            if (container is StackSourceInput inputContainer)
            {

                return inputContainer.inputStr;
            }
            return null;
        }
    }

    public partial class VirtualMachine
    {

        /// <summary>
        /// execute a syntax tree, if stack is not empty, then return the top value
        /// else return null
        /// </summary>
        public object ExecuteRoot(SyntaxTree node)
        {

            Execute(node, default);
            if (_stack.Count == 0) return null;
            var topValue = TopValue;
            _stack.Clear();

            if (topValue is IValueGetter valueGetter)
            {
                return valueGetter.GetValue();

            }
            else if (topValue is StackCallable callable)
            {

                return callable.Instance;
            }
            else if (topValue is StackSourceInput inputContainer)
            {

                return inputContainer.inputStr;
            }
            return null;
        }


        void Execute(SyntaxTree node, ExecuteStatus status = default)
        {

            switch (node.opcode)
            {

                #region Operations
                case SyntaxTreeCode.OP_SET_FIELD:
                    ExecuteSetField(node, status);
                    break;

                case SyntaxTreeCode.OP_SET_ELEMENT:
                    ExecuteSetElement(node, status);
                    break;

                case SyntaxTreeCode.OP_ASSIGN:
                    ExecuteAssign(node, status);
                    break;

                case SyntaxTreeCode.OP_LOADVAR:
                    ExecuteLoadVariable(node, status);
                    break;

                case SyntaxTreeCode.OP_INDEX:
                    ExecuteIndex(node, status);
                    break;

                case SyntaxTreeCode.OP_CALL:
                    ExecuteCall(node, status);
                    break;

                case SyntaxTreeCode.OP_DOT:
                    ExecuteDot(node, status);
                    break;

                case SyntaxTreeCode.OP_CVT:
                    ExecuteConvert(node, status);
                    break;

                #endregion

                #region Factors

                case SyntaxTreeCode.FACTOR_FLOAT:
                    ExecuteFactorFloat(node, status);
                    break;

                case SyntaxTreeCode.FACTOR_INT:
                    ExecuteFactorInt(node, status);
                    break;

                case SyntaxTreeCode.FACTOR_TRUE:
                    ExecuteFactorTrue(node, status);
                    break;

                case SyntaxTreeCode.FACTOR_FALSE:
                    ExecuteFactorFalse(node, status);
                    break;

                case SyntaxTreeCode.FACTOR_NULL:
                    ExecuteFactorNull(node, status);
                    break;

                case SyntaxTreeCode.FACTOR_STRING:
                    ExecuteFactorString(node, status);
                    break;

                case SyntaxTreeCode.FACTOR_INPUT:
                    ExecuteFactorInput(node, status);
                    break;
                    #endregion
            }
        }

        void ExecuteFactorFloat(SyntaxTree node, ExecuteStatus status)
        {

            switch (status.target)
            {
                case CallTarget.RequireValue:
                    if (status.requiredType != null)
                    {
                        if (status.requiredType.IsAssignableFrom(typeof(float)))
                        {
                            _stack.Push(new StackFloat(float.Parse(node.data)));
                            return;
                        }
                        if (TryParseInput(node.data, status.requiredType, out var data))
                        {
                            _stack.Push(StackValue.Wrap(data));
                            return;
                        }
                        throw new CommandExecuteException($"cannot convert \"{node.data}\" to type \"{status.requiredType}\" near \"{node.DebugInfo}\"");
                    }
                    /* wait other process to handle it */
                    _stack.Push(new StackFloat(float.Parse(node.data)));
                    return;

                case CallTarget.RequireCallable:
                    if (callables.TryGetValue(node.data, out var callable))
                    {
                        _stack.Push(callable);
                        return;
                    }
                    throw new CommandExecuteException($"cannot find callable named \"{node.data}\" near \"{node.DebugInfo}\"");

                default:
                    _stack.Push(new StackFloat(float.Parse(node.data)));
                    return;
            }
        }
        void ExecuteFactorInt(SyntaxTree node, ExecuteStatus status)
        {

            switch (status.target)
            {
                case CallTarget.RequireValue:
                    if (status.requiredType != null)
                    {
                        if (status.requiredType.IsAssignableFrom(typeof(int)))
                        {
                            _stack.Push(new StackInt(int.Parse(node.data)));
                            return;
                        }
                        if (TryParseInput(node.data, status.requiredType, out var data))
                        {
                            _stack.Push(StackValue.Wrap(data));
                            return;
                        }
                        throw new CommandExecuteException($"cannot convert \"{node.data}\" to type \"{status.requiredType}\" near \"{node.DebugInfo}\"");
                    }
                    else
                    {
                        _stack.Push(new StackInt(int.Parse(node.data)));
                        return;
                    }

                case CallTarget.RequireCallable:
                    if (callables.TryGetValue(node.data, out var callable))
                    {
                        _stack.Push(callable);
                        return;
                    }
                    throw new CommandExecuteException($"cannot find callable named \"{node.data}\" near \"{node.DebugInfo}\"");

                default:
                    _stack.Push(new StackInt(int.Parse(node.data)));
                    return;
            }
        }
        void ExecuteFactorTrue(SyntaxTree node, ExecuteStatus status)
        {
            switch (status.target)
            {
                case CallTarget.RequireValue:
                    if (status.requiredType != null)
                    {
                        if (status.requiredType.IsAssignableFrom(typeof(bool)))
                        {
                            _stack.Push(StackBool.True);
                            return;
                        }
                        if (TryParseInput(node.data, status.requiredType, out var data))
                        {
                            _stack.Push(StackValue.Wrap(data));
                            return;
                        }
                        throw new CommandExecuteException($"cannot convert \"{node.data}\" to type \"{status.requiredType}\" near \"{node.DebugInfo}\"");
                    }
                    else
                    {
                        _stack.Push(StackBool.True);
                        return;
                    }

                case CallTarget.RequireCallable:
                    if (callables.TryGetValue(node.data, out var callable))
                    {
                        _stack.Push(callable);
                        return;
                    }
                    throw new CommandExecuteException($"cannot find callable named \"{node.data}\" near \"{node.DebugInfo}\"");

                default:
                    _stack.Push(StackBool.True);
                    return;
            }
        }
        void ExecuteFactorFalse(SyntaxTree node, ExecuteStatus status)
        {
            switch (status.target)
            {
                case CallTarget.RequireValue:
                    if (status.requiredType != null)
                    {
                        if (status.requiredType.IsAssignableFrom(typeof(bool)))
                        {
                            _stack.Push(StackBool.False);
                            return;
                        }
                        if (TryParseInput(node.data, status.requiredType, out var data))
                        {
                            _stack.Push(StackValue.Wrap(data));
                            return;
                        }
                        throw new CommandExecuteException($"cannot convert \"{node.data}\" to type \"{status.requiredType}\" near \"{node.DebugInfo}\"");
                    }
                    else
                    {
                        _stack.Push(StackBool.False);
                        return;
                    }

                case CallTarget.RequireCallable:
                    if (callables.TryGetValue(node.data, out var callable))
                    {
                        _stack.Push(callable);
                        return;
                    }
                    throw new CommandExecuteException($"cannot find callable named \"{node.data}\" near \"{node.DebugInfo}\"");

                default:
                    _stack.Push(StackBool.False);
                    return;
            }
        }
        void ExecuteFactorNull(SyntaxTree node, ExecuteStatus status)
        {
            switch (status.target)
            {
                case CallTarget.RequireValue:
                    if (status.requiredType != null)
                    {
                        if (status.requiredType.IsNullable())
                        {
                            _stack.Push(StackNull.Default);
                            return;
                        }
                        if (TryParseInput(node.data, status.requiredType, out var data))
                        {
                            _stack.Push(StackValue.Wrap(data));
                            return;
                        }
                        throw new CommandExecuteException($"cannot convert \"{node.data}\" to type \"{status.requiredType}\" near \"{node.DebugInfo}\"");
                    }
                    else
                    {
                        _stack.Push(StackNull.Default);
                        return;
                    }

                case CallTarget.RequireCallable:
                    if (callables.TryGetValue(node.data, out var callable))
                    {
                        _stack.Push(callable);
                        return;
                    }
                    throw new CommandExecuteException($"cannot find callable named \"{node.data}\" near \"{node.DebugInfo}\"");

                default:
                    _stack.Push(StackNull.Default);
                    return;
            }
        }

        bool HandleString(string input, ExecuteStatus status, string DebugInfo)
        {

            if (status.target == CallTarget.RequireValue)
            {

                if (status.requiredType != null)
                {
                    if (status.requiredType.IsAssignableFrom(typeof(string)))
                    {
                        _stack.Push(new StackString(input));
                        return true;
                    }

                    if (TryParseInput(input, status.requiredType, out var data))
                    {
                        _stack.Push(StackValue.Wrap(data));
                        return true;
                    }
                    throw new CommandExecuteException($"cannot parse \"{input}\" to type \"{status.requiredType}\" near \"{DebugInfo}\"");
                }

                if (callables.TryGetValue(input, out var command))
                {

                    // the method can only call when it has no parameter
                    if (command.ParameterCount == 0)
                    {
                        object methodReturn = command.Invoke(Array.Empty<object>());
                        _stack.Push(StackValue.Wrap(methodReturn));
                        return true;
                    }
                }

                return false;
            }

            if (status.target == CallTarget.RequireCallable && callables.TryGetValue(input, out var callable))
            {
                _stack.Push(callable);
                return true;
            }

            return false;
        }

        void ExecuteFactorInput(SyntaxTree node, ExecuteStatus status)
        {

            if (HandleString(node.data, status, node.DebugInfo)) return;
            _stack.Push(new StackSourceInput(node.data));
        }
        void ExecuteFactorString(SyntaxTree node, ExecuteStatus status)
        {

            if (HandleString(node.data, status, node.DebugInfo)) return;
            _stack.Push(new StackString(node.data));
        }

        void ExecuteSetField(SyntaxTree node, ExecuteStatus status)
        {

            // get instance
            Execute(node.children[0], new ExecuteStatus { target = CallTarget.RequireValue });
            var topValue = TopValue;
            var instance = UnwrapStackObjectNoRequire(topValue)
                ?? throw new CommandExecuteException($"cannot write value to \"null.{node.children[1].data}\" near \"{node.DebugInfo}\"");
            var instanceType = instance.GetType();

            // get instance.member type and property
            var memberName = node.children[1].data;
            var member = instanceType.GetDefaultMember(memberName)
                ?? throw new CommandExecuteException($"\"{node.children[0].data}\" has no member named \"{memberName}\" near \"{node.DebugInfo}\"");
            Type memberType;
            StackProperty instancePropertyHandle;
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    var fld = (FieldInfo)member;
                    memberType = fld.FieldType;
                    instancePropertyHandle = fld.CreateStackProperty(instance, memberName) ??
                        throw new CommandExecuteException($"cannot write value to \"{memberName}\" of \"{instanceType}\" near \"{node.DebugInfo}\"");
                    break;

                case MemberTypes.Property:
                    var ppt = (PropertyInfo)member;
                    memberType = ppt.PropertyType;
                    instancePropertyHandle = ppt.CreateStackProperty(instance, memberName) ??
                        throw new CommandExecuteException($"cannot write value to \"{memberName}\" of \"{instanceType}\" near \"{node.DebugInfo}\"");
                    break;

                default:
                    throw new CommandExecuteException($"cannot write value to \"{node.children[0].data}.{memberName}\" near \"{node.DebugInfo}\"");
            }

            // set value to instance
            Execute(node.children[2], new ExecuteStatus { target = CallTarget.RequireValue, requiredType = memberType });
            var stackValue = TopValue;
            object value;
            if (stackValue is IValueGetter valueGetter)
            {
                value = valueGetter.GetValue();

            }
            else if (stackValue is StackSourceInput inputContainer)
            {

                if (TryParseInput(inputContainer.inputStr, memberType, out var data))
                {
                    instancePropertyHandle.SetValue(data);
                    return;
                }
                throw new CommandExecuteException($"cannot parse \"{inputContainer.inputStr}\" to type {memberType} near \"{node.DebugInfo}\"");
            }
            else
            {
                throw new CommandExecuteException($"cannot assign <{stackValue.stackType}> to {stackValue.stackType} near \"{node.DebugInfo}\"");
            }
            SetProperty(instancePropertyHandle, value, node.DebugInfo);
        }

        void ExecuteLoadVariable(SyntaxTree node, ExecuteStatus status)
        {

            var variableName = node.data;
            if (TryLoadVariable(variableName)) return;
            var propertyHandle = new StackPropertyCustom(variableName);
            localVariables.Add(variableName, propertyHandle);
            _stack.Push(propertyHandle);
        }

        void ExecuteDot(SyntaxTree node, ExecuteStatus status)
        {

            // get instance
            Execute(node.children[0], new ExecuteStatus { target = CallTarget.RequireValue });
            var stackValue = TopValue;

            object instance;
            if (stackValue is IValueGetter valueGetter)
            {

                instance = valueGetter.GetValue() ?? throw new CommandExecuteException($"cannot get member from null near \"{node.DebugInfo}\"");

            }
            else if (stackValue is StackCallable callable)
            {

                instance = callable.Instance;
            }
            else
            {
                /* try parse inputContainer */
                if (stackValue is StackSourceInput inputContainer)
                {
                    instance = inputContainer.inputStr;
                }
                else
                {
                    throw new CommandExecuteException($"cannot get member from <{stackValue.stackType}> near \"{node.DebugInfo}\"");
                }
            }
            var instanceType = instance.GetType();

            // dot instance.member
            var memberName = node.children[1].data;
            var member = instanceType.GetDefaultMember(memberName)
                ?? throw new CommandExecuteException($"\"{instanceType}\" has no member named \"{memberName}\" near \"{node.DebugInfo}\"");
            StackObject result = member.MemberType switch
            {
                /* create property from FieldInfo */
                MemberTypes.Field => ((FieldInfo)member).CreateStackProperty(instance, memberName) ??
                    throw new CommandExecuteException($"invalid member dot \"{memberName}\" from {stackValue.GetType()} around \"{node.DebugInfo}\""),

                /* create property from PropertyInfo */
                MemberTypes.Property => ((PropertyInfo)member).CreateStackProperty(instance, memberName) ??
                    throw new CommandExecuteException($"invalid member dot \"{memberName}\" from {stackValue.GetType()} around \"{node.DebugInfo}\""),

                /* create method from method */
                MemberTypes.Method => ((MethodInfo)member).CreateStackCallable(instance) ??
                    throw new CommandExecuteException($"invalid member dot \"{memberName}\" from {stackValue.GetType()} around \"{node.DebugInfo}\""),

                _ => throw new CommandExecuteException($"cannot get member \"{memberName}\" from {stackValue.GetType()} around \"{node.DebugInfo}\""),
            };
            _stack.Push(result);
        }

        /// <summary>
        /// run a callable object, which maybe a MethodInfo or a static method marked by CommandAttribute
        /// while execute this object, it will push return value to stack, if the method has no return value,
        /// </summary>
        void ExecuteCall(SyntaxTree node, ExecuteStatus status = default)
        {

            // get callable
            Execute(node.children[0], new ExecuteStatus { target = CallTarget.RequireCallable });
            var stackValue = TopValue;
            if (stackValue is StackCallable callable)
            {

                // execute parameter nodes
                var paramsNode = node.children[1];
                var paramlist = new object[callable.ParameterCount];
                for (int i = 0; i < callable.ParameterCount; i++)
                {
                    var parameter = callable.GetParameter(i);
                    var parameterType = parameter.ParameterType;
                    if (i < paramsNode.children.Length)
                    {
                        /* there is a given value */

                        Execute(paramsNode.children[i], new ExecuteStatus
                        {
                            target = CallTarget.RequireValue,
                            requiredType = parameterType
                        });

                        // get value from stack and check if param type is valid
                        stackValue = TopValue;
                        if (stackValue is IValueGetter valueGetter)
                        {
                            object value = valueGetter.GetValue();
                            if (value == null)
                            {
                                if (parameterType.IsNullable())
                                {
                                    paramlist[i] = null;
                                    continue;
                                }
                                throw new CommandExecuteException($"invalid parameter value \"null\" for \"{parameter.Name}\" ({parameterType}) near \"{node.DebugInfo}\"");
                            }
                            var valueType = value.GetType();

                            if (parameterType.IsAssignableFrom(valueType))
                            {
                                paramlist[i] = value;
                                continue;
                            }

                            /* NOTE> 当获取的参数类型无法直接转换为需求类型时，尝试获取其string类型并利用类型解析器将string类型解析为可用的目标类型 */
                            string valueStr = value.ToString()
                                ?? throw new CommandExecuteException($"invalid parameter type \"{value.GetType()}\" for \"{parameter.Name}\" ({parameterType}) near \"{node.DebugInfo}\"");
                            if (TryParseInput(valueStr, parameterType, out var data))
                            {
                                paramlist[i] = data;
                                continue;
                            }

                            /* unable to handle value to parameter type */
                            throw new CommandExecuteException($"invalid parameter type \"{value.GetType()}\" for \"{parameter.Name}\" ({parameterType}) near \"{node.DebugInfo}\"");
                        }
                        else if (stackValue is StackCallable callableParamter)
                        {

                            object value = callableParamter.Instance;
                            if (parameterType.IsAssignableFrom(value.GetType()))
                            {
                                paramlist[i] = value;
                                continue;
                            }
                            throw new CommandExecuteException($"invalid parameter type \"{value.GetType()}\" for \"{parameter.Name}\" ({parameterType}) near \"{node.DebugInfo}\"");
                        }
                        else if (stackValue is StackSourceInput inputContainerParam)
                        {

                            string input = inputContainerParam.inputStr;
                            if (parameterType == typeof(string))
                            {
                                paramlist[i] = input;
                                continue;
                            }
                            if (TryParseInput(input, parameterType, out var data))
                            {
                                paramlist[i] = data;
                                continue;
                            }
                            throw new CommandExecuteException($"cannot parse \"{input}\" to type \"{parameterType}\" near \"{node.DebugInfo}\"");
                        }
                        else
                        {

                            if (parameterType.IsNullable())
                            {
                                paramlist[i] = null;
                                continue;
                            }
                            paramlist[i] = 0;
                        }
                    }
                    else
                    {
                        /* should check if there is a default value */

                        if (callable.TryGetParameterDefaultValue(i, out var defaultValue))
                        {
                            paramlist[i] = defaultValue;
                            continue;
                        }
                        throw new CommandExecuteException($"parameter \"{parameter.Name}\" of method \"{callable.Name}\" is not provided");
                    }
                }

                if (callable.HasReturnValue)
                {
                    _stack.Push(StackValue.Wrap(callable.Invoke(paramlist)));
                    return;
                }
                callable.Invoke(paramlist);
                if (TreatVoidAsNull) _stack.Push(StackNull.Default);
                return;
            }

            /* 
                the source input of a command call node is a string without any quotes and @ symbol,
                while it isn't a command, so VirtualMachine will try to parse it to a normal value with 
                registered parsers
            */
            if (stackValue is StackSourceInput inputContainer)
            {

                /* while not set required type, then it should be null, but 
                   i think it maybe user has input wrong characters, so i decide to 
                   raise en error instead of taking cover by null */
                // if(status.requireReturnType == null){
                //     stack.Push(StackNull.Default);
                //     return;
                // }
                string inputStr = inputContainer.inputStr;
                if (status.requiredType == null)
                {
                    throw new CommandExecuteException($"don't know how to handle input \"{inputStr}\" near \"{node.DebugInfo}\"");
                }

                /* <input> would be converted to string type only if required type is string type */
                if (status.requiredType == typeof(string))
                {
                    _stack.Push(new StackString(inputStr));
                    return;
                }

                /* try to parse <input> to required type */
                if (TryParseInput(inputStr, status.requiredType, out var data))
                {
                    _stack.Push(StackValue.Wrap(data));
                    return;
                }
                throw new CommandExecuteException($"cannot parse \"{inputStr}\" to type {status.requiredType} near \"{node.DebugInfo}\"");
            }

            throw new CommandExecuteException($"<{stackValue.stackType}> is not callable near \"{node.DebugInfo}\"");
        }





        /// <summary>
        /// assign value to target, the target can be a property or a local variable
        /// </summary>
        void ExecuteAssign(SyntaxTree node, ExecuteStatus status)
        {

            // get container
            Execute(node.children[0]);
            var stackValue = TopValue;
            if (stackValue is not IValueSetter valueSetter)
            {
                if (!IgnoreInvalidSetBehaviour)
                    throw new CommandExecuteException($"cannot assign value to <{stackValue.stackType}> near \"{node.DebugInfo}\"");
                Execute(node.children[1]);
                return;
            }

            // execute child node 2 and get a value to set
            if (valueSetter is StackPropertyCustom)
            {
                Execute(node.children[1], new ExecuteStatus { target = CallTarget.RequireValue });
            }
            else
            {
                Execute(node.children[1], new ExecuteStatus { target = CallTarget.RequireValue, requiredType = valueSetter.ValueType });
            }

            // set value
            // Execute(node.children[1], new ExecuteStatus{ target = CallTarget.RequireValue , requiredType = valueSetter.ValueType });
            stackValue = TopValue;
            object value;
            if (stackValue is IValueGetter valueGetter)
            {
                value = valueGetter.GetValue();

            }
            else if (stackValue is StackSourceInput inputContainer)
            {

                // if target is a input value, then it would be treated as a string value
                if (valueSetter is StackPropertyCustom customPropertyHandle)
                {
                    customPropertyHandle.SetValue(inputContainer.inputStr);
                    return;
                }
                if (TryParseInput(inputContainer.inputStr, valueSetter.ValueType, out value))
                {
                    valueSetter.SetValue(value);
                    return;
                }
                throw new CommandExecuteException($"cannot parse \"{inputContainer.inputStr}\" to type \"{valueSetter.ValueType}\" near \"{node.DebugInfo}\"");
            }
            else
            {
                throw new CommandExecuteException($"cannot assign <{stackValue.stackType}> to \"{stackValue.stackType}\" near \"{node.DebugInfo}\"");
            }
            SetProperty(valueSetter, value, node.DebugInfo);
        }

        /// <summary>
        /// execute index operation
        /// </summary>
        void ExecuteIndex(SyntaxTree node, ExecuteStatus status)
        {

            Execute(node.children[0]);
            var stackValue = TopValue;
            if (stackValue is not IValueGetter valueGetter)
            {
                throw new CommandExecuteException($"cannot get element from <{stackValue.stackType}> near \"{node.DebugInfo}\"");
            }
            object instance = valueGetter.GetValue()
                ?? throw new CommandExecuteException($"cannot get element from \"null\" near \"{node.DebugInfo}\"");

            var instanceType = instance.GetType();

            // execute index value
            Execute(node.children[1]);
            stackValue = TopValue;
            if (stackValue is StackInt intContainer)
            {

                object value = VirtualMachineUtils.GetElement(instance, intContainer.intValue);
                _stack.Push(StackValue.Wrap(value));
                return;
            }
            if (stackValue is StackBool boolContainer)
            {

                object value = VirtualMachineUtils.GetElement(instance, boolContainer.boolValue ? 1 : 0);
                _stack.Push(StackValue.Wrap(value));
                return;
            }
            if (stackValue is IValueGetter indexValueGetter)
            {
                object index = indexValueGetter.GetValue();
                object result = VirtualMachineUtils.GetElement(instance, index);
                _stack.Push(StackValue.Wrap(result));
                return;
            }
            throw new CommandExecuteException($"<{stackValue.stackType}> cannot be index of \"{instanceType}\" near \"{node.DebugInfo}\"");
        }

        void ExecuteSetElement(SyntaxTree node, ExecuteStatus status)
        {

            Execute(node.children[1]);
            var stackValue = TopValue;
            if (stackValue is not IValueGetter indexGetter)
            {
                throw new CommandExecuteException($"<{stackValue.stackType}> cannot be index near \"{node.DebugInfo}\"");
            }
            object indexValue = indexGetter.GetValue()
                ?? throw new CommandExecuteException($"null cannot be index \"{node.DebugInfo}\"");


            bool isArray = stackValue is StackInt || stackValue is StackBool;

            // get instance and try to get it's element type
            Execute(node.children[0]);
            stackValue = TopValue;
            if (stackValue is not IValueGetter instanceGetter)
            {
                throw new CommandExecuteException($"cannot set element of <{stackValue.stackType}> near \"{node.DebugInfo}\"");
            }
            object instance = instanceGetter.GetValue()
                ?? throw new CommandExecuteException($"cannot set element of null near \"{node.DebugInfo}\"");

            // get element type
            Type elementType = null;
            if (isArray) elementType = VirtualMachineUtils.GetElementTypeOfArray(instance);
            if (elementType == null)
            {
                isArray = false;
                elementType = VirtualMachineUtils.GetElementTypeOfDict(instance, indexValue.GetType()) ??
                throw new CommandExecuteException($"cannot set element of <{stackValue.stackType}> near \"{node.DebugInfo}\"");
            }

            // get value user want to set accordding to element type
            Execute(node.children[2], new ExecuteStatus { requiredType = elementType });
            stackValue = TopValue;
            if (stackValue is not IValueGetter valueGetter)
            {
                throw new CommandExecuteException($"cannot set <{stackValue.stackType}> as element of {instance.GetType()} near \"{node.DebugInfo}\"");
            }
            object value = valueGetter.GetValue();
            if (isArray)
            {

                if (stackValue is StackInt intContainer)
                {
                    VirtualMachineUtils.SetElement(instance, intContainer.intValue, value);
                    return;
                }
                if (stackValue is StackBool boolContainer)
                {
                    VirtualMachineUtils.SetElement(instance, boolContainer.boolValue ? 1 : 0, value);
                    return;
                }
                throw new CommandExecuteException($"cannot convert \"{indexValue}\" to int type near \"{node.DebugInfo}\"");
            }

            VirtualMachineUtils.SetElement(instance, indexValue, value);
        }

        /// <summary>
        /// convert value to given type
        /// </summary>
        void ExecuteConvert(SyntaxTree node, ExecuteStatus status)
        {

            Execute(node.children[0]);
            var stackValue = TopValue;
            string typeAlias = node.children[1].data;

            /* DOC> if no converter type given, then push value back */
            Type requiredType = TryGetTypeByAlias(typeAlias) ?? status.requiredType;
            if (requiredType == null)
            {
                _stack.Push(stackValue);
                return;
            }

            /* anything at value position would be convert to string type and try parse it 
            to destination indicated by type alias */
            if (stackValue is IValueGetter valueGetter)
            {

                object value = valueGetter.GetValue();
                if (value == null)
                {
                    if (requiredType.IsNullable())
                    {
                        _stack.Push(StackNull.Default);
                        return;
                    }
                    throw new CommandExecuteException($"cannot convert \"null\" to \"{status.requiredType}\" near \"{node.DebugInfo}\"");
                }

                /* no need to do any convert */
                if (requiredType.IsAssignableFrom(value.GetType()))
                {
                    _stack.Push(stackValue);
                    return;
                }

                /* check if target type is string type */
                string inputStr = value.ToString();
                if (inputStr == null)
                {
                    /* unknown how to handle an input with null ToString() just push it back */

                    _stack.Push(stackValue);
                    return;
                }

                if (requiredType == typeof(string))
                {
                    _stack.Push(new StackString(inputStr));
                    return;
                }

                /* try parse inputStr to target type */
                if (TryParseInput(inputStr, requiredType, out object resultValue))
                {
                    _stack.Push(StackValue.Wrap(resultValue));
                    return;
                }
            }

            /* normal handle way, given a string input and try convert it to some type you want */
            if (stackValue is StackSourceInput inputGetter)
            {

                if (requiredType == typeof(string))
                {
                    _stack.Push(new StackString(inputGetter.inputStr));
                    return;
                }

                if (TryParseInput(inputGetter.inputStr, requiredType, out object resultValue))
                {
                    _stack.Push(StackValue.Wrap(resultValue));
                    return;
                }
            }

            /* unknow how to convert, just put it back */
            _stack.Push(stackValue);
        }
    }


}