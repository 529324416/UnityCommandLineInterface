using System;
using System.Collections.Generic;
using System.Diagnostics;

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
                if(lastIndex < 0)lastIndex = history.Count - 1;
                return history[lastIndex --];
            }
        }
        /// <summary>get next input string</summary>
        public string Next{
            get{
                if(history.Count == 0)return string.Empty;
                if(lastIndex >= history.Count - 1)lastIndex = 0;
                return history[lastIndex ++];
            }
        }
    }


    /// <summary>command selector</summary>
    class LinearSelector{

        readonly Action<int> onSelectionChanged;
        int totalCount;
        int currentIndex;

        public int SelectionIndex => currentIndex;

        public LinearSelector(Action<int> onSelectionChanged){

            this.onSelectionChanged = onSelectionChanged;
            this.totalCount = 1;
            this.currentIndex = 0;
        }

        /// <summary>
        /// set current alternative options, and it must have
        /// at least one choice 
        /// </summary>
        public void SetTotalCount(int count){
            this.totalCount = Math.Max(count, 1);
            this.currentIndex = 0;
        }

        /// <summary>move to next alternative option</summary>
        public void MoveNext(){

            currentIndex = currentIndex < totalCount - 1 ? currentIndex + 1 : 0;
            onSelectionChanged?.Invoke(currentIndex);
        }

        /// <summary>move to last alternative option</summary>
        public void MoveLast(){

            currentIndex = currentIndex > 0 ? currentIndex - 1 : totalCount - 1;
            onSelectionChanged?.Invoke(currentIndex);
        }
    }

    /// <summary>command console</summary>
    public class Console{

        public event Action OnFocusOut;
        public event Action OnFocus;

        readonly int alternativeCommandCount;
        readonly bool shouldRecordFailedCommand;

        readonly ICommandSystem commandSystem;
        readonly IConsoleRenderer renderer;
        readonly IConsoleInput userInput;
        readonly InputHistory inputHistory;
        readonly LinearSelector selector;
        readonly List<string> optionsBuffer;

        public IEnumerable<string> TotalCommandInfos => commandSystem.CommandInfos;

        public Console(
            ICommandSystem commandSystem,
            IConsoleRenderer renderer, 
            IConsoleInput userInput,
            int memoryCapacity = 50,
            int alternativeCommandCount = 8,
            bool shouldRecordFailedCommand = true,
            int outputPanelCapacity = 400
        ){
            this.commandSystem = commandSystem;
            this.renderer = renderer;
            this.userInput = userInput;
            inputHistory = new InputHistory(memoryCapacity);
            selector = new LinearSelector(idx => {
                renderer.AlternativeOptionsIndex = idx;
                renderer.MoveCursorToEnd();
            });
            optionsBuffer = new();
            
            this.alternativeCommandCount = alternativeCommandCount;
            this.shouldRecordFailedCommand = shouldRecordFailedCommand;
            renderer.OutputPanelCapacity = outputPanelCapacity;
            renderer.BindOnSubmit(OnSubmit);
            renderer.BindOnTextChanged(OnTextChanged);
            this.commandSystem.SetOutputFunc(s => {
                renderer.Output(s, "#ff0000");
            });
        }
        public void Update(){

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
                        renderer.InputText = inputHistory.Next;
                    }
                }else if(userInput.MoveUp){

                    if(renderer.IsAlternativeOptionsActive){
                        selector.MoveLast();
                    }else{
                        renderer.InputText = inputHistory.Last;
                    }
                }
                return;
            }
            if(userInput.Focus){
                renderer.Focus();
                OnFocus?.Invoke();
            }
        }
        
        /// <summary>Output message on console</summary>
        public void Output(string msg) => renderer.Output(msg);

        /// <summary>Output message on console with given color</summary>
        public void Output(string msg, string color = "#ff0000") => renderer.Output(msg, color);

        /// <summary>Output message on console</summary>
        public void Output(string[] msg, string color = "#ffffff") => renderer.Output(msg, color);

        /// <summary>clear current console</summary>
        public void ClearOutputPanel() => renderer.Clear();

        /// <summary>input string into current console</summary>
        public void OnSubmit(string text){

            if(renderer.IsAlternativeOptionsActive){
                renderer.InputText = optionsBuffer[selector.SelectionIndex];
                renderer.IsAlternativeOptionsActive = false;
                renderer.ActivateInput();
                renderer.SetInputCursorPosition(renderer.InputText.Length);
                return;
            }

            renderer.Output(text);
            if(text.Length > 0){
                if(commandSystem.Execute(text)){
                    inputHistory.Record(text);
                }else{
                    if(shouldRecordFailedCommand)inputHistory.Record(text);
                }
                renderer.ActivateInput();
                renderer.InputText = string.Empty;
                renderer.MoveScrollBarToEnd();
            }
        }

        public void OnTextChanged(string text){
            /* when input new string, should query commands from commandSystem */

            /* hide options panel when there's no text or no commands found */
            if(text == null || text.Length == 0 || text.Contains(' ')){
                if(renderer.IsAlternativeOptionsActive){
                    renderer.IsAlternativeOptionsActive = false;
                }
                return;
            }

            var result = commandSystem.Query(text, alternativeCommandCount);
            if(result.Count == 0){
                if(renderer.IsAlternativeOptionsActive){
                    renderer.IsAlternativeOptionsActive = false;
                }
                return;
            }

            /* show options panel */
            optionsBuffer.Clear();
            var list = new List<string>();
            foreach(var elem in result){
                optionsBuffer.Add(elem.Item1);
                list.Add($"{elem.Item1}: {elem.Item2}");
            }
            if(!renderer.IsAlternativeOptionsActive){
                renderer.IsAlternativeOptionsActive = true;
            }
            selector.SetTotalCount(optionsBuffer.Count);
            renderer.AlternativeOptions = list;
            // renderer.SetInputCursorPosition(renderer.InputText.Length - 1);
        }
    }
}