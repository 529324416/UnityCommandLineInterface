using System;
using System.Collections.Generic;

namespace RedSaw.CommandLineInterface{



    /// <summary>use to record command input history</summary>
    class InputHistory{

        readonly List<string> history;
        readonly int capacity;
        int lastIndex;

        public InputHistory(int capacity){

            this.lastIndex = 0;
            this.capacity = capacity;
            this.history = new List<string>();
        }
        /// <summary>record input string</summary>
        public void Record(string input){
            
            if(history.Count == capacity){
                history.RemoveAt(0);
            }
            history.Add(input);
            lastIndex = history.Count - 1;
        }

        /// <summary>get last input string</summary>
        public string Last{
            get{
                if(history.Count == 0)return string.Empty;
                if(lastIndex < 0 || lastIndex >= history.Count)lastIndex = history.Count - 1;
                return history[lastIndex --];
            }
        }

        /// <summary>get next input string</summary>
        public string Next{
            get{
                if(history.Count == 0)return string.Empty;
                if(lastIndex >= history.Count || lastIndex < 0)lastIndex = 0;
                return history[lastIndex ++];
            }
        }
    }


    /// <summary>command selector</summary>
    class LinearSelector{

        public event Action<int> OnSelectionChanged;
        readonly List<string> optionsBuffer;
        int currentIndex;

        int TotalCount{
            get{
                if(optionsBuffer == null)return 0;
                return optionsBuffer.Count;
            }
        }

        /// <summary>
        /// the current selection of the alternative options
        /// if value is -1, it means there's no alternative options choosed
        /// </summary>
        public int SelectionIndex => currentIndex;

        public LinearSelector(){

            this.optionsBuffer = new();
            this.currentIndex = -1;
        }

        public void LoadOptions(List<string> options){

            this.optionsBuffer.Clear();
            this.optionsBuffer.AddRange(options);
            this.currentIndex = -1;
        }

        public bool GetCurrentSelection(out string selection){

            selection = string.Empty;
            if(currentIndex == -1)return false;
            selection = optionsBuffer[currentIndex];
            return true;
        }

        /// <summary>move to next alternative option</summary>
        public void MoveNext(){

            if(TotalCount == 0)return;
            if(currentIndex == -1){
                currentIndex = 0;
                OnSelectionChanged?.Invoke(currentIndex);
                return;
            }
            currentIndex = currentIndex < TotalCount - 1 ? currentIndex + 1 : 0;
            OnSelectionChanged?.Invoke(currentIndex);
        }

        /// <summary>move to last alternative option</summary>
        public void MoveLast(){

            if(TotalCount == 0)return;
            if(currentIndex == -1){
                currentIndex = TotalCount - 1;
                OnSelectionChanged?.Invoke(currentIndex);
                return;
            }
            currentIndex = currentIndex > 0 ? currentIndex - 1 : TotalCount - 1;
            OnSelectionChanged?.Invoke(currentIndex);
        }
    }


    public class LogManager<T> where T : Enum{
        /* about output logs */

        /// <summary>console log</summary>
        public readonly struct Log{

            public static Log Empty => new(string.Empty, default);

            public readonly string message;
            public readonly string color;
            public readonly T logType;

            public readonly bool HasColor => 
                color != null && color.Length > 0;

            public Log(string message, string color, T logType = default){

                this.message = message;
                this.color = color;
                this.logType = logType;
            }

            public Log(string message, T logType = default){

                this.message = message;
                this.color = null;
                this.logType = logType;
            }
            public override string ToString(){
                return color != null ? $"<color={color}>{message}</color>" : message;
            }
        }

        /// <summary>
        /// this event would triggered while receive messages
        /// </summary>
        public event Action<Log> OnReceivedMessage;
        readonly int logCapacity;
        readonly bool hasLogCapacity; 

        readonly List<Log> logs = new();

        /// <summary>
        /// initialize log manager
        /// </summary>
        /// <param name="logCapacity">the capacity of logs, -1 as infinite</param>
        public LogManager(int logCapacity = -1){

            this.logCapacity = logCapacity;
            this.hasLogCapacity = logCapacity > 0;
        }

        /// <summary>
        /// output a debug log on console, if info is null, then the tag would work on it
        /// </summary>
        public void Output(string info, T type = default){

            if( info == null || info.Length == 0 ){
                Output(Log.Empty);
                return;
            }
            Output(new(info, type));
        }
        /// <summary>
        /// output a debug log on console, if info is null, then the tag would work on it,
        /// the color should in format of '#ffffff';
        /// </summary>
        public void Output(string info, string color, T type = default){

            if( color == null || color.Length == 0 ){
                Output(info, type);
                return;
            }
            if( info == null || info.Length == 0 ){
                Output(Log.Empty);
                return;
            }
            Output(new(info, color, type));
        }

