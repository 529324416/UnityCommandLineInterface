/*
MIT License - CommandSystem.cs
Created By : Prince Biscuit
Created Date : 2024/01/01

Description : CommandSystem.cs is a part of GameCLI package. 
              use for any game that need a command line interface.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RedSaw.CommandLineInterface{

    /// <summary>show command information</summary>
    public readonly struct CommandInfo{

        /// <summary>Command Name</summary>
        public readonly string Name;

        /// <summary>Command Description</summary>
        public readonly string Description;

        /// <summary>the parameter count of command</summary>
        public readonly int ParameterCount;
        
        public CommandInfo(string Name, string Description, int ParameterCount){

            this.Name = Name;
            this.Description = Description;
            this.ParameterCount = ParameterCount;
        }
        public override string ToString()
        {
            return $"{Name}: {Description}";
        }
    }

    /// <summary>command structure</summary>
    readonly struct Command{

        public readonly string name;
        public readonly string description;
        public readonly MethodInfo method;
        public readonly ParameterInfo[] parameters;

        public readonly int ParameterCount => parameters.Length;
        public readonly CommandInfo CommandInfo => new(name, description, ParameterCount);

        /// <summary>command constructor</summary>
        public Command(string name, string description, MethodInfo method){

            this.name = name;
            this.description = description;
            this.method = method;
            parameters = method.GetParameters();
        }

        /// <summary>execute this command with given string array</summary>
        public Exception Execute(string[] args)
        {
            try{

                /* parse all parameters from args and load them into an object array */
                object[] loadedParams = new object[parameters.Length];
                for(int i = 0; i < loadedParams.Length; i ++){

                    /* check if has default parameter value */
                    if(i >= args.Length){
                        if(parameters[i].HasDefaultValue){
                            loadedParams[i] = parameters[i].DefaultValue;
                            continue;
                        }
                        throw new Exception($"parameter {parameters[i].Name} is missing");
                    }

                    /* parse parameter from string */
                    if(CommandParameterHandle.ParseParameter(args[i], parameters[i].ParameterType, out loadedParams[i]))continue;
                    throw new Exception($"parameter <{parameters[i].Name}:{args[i]}> is invalid");
                }
                /* call the method */
                method.Invoke(null, loadedParams);
                return null;

            }catch(TargetInvocationException ex){

                return ex.InnerException ?? ex;
            }catch(Exception ex){

                return ex.InnerException ?? ex;
            }
        }

        public override string ToString()
        {
            return $"{name} : {description}";
        }
    }

    /// <summary>parameter parser</summary>
    /// <param name="args">parameter string</param>
    /// <param name="value">parameter value</param>
    /// <returns>return true if success</returns>
    delegate bool ParameterParser(string args, out object value);

    /// <summary>handle parameters of command method</summary>
    static class CommandParameterHandle{

        static readonly Dictionary<Type, ParameterParser> DefaultParseFunctions;
        static readonly Dictionary<Type, ParameterParser> CustomParseFunctions;

        static CommandParameterHandle(){
                
            DefaultParseFunctions = new Dictionary<Type, ParameterParser>();
            CustomParseFunctions = new Dictionary<Type, ParameterParser>();

            /* register default parameter parsers */
            DefaultParseFunctions.Add(typeof(int), ParseInt);
            DefaultParseFunctions.Add(typeof(string), ParseString);
            DefaultParseFunctions.Add(typeof(float), ParseFloat);
            DefaultParseFunctions.Add(typeof(double), ParseDouble);
            DefaultParseFunctions.Add(typeof(bool), ParseBool);
            DefaultParseFunctions.Add(typeof(char), ParseChar);
            DefaultParseFunctions.Add(typeof(byte), ParseByte);
            DefaultParseFunctions.Add(typeof(short), ParseShort);
            DefaultParseFunctions.Add(typeof(long), ParseLong);
            DefaultParseFunctions.Add(typeof(ushort), ParseUShort);
            DefaultParseFunctions.Add(typeof(uint), ParseUInt);
            DefaultParseFunctions.Add(typeof(ulong), ParseULong);
            DefaultParseFunctions.Add(typeof(decimal), ParseDecimal);
            DefaultParseFunctions.Add(typeof(sbyte), ParseSByte);
        }

        static bool ParseInt(string args, out object value)
        {
            if(int.TryParse(args, out int result)){
                value = result;
                return true;
            }
            value = null;
            return false;
        }
        static bool ParseString(string args, out object value)
        {
            value = args ?? string.Empty;
            return true;
        }
        static bool ParseFloat(string args, out object value)
        {
            if(float.TryParse(args, out float result)){
                value = result;
                return true;
            }
            value = null;
            return false;
        }
        static bool ParseDouble(string args, out object value)
        {
            if(double.TryParse(args, out double result)){
                value = result;
                return true;
            }
            value = null;
            return false;
        }
        static bool ParseBool(string args, out object value)
        {
            if(bool.TryParse(args, out bool result)){
                value = result;
                return true;
            }
            value = null;
            return false;
        }
        static bool ParseChar(string args, out object value)
        {
            if(char.TryParse(args, out char result)){
                value = result;
                return true;
            }
            value = null;
            return false;
        }
        static bool ParseByte(string args, out object value)
        {
            if(byte.TryParse(args, out byte result)){
                value = result;
                return true;
            }
            value = null;
            return false;
        }
        static bool ParseShort(string args, out object value)
        {
            if(short.TryParse(args, out short result)){
                value = result;
                return true;
            }
            value = null;
            return false;
        }
        static bool ParseLong(string args, out object value)
        {
            if(long.TryParse(args, out long result)){
                value = result;
                return true;
            }
            value = null;
            return false;
        }
        static bool ParseUShort(string args, out object value)
        {
            if(ushort.TryParse(args, out ushort result)){
                value = result;
                return true;
            }
            value = null;
            return false;
        }
        static bool ParseUInt(string args, out object value)
        {
            if(uint.TryParse(args, out uint result)){
                value = result;
                return true;
            }
            value = null;
            return false;
        }
        static bool ParseULong(string args, out object value)
        {
            if(ulong.TryParse(args, out ulong result)){
                value = result;
                return true;
            }
            value = null;
            return false;
        }
        static bool ParseDecimal(string args, out object value)
        {
            if(decimal.TryParse(args, out decimal result)){
                value = result;
                return true;
            }
            value = null;
            return false;
        }
        static bool ParseSByte(string args, out object value)
        {
            if(sbyte.TryParse(args, out sbyte result)){
                value = result;
                return true;
            }
            value = null;
            return false;
        }

        /// <summary>register a parameter parser, if parser already has been registered
        /// then old parser would be replaced by the new one</summary>
        /// <param name="type">parameter type</param>
        /// <param name="parser">parameter parser</param>
        /// <returns>return true if success</returns>
        public static bool RegisterParser(Type type, ParameterParser parser)
        {
            if(type == null || parser == null)return false;
            if(DefaultParseFunctions.ContainsKey(type))return false;
            if(CustomParseFunctions.ContainsKey(type)){
                CustomParseFunctions[type] = parser;
            }else{
                CustomParseFunctions.Add(type, parser);
            }
            return true;
        }

        /// <summary>check if a type has been registered</summary>
        /// <param name="type">the type you want to query</param>
        /// <returns>return true if success</returns>
        public static bool ContainsParser(Type type){

            if(type == null)return false;
            return DefaultParseFunctions.ContainsKey(type) || CustomParseFunctions.ContainsKey(type);
        }

        /// <summary>parse parameter from string</summary>
        /// <param name="args">the input string</param>
        /// <param name="type">parameter type defined in method</param>
        /// <param name="value">the parse result</param>
        /// <returns>if success parsed</returns>
        public static bool ParseParameter(string args, Type type, out object value)
        {
            value = null;
            if(DefaultParseFunctions.TryGetValue(type, out var parser)){
                return parser(args, out value);
            }
            if(!CustomParseFunctions.TryGetValue(type, out parser))return false;
            try{
                return parser(args, out value);
            }catch(Exception){
                return false;
            }
        }
    }

    /// <summary>command creator</summary>
    static class CommandCreator{

        /// <summary>collect all commands from execution position</summary>
        /// <returns>return all commands</returns>
        public static IEnumerable<Command> CollectCommands(){

            Type[] totalTypes = Assembly.GetExecutingAssembly().GetTypes();
            RegisterParameterParsers(totalTypes);
            foreach(Type type in totalTypes){
                
                MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if(methods.Length == 0)continue;
                foreach(MethodInfo method in methods){
                    var attr = method.GetCustomAttribute<CommandAttribute>();
                    if(attr == null)continue;
                    if(CreateCommand(method, attr, out Command command)){
                        yield return command;
                    }
                }
            }
        }

        /// <summary>get all parameter parser function from given types</summary>
        static void RegisterParameterParsers(Type[] totalTypes){

            foreach(Type type in totalTypes){
                MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if(methods.Length == 0)continue;
                foreach(var method in methods){
                    var attr = method.GetCustomAttribute<CommandParameterParserAttribute>();
                    if(attr == null || attr.type == null)continue;
                    if(TryConvertDelegate<ParameterParser>(method, out var pp)){
                        CommandParameterHandle.RegisterParser(attr.type, pp);
                    }
                }
            }
        }

        /// <summary>check if target methodInfo have same signature with given delegate type</summary>
        static bool TryConvertDelegate<T>(MethodInfo methodInfo, out T result) where T: Delegate
        {
            // Check if the return types match
            result = null;
            MethodInfo delegateMethodInfo = typeof(T).GetMethod("Invoke");
            if (methodInfo.ReturnType != delegateMethodInfo.ReturnType) return false;

            // Check if the parameter types match
            var methodParams = methodInfo.GetParameters();
            var delegateParams = delegateMethodInfo.GetParameters();

            if (methodParams.Length != delegateParams.Length) return false;

            for (int i = 0; i < methodParams.Length; i++){
                if (methodParams[i].ParameterType != delegateParams[i].ParameterType)
                    return false;
            }
            
            result = Delegate.CreateDelegate(typeof(T), null, methodInfo) as T;
            return true;
        }

        /// <summary>create a command from a method</summary>
        /// <param name="methodInfo">method info defined <b>CommandAttribute</b></param>
        /// <param name="attr">the target command attribute</param>
        /// <param name="command">result command</param>
        /// <returns>return true if success</returns>
        static bool CreateCommand(MethodInfo methodInfo, CommandAttribute attr, out Command command){

            /* check if target type has been supported by invoker */
            ParameterInfo[] parameters = methodInfo.GetParameters();
            if(parameters.Length > 0){
                foreach(ParameterInfo parameter in parameters){
                    if(CommandParameterHandle.ContainsParser(parameter.ParameterType))continue;
                    command = default;
                    return false;
                }
            }

            /* command name should be the one defined in attribute, if client not set a name
               then would use method name as default */
            string commandName = attr.Name ?? methodInfo.Name;
            command = new Command(commandName, attr.Desc, methodInfo);
            return true;
        }
    }

    /// <summary>command parser</summary>
    class CommandParser{

        const char EOL = '\0';
        const char SPACE = ' ';
        const char DBL_QUOTATION = '"';
        const char SGL_QUOTATION = '\'';

        int index;
        string input;
        bool HasMore => index < input.Length;

        /// <summary>parse command with given input string</summary>
        public bool Parse(string input, out string[] result){

            result = null;
            if(input == null || input.Length == 0)return false;

            /* pad EOL in the end for we known where to leave state machine */
            this.input = input.Trim() + EOL;
            index = 0;
            try{
                var list = new List<string>();
                foreach(var part in Walk()){
                    list.Add(part);
                }
                result = list.ToArray();
                return list.Count > 0;
            }catch(Exception){

                result = null;
                return false;
            }
        }
        IEnumerable<string> Walk(){

            while(HasMore){
                char c = input[index ++];
                System.Console.WriteLine(c);
                switch(c){
                    case DBL_QUOTATION:
                        yield return NextString(index, DBL_QUOTATION);
                        if(HasMore && input[index] == SPACE)index ++;
                        break;

                    case SGL_QUOTATION:
                        yield return NextString(index, SGL_QUOTATION);
                        if(HasMore && input[index] == SPACE)index ++;
                        break;

                    case EOL:
                        break;

                    default:
                        yield return NextId(index - 1);
                        break;
                }
            }
        }
        string NextId(int start, int length = 0){

            while(HasMore){
                char c = input[index ++];
                if(char.IsWhiteSpace(c)){
                    return input.Substring(start, length + 1);
                }
                if(c == DBL_QUOTATION || c == SGL_QUOTATION){
                    index --;
                    return input.Substring(start, length + 1);
                }
                length ++;
            }
            return input.Substring(start, length);
        }
        string NextString(int start, char quotation, int length = 0){

            while(HasMore){
                char c = input[index ++];
                if(c == quotation)return input.Substring(start, length);
                length ++;
            }
            throw new Exception("string parser error");
        }
    }

    /// <summary>command system</summary>
    public static class CommandSystem{

        static readonly Dictionary<string, Command> commands = new();
        static readonly CommandParser parser = new();
        static Action<string> log = null;

        /// <summary>get all commands' informations</summary>
        public static IEnumerable<CommandInfo> TotalCommands{
            get{
                foreach(var command in commands.Values){
                    yield return command.CommandInfo;
                }
            }
        }

        static CommandSystem(){

            foreach(var command in CommandCreator.CollectCommands()){
                if(commands.ContainsKey(command.name)){
                    commands[command.name] = command;
                    continue;
                }
                commands.Add(command.name, command);
            }
        }

        static void Log(string message) => log?.Invoke(message);

        /// <summary>execute given command</summary>
        /// <param name="input">the command you want to execute</param>
        /// <returns>if success</returns>
        public static bool Execute(string input){

            if(!parser.Parse(input, out string[] result)){
                Log($"invalid command: {input} ");
                return false;
            }
            string commandName = result[0];
            if(!commands.TryGetValue(commandName, out Command command)){
                Log($"unknown command name: {commandName}");
                return false;
            }
            Exception e;
            if(result.Length == 1){
                e = command.Execute(new string[]{});
            }else{
                string[] args = new string[result.Length - 1];
                for(int i = 1; i < result.Length; i ++){
                    args[i - 1] = result[i];
                }
                e = command.Execute(args);
            }
            if(e != null){
                Log($"command error : {e.Message}");
                Log(e.StackTrace);
                return false;
            }
            return true;
        }

        /// <summary>you can set a output function of CommandSystem
        /// to receive error message </summary>
        public static void SetOutputFunc(Action<string> func) => log = func;

        /// <summary>query a group of commands with top score of all commands</summary>
        /// <param name="query">query string</param>
        /// <param name="count">the count of commands you want to query</param>
        /// <param name="scoreFunc">score function</param>
        /// <returns>return a group of commands</returns>
        public static CommandInfo[] QueryCommands(string query, int count, Func<string, string, int> scoreFunc){

            count = Mathf.Max(1, count);
            var bestChoices = commands.Select(s => new {value = s, score = scoreFunc(query, s.Key)}).Where(s => s.score > 0).OrderByDescending(s => s.score).Take(count).ToArray();
            return bestChoices.Select(s => s.value.Value.CommandInfo).ToArray();
        }
    }
}