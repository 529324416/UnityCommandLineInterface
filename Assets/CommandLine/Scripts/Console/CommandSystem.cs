/*
MIT License - CommandSystem.cs
Created By : Prince Biscuit
Created Date : 2024/01/01

Description : CommandSystem.cs is a part of GameCLI package. 
              use for any game that need a command line interface.

version : 0.2.0
@2024.01.28 : Totally refactor the CommandSystem.cs, now it has more functions

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace RedSaw.CommandLineInterface{

    /// <summary> 
    /// command, usually represent by an static method 
    /// </summary>
    public class Command : StackMethod{

        public readonly string name;
        public readonly string description;
        public readonly string tag;

        public override string Name => name;

        /// <summary>static method command</summary>
        /// <param name="method">the method info, must be a static method</param>
        public Command(string name, string description, string tag, MethodInfo method) : base(null, method){

            this.name = name;
            this.description = description;
            this.tag = tag;
        }
        /// <summary>instance method command</summary>
        /// <param name="instance">the instance of method</param>
        /// <param name="method">the method info</param>
        public Command(string name, string description, string tag, object instance, MethodInfo method)
        :base(instance, method){
            this.name = name;
            this.description = description;
            this.tag = tag;
        }

        /// <summary>check if this command has given tag</summary>
        public bool CompareTag(string tag){
            
            if(tag == null || tag.Length == 0)return true;
            return this.tag == tag;
        }
        public override string ToString()
        {
            return $"{name} : {description}";
        }
    }


    /// <summary>command creator</summary>
    static class CommandCreator{

        /// <summary>collect all commands from given attribute type</summary>
        public static IEnumerable<Command> CollectCommands<T>() where T : CommandAttribute{

            foreach(var type in Assembly.GetExecutingAssembly().GetTypes()){
                foreach(var methodInfo in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)){
                    var attribute = methodInfo.GetCustomAttribute<T>();
                    if(attribute != null){
                        var command = new Command(attribute.Name ?? methodInfo.Name, attribute.Desc, attribute.Tag, methodInfo);
                        yield return command;
                    }
                }
            }
        }

        /// <summary>collect all properties from given attribute type</summary>
        public static IEnumerable<StackProperty> CollectProperties<T>() where T: CommandPropertyAttribute{

            foreach(var type in Assembly.GetExecutingAssembly().GetTypes()){
                foreach(var propertyInfo in type.GetProperties(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)){
                    var attribute = propertyInfo.GetCustomAttribute<T>();
                    if(attribute != null){
                        var ppt = propertyInfo.CreateStackProperty(
                            attribute.Name ?? propertyInfo.Name,
                            attribute.Desc,
                            attribute.Tag
                        );
                        if( ppt != null ) yield return ppt;

                    }
                }

                foreach(var fieldInfo in type.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)){
                    var attribute = fieldInfo.GetCustomAttribute<T>();
                    if(attribute != null){
                        var ppt = fieldInfo.CreateStackProperty(
                            attribute.Name ?? fieldInfo.Name,
                            attribute.Desc,
                            attribute.Tag
                        );
                        if( ppt != null ) yield return ppt;
                    }
                }
            }
        }

        /// <summary>collect all custom type parsers</summary>
        public static IEnumerable<(ValueParser, Type, string)> CollectValueParsers<T>() where T : CommandValueParserAttribute{

            foreach(var type in Assembly.GetExecutingAssembly().GetTypes()){
                foreach(var methodInfo in type.GetMethods(
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public
                )){
                    var attr = methodInfo.GetCustomAttribute<T>();
                    if( attr == null )continue;
                    if( TryConvertDelegate(methodInfo, out ValueParser parser) ){
                        yield return (parser, attr.type, attr.Alias);
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

    }

    /// <summary>query buffer</summary>
    class QueryCache<T> where T: struct{

        readonly Dictionary<string, T[]> buffer = new();
        readonly List<string> queryHistory = new();
        readonly int capacity;
        public QueryCache(int capacity){
            this.capacity = Math.Max(1, capacity);
        }

        /// <summary>store a query result</summary>
        /// <param name="query">query string, this must not be a empty or null value</param>
        /// <param name="result">query result, must not be a null value</param>
        public void Cache(string query, T[] result){

            if(buffer.ContainsKey(query))return;
            if( queryHistory.Count + 1 > capacity ){
                buffer.Remove(queryHistory[0]);
                queryHistory.RemoveAt(0);
            }
            buffer.Add(query, result);
            queryHistory.Add(query);
        }
        /// <summary>
        /// check if some query has been recorded
        /// </summary>
        /// <param name="query">query string, and you should check if it is 
        /// null or empty before you call this function</param>
        public bool GetCache(string query, out T[] result){
            
            return buffer.TryGetValue(query, out result);
        }
    }



    /// <summary>
    /// command system, provide interface to control VirtualMachine
    /// </summary>
    public partial class CommandSystem{

        /// <param name="logCapacity">
        /// the capacity of logs that shows on console
        /// </param>
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
        public CommandSystem(

            float scoreThresholdCommand = 0.3f,
            float scoreThresholdVariable = 0.1f,
            float scoreThresholdType = 0.1f,
            int typeReflectionQueryCache = 20,
            int commandQueryCacheCapacity = 20,
            int variableQueryCacheCapacity = 20,
            bool IgnoreInvalidSetBehaviour = true,
            bool ReceiveValueFromNonStringType = true,
            bool TreatVoidAsNull = true
        ){

            /* initialize virtual machine */
            lexer = new Lexer();
            syntaxAnalyzer = new SyntaxAnalyzer();
            vm = new VirtualMachine(
                IgnoreInvalidSetBehaviour,
                ReceiveValueFromNonStringType,
                TreatVoidAsNull
            );
            charAutomaton = new(vm.GetPropertyType, vm.GetCallableType);

            this.scoreThresholdCommand = Math.Clamp(scoreThresholdCommand, 0, 1);
            this.scoreThresholdVariable = Math.Clamp(scoreThresholdVariable, 0, 1);
            this.scoreThresholdType = Math.Clamp(scoreThresholdType, 0, 1);
            QC_command = new(commandQueryCacheCapacity);
            QC_variable = new(variableQueryCacheCapacity);
            QC_type = new(typeReflectionQueryCache);

            /* load custom commands */
            foreach(var command in CommandCreator.CollectCommands<CommandAttribute>()){
                vm.RegisterCallable(command);
            }

            /* load custom properties */
            foreach(var property in CommandCreator.CollectProperties<CommandPropertyAttribute>()){
                vm.RegisterProperty(property);
            }

            /* load value parsers */
            foreach(var pack in CommandCreator.CollectValueParsers<CommandValueParserAttribute>()){
                vm.RegisterValueParser(pack.Item2, pack.Item1, pack.Item3);
            }
            
        }
    }

    #region About Virtual Machine
    public partial class CommandSystem{

        readonly Lexer lexer;
        readonly SyntaxAnalyzer syntaxAnalyzer;
        readonly float scoreThresholdCommand;
        readonly float scoreThresholdVariable;
        readonly float scoreThresholdType;
        readonly QueryCache<Suggestion> QC_command;
        readonly QueryCache<Suggestion> QC_variable;
        readonly QueryCache<Suggestion> QC_type;

        /// <summary>a simple virtual machine to execute user input</summary>
        readonly VirtualMachine vm;

        readonly CharAutomaton charAutomaton;
        string lastQueryStr = string.Empty;

        /// <summary>register a new console command whatever static method or instance method</summary>
        public void RegisterCommand(Command command) => vm.RegisterCallable(command);

        /// <summary>register a new console property</summary>
        public void RegisterProperty(StackProperty property) => vm.RegisterProperty(property);

        /// <summary>register a new console value parser</summary>
        public void RegisterValueParser(Type type, ValueParser parser, string alias = null) => vm.RegisterValueParser(type, parser, alias);

        /// <summary>register a new console value parser</summary>
        public void RegisterValueParser<TType>(ValueParser parser, string alias = null) => vm.RegisterValueParser(typeof(TType), parser, alias);

        /// <summary>get variable created at runtime</summary>
        public object GetLocalVariable(string name) => vm.GetLocalVariable(name);

        /// <summary>set variable created at runtime</summary>
        public void SetLocalVariable(string name, object value) => vm.SetLocalVariable(name, value);

        /// <summary>
        /// get current suggestions
        /// </summary>
        public Suggestion[] GetCurrentSuggestions(string currentText, int count, Func<string, string, float> scoreFunc){

            var queryStatus = charAutomaton.Input(currentText);
            lastQueryStr = queryStatus.queryStr;
            return queryStatus.suggestionType switch
            {
                SuggestionType.Variable => QueryVariable(queryStatus.queryStr, count, scoreFunc),
                SuggestionType.Command => QueryCommands(queryStatus.queryStr, count, scoreFunc),
                SuggestionType.Member => QueryType(queryStatus.queryStr, queryStatus.queryType, count, scoreFunc),
                _ => Array.Empty<Suggestion>(),
            };
        }

        public string TakeSuggestion(string currentInput, string primary){
            if( lastQueryStr == string.Empty )return currentInput + primary;
            return currentInput[..^lastQueryStr.Length] + primary;
        }


        /// <summary>
        /// execute given command
        /// </summary>
        /// <param name="commandInput">
        /// command input, and it must not be empty or null
        /// </param>
        /// <returns>if success return null else return the Exception</returns>
        public Exception Execute(string commandInput, out object executeResult){

            try{
                
                var lexerResult = lexer.Parse(commandInput);
                var syntaxTree = syntaxAnalyzer.Analyze(lexerResult);
                executeResult = vm.ExecuteRoot(syntaxTree);
                return null;

            }catch( CommandSystemException ex ){

                executeResult = null;
                return ex;

            }catch( Exception ex ){
                
                executeResult = null;
                return ex.InnerException ?? ex;
            }
        }

        /// <summary>query a group of commands with top score of all commands</summary>
        /// <param name="query">query string</param>
        /// <param name="count">the count of commands you want to query</param>
        /// <param name="scoreFunc">score function</param>
        /// <returns>return a group of commands</returns>
        public Suggestion[] QueryCommands(string query, int count, Func<string, string, float> scoreFunc, string tag = null){

            query ??= string.Empty;

            /* check if this query has been stored */ 
            var queryDetailInfo = query + ":" + tag ?? string.Empty;
            if(QC_command.GetCache(queryDetailInfo, out var result))return result;

            /* start to query */
            var bestChoices = vm.AllCallables
            .Where( s => s is Command command && command.CompareTag(tag) )
            .Select(s => new {value = s, score = scoreFunc(query, s.Name)})
            .Where(s => s.score > scoreThresholdCommand)
            .OrderByDescending(s => s.score)
            .Take(Math.Max(1, count));

            result = bestChoices.Select( (s) => {
                var c = (Command)s.value;
                return new Suggestion(c.Name, c.description);
            }).ToArray();
            QC_command.Cache(queryDetailInfo, result);
            return result;
        }

        public Suggestion[] QueryVariable(string query, int count, Func<string, string, float> scoreFunc, string tag = null){

            query ??= string.Empty;

            var queryDetailInfo = query + ":" + tag ?? string.Empty;
            if(QC_variable.GetCache(queryDetailInfo, out var result))return result;

            var bestChoices = vm.AllProperties
            .Where( s => s.CompareTag(tag) )
            .Select( s => new { value = s, score = scoreFunc(query, s.Name )})
            .Where(s => s.score > scoreThresholdVariable)
            .OrderByDescending( s => s.score )
            .Take(Math.Max(1, count));
            
            result = bestChoices.Select( s => new Suggestion(s.value.Name, s.value.description)).ToArray();
            QC_variable.Cache(queryDetailInfo, result);
            return result;
        }

        public Suggestion[] QueryType(string query, Type type, int count, Func<string, string, float> scoreFunc){

            query ??= string.Empty;

            var queryDetailInfo = $"{type.Name}.{query}";
            if(QC_type.GetCache(queryDetailInfo, out var result))return result;
            var bestChoices = type.GetMembers()
            .Select( s => new { value = s, score = scoreFunc(query, s.Name) })
            .Where(s => s.score > scoreThresholdType)
            .OrderByDescending( s => s.score )
            .Take(Math.Max(1, count));

            result = bestChoices.Select( s => new Suggestion(s.value.Name, s.value.GetMemberTypeName())).ToArray();
            QC_type.Cache(queryDetailInfo, result);
            return result;
        }
    }
    #endregion



    // /// <summary>command system</summary>
    // public static class CommandSystem{

        // static readonly VirtualMachine vm;
        // static readonly Dictionary<string, Command> commands = new();
        // static readonly QueryBuffer queryBuffer = new(20);

        // static readonly CommandParser parser = new();
        // static Action<string> output;
        // static Action<string> outputErr;
        // static void Output(string message) => output?.Invoke(message);
        // static void OutputErr(string message) => outputErr?.Invoke(message);

        // /// <summary>get all commands' informations</summary>
        // public static IEnumerable<Command> TotalCommands => commands.Values;

        // static CommandSystem(){

        //     foreach(var command in CommandCreator.CollectCommands<CommandAttribute>()){
        //         if(commands.ContainsKey(command.name)){
        //             commands[command.name] = command;
        //             continue;
        //         }
        //         commands.Add(command.name, command);
        //     }
        // }

        // public static void CollectDefaultCommand(){

        //     // foreach(var command in CommandCreator.CollectCommands<DefaultCommandAttribute>()){
        //     //     if(commands.ContainsKey(command.name))continue;
        //     //     commands.Add(command.name, command);
        //     // }
        // }

        // /// <summary>execute given command</summary>
        // /// <param name="input">the command you want to execute</param>
        // /// <returns>if success</returns>
        // public static bool Execute(string input){

        //     if(!parser.Parse(input, out string[] result)){
        //         OutputErr($"invalid command: {input} ");
        //         return false;
        //     }
        //     string commandName = result[0];
        //     if(!commands.TryGetValue(commandName, out Command command)){
        //         OutputErr($"unknown command name: {commandName}");
        //         return false;
        //     }
        //     Exception e = null;
        //     if(result.Length == 1){
        //         // e = command.Execute(new string[]{});
        //     }else{
        //         string[] args = new string[result.Length - 1];
        //         for(int i = 1; i < result.Length; i ++){
        //             args[i - 1] = result[i];
        //         }
        //         // e = command.Execute(args);
        //     }
        //     if(e != null){
        //         OutputErr($"command error : {e.Message}");
        //         OutputErr(e.StackTrace);
        //         return false;
        //     }
        //     return true;
        // }

        // /// <summary>execute given command</summary>
        // /// <param name="input">the command you want to execute</param>
        // /// <returns>if success</returns>
        // public static bool ExecuteSlience(string input){

        //     if(!parser.Parse(input, out string[] result))return false;
        //     if(!commands.TryGetValue(result[0], out Command command))return false;
        //     Exception e = null;
        //     if(result.Length == 1){
        //         // e = command.Execute(new string[]{});
        //     }else{
        //         string[] args = new string[result.Length - 1];
        //         for(int i = 1; i < result.Length; i ++){
        //             args[i - 1] = result[i];
        //         }
        //         // e = command.Execute(args);
        //     }
        //     return e == null;
        // }


        // /// <summary>you can set a output function of CommandSystem
        // /// to receive normal message </summary>
        // public static void SetOutputFunc(Action<string> func) => output = func;

        // /// <summary>you can set a output function of CommandSystem
        // /// to receive error message </summary>
        // public static void SetOutputErrFunc(Action<string> func) => outputErr = func;

        // /// <summary>query a group of commands with top score of all commands</summary>
        // /// <param name="query">query string</param>
        // /// <param name="count">the count of commands you want to query</param>
        // /// <param name="scoreFunc">score function</param>
        // /// <returns>return a group of commands</returns>
        // public static Command[] QueryCommands(string query, int count, Func<string, string, float> scoreFunc, string tag = null){

        //     /* ensure that query must be a valid string */
        //     if(query == null || query.Length == 0)return new Command[0];

        //     /* check if this query has been stored */ 
        //     var queryDetailInfo = query + ":" + tag ?? string.Empty;
        //     if(queryBuffer.GetCache(queryDetailInfo, out var result))return result;

        //     /* start to query */
        //     count = Math.Max(1, count);
        //     var bestChoices = commands
        //     .Where(s => s.Value.CompareTag(tag))
        //     .Select(s => new {value = s, score = scoreFunc(query, s.Key)})
        //     .Where(s => s.score > 0)
        //     .OrderByDescending(s => s.score)
        //     .Take(count)
        //     .ToArray();

        //     result = bestChoices.Select(s => s.value.Value).ToArray();
        //     queryBuffer.Cache(queryDetailInfo, result);
        //     return result;
        // }
    // }
}