        void Output(Log log){
            /* trigger event */

            logs.Add(log);
            if( hasLogCapacity && logs.Count >= logCapacity ){
                logs.RemoveAt(0);
            }
            OnReceivedMessage?.Invoke(log);
        }

        /// <summary>
        /// get total logs of current console
        /// </summary>
        public IEnumerable<Log> AllLogs => logs;

        /// <summary>
        /// get last logs
        /// </summary>
        public IEnumerable<Log> GetLastLogs( int count ){

            if( count == 0 )yield break;
            count = Math.Min(count, logs.Count);
            int L = logs.Count - count;
            int R = L + count;
            for( int i = L; i < R; i ++ ){
                yield return logs[i];
            }
        }

        /// <summary>
        /// get last logs with target tag
        /// </summary>
        public IEnumerable<Log> GetLastLogs( int count, T type = default ){

            if( count == 0 )yield break;
            foreach(var log in logs){
                if( log.logType.Equals(type) ){
                    yield return log;
                }
                if( -- count <= 0 )break;
            }
        }
    }

















    /// <summary>command console</summary>
    public class ConsoleController<TLog> where TLog : Enum{

        readonly IConsoleRenderer renderer;
        readonly IConsoleInput userInput;
        readonly CommandSystem commandSystem;
        readonly LogManager<TLog> logManager;

        public event Action OnFocusOut;
        public event Action OnFocus;

        readonly int alternativeCommandCount;
        readonly bool shouldRecordFailedCommand;
        readonly bool outputWithTime;
        readonly bool outputStackTraceOfCommandExecution;

        readonly InputHistory inputHistory;
        readonly LinearSelector selector;
        bool ignoreTextChanged;

        /// <summary>initialize console</summary>
        /// <param name="renderer">the renderer of console</param>
        /// <param name="userInput">the input of console</param>

        #region About Command System
        /// <param name="commandQueryCacheCapacity">the capacity of command query cache, 20 as default</param>
        /// <param name="outputStackTraceOfCommandExecution">
        /// should output stack trace of command excution, it maybe too long.. 
        /// <para>only use for debug</para>
        #endregion

        /// <param name="inputHistoryCapacity">the capacity of input history</param>
        /// <param name="alternativeCommandCount">the count of alternative command options</param>
        /// <param name="shouldRecordFailedCommand">should record failed command input</param>
        /// <param name="outputPanelCapacity">the capacity of output panel</param>
        /// <param name="outputWithTime">should output with time information of [HH:mm:ss]</param>

        /// </param>
        public ConsoleController(
            IConsoleRenderer renderer, 
            IConsoleInput userInput,

            int logCapacity = -1,
            int inputHistoryCapacity = 20,
            int commandQueryCacheCapacity = 20,
            int alternativeCommandCount = 8,
            bool shouldRecordFailedCommand = true,
            bool outputWithTime = true,
            bool outputStackTraceOfCommandExecution = true
        ){
            // about renderer
            this.renderer = renderer;
            renderer.BindOnSubmit(OnSubmit);
            renderer.BindOnTextChanged(OnTextChanged);

            // about user input
            this.userInput = userInput;

            // about command system
            commandSystem = new CommandSystem(
                commandQueryCacheCapacity:commandQueryCacheCapacity
            );

            logManager = new LogManager<TLog>(logCapacity);
            logManager.OnReceivedMessage += log => {

                if(log.HasColor){
                    renderer.Output(log.message, log.color);
                    return;
                }
                renderer.Output(log.message);
            };

            // other things - input history
            inputHistory = new InputHistory(Math.Max(inputHistoryCapacity, 2));

            // other things - alternative options
            selector = new LinearSelector();
            this.selector.OnSelectionChanged += idx => {
                renderer.AlternativeOptionsIndex = idx;
                renderer.MoveCursorToEnd();
            };
            
            this.alternativeCommandCount = Math.Max(alternativeCommandCount, 1);
            this.shouldRecordFailedCommand = shouldRecordFailedCommand;
            this.outputWithTime = outputWithTime;
            this.outputStackTraceOfCommandExecution = outputStackTraceOfCommandExecution;
        }
        public void Update(){

            if(userInput.ShowOrHide){
                renderer.IsVisible = !renderer.IsVisible;
            }
            if(!renderer.IsVisible)return;

            if(renderer.IsInputFieldFocus){

                // quit focus
                if(userInput.QuitFocus){

                    renderer.QuitFocus();
                    OnFocusOut?.Invoke();
                }
                else if(userInput.MoveDown){

                    if(renderer.IsAlternativeOptionsActive){
                        selector.MoveNext();
                    }else{
                        ignoreTextChanged = true;
                        renderer.InputText = inputHistory.Next;
                        renderer.MoveCursorToEnd();
                    }
                }else if(userInput.MoveUp){

                    if(renderer.IsAlternativeOptionsActive){
                        selector.MoveLast();
                    }else{
                        ignoreTextChanged = true;
                        renderer.InputText = inputHistory.Last;
                        renderer.MoveCursorToEnd();
                    }
                }
                return;
            }
            if(userInput.Focus){
                renderer.Focus();
                renderer.ActivateInput();
                OnFocus?.Invoke();
            }
        }
        
        string AddTimeInfo(string msg){
            if( outputWithTime ){
                return CLIUtils.TimeInfo + msg;
            }
            return msg;
        }
        string[] AddTimeInfo(string[] msgs){

            if( outputWithTime ){
                var timeInfo = CLIUtils.TimeInfo;
                for(int i = 0; i < msgs.Length; i ++){
                    msgs[i] = timeInfo + msgs[i];
                }
                return msgs;
            }
            return msgs;
        }

        /// <summary>Output message on console</summary>
        public void Output(string msg) => 
            logManager.Output(AddTimeInfo(msg));

        /// <summary>Output message on console with given color</summary>
        public void Output(string msg, string color = "#ffffff") => logManager.Output(AddTimeInfo(msg), color);

        /// <summary>Output messages on console</summary>
        public void Output(string[] msgs, string color = "#ffffff"){

            foreach(var m in AddTimeInfo(msgs))
                logManager.Output(m, color);
        }

        /// <summary>Output messages on console</summary>
        public void Output(string[] msgs){

            foreach(var m in AddTimeInfo(msgs))
                logManager.Output(m);
        }

        /// <summary>clear current console</summary>
        public void ClearOutputPanel() => renderer.Clear();

        public void OnTextChanged(string text){
            /* when input new string, should query commands from commandSystem */

            if(ignoreTextChanged){
                ignoreTextChanged = false;
                return;
            }

            string queryText = renderer.InputTextToCursor;
            var result = commandSystem.GetCurrentSuggestions(queryText, alternativeCommandCount, CLIUtils.FindSimilarity);

            if(result.Length == 0){
                if(renderer.IsAlternativeOptionsActive){
                    renderer.IsAlternativeOptionsActive = false;
                }
                return;
            }

            /* show options panel */
            var optionsBuffer = new List<string>();
            var list = new List<string>();
            foreach(var elem in result){
                optionsBuffer.Add(elem.primary);
                list.Add(elem.ToString());
            }
            if(!renderer.IsAlternativeOptionsActive){
                renderer.IsAlternativeOptionsActive = true;
            }
            selector.LoadOptions(optionsBuffer);
            renderer.AlternativeOptions = list;
            renderer.AlternativeOptionsIndex = selector.SelectionIndex;
        }

        /// <summary>input string into current console</summary>
        public void OnSubmit(string text){

            if(renderer.IsAlternativeOptionsActive && selector.GetCurrentSelection(out string selection)){
                renderer.InputTextToCursor = commandSystem.TakeSuggestion(renderer.InputTextToCursor, selection);
                renderer.IsAlternativeOptionsActive = false;
                renderer.ActivateInput();
                renderer.SetInputCursorPosition(renderer.InputText.Length);
                return;
            }

            if(renderer.IsAlternativeOptionsActive)
                renderer.IsAlternativeOptionsActive = false;

            Output(text);
            if(text.Length > 0){
                var ex = commandSystem.Execute(text, out object executeResult);
                if( ex == null ){
                    inputHistory.Record(text);
                    OutputResult(executeResult);
                }else{
                    if(shouldRecordFailedCommand)inputHistory.Record(text);
                    Output(ex.Message, "#f27a5f");
                    if( outputStackTraceOfCommandExecution )
                        Output(ex.StackTrace, "#f27a5f");
                }
                renderer.InputText = string.Empty;
            }
            renderer.MoveScrollBarToEnd();
            renderer.ActivateInput();
        }

        void OutputResult(object instance){

            if( instance == null )return;
            var debugInfos = DebugHelper.GetDebugInfos(instance);
            if (debugInfos.Length == 0){
                Output(instance.ToString());
                return;
            }
            Output($"---------- {instance} start ----------");
            foreach(var (message, color) in debugInfos){
                Output(message, color);
            }
            Output($"---------- {instance} end ----------");
        }
    }
}