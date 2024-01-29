using System;
using System.Collections.Generic;
using System.Reflection;

namespace RedSaw.CommandLineInterface{
    
    public enum SuggestionType{

        /// <summary>no suggestion</summary>
        None,

        /// <summary>search property or local variable</summary>
        Variable,

        /// search member of a type
        Member,

        /// search command
        Command,
    }

    /// <summary> save current suggestion query information, provide to 
    /// current command system to get some suggestion back</summary>
    public readonly struct SuggestionQuery{

        public static readonly SuggestionQuery None = 
            new(SuggestionType.None, string.Empty);

        public readonly SuggestionType suggestionType;
        public readonly string queryStr;
        public readonly Type queryType;

        public SuggestionQuery(SuggestionType suggestionType, string queryStr, Type queryType){

            this.suggestionType = suggestionType;
            this.queryStr = queryStr;
            this.queryType = queryType;
        }
        public SuggestionQuery(SuggestionType suggestionType, string queryStr) : 
            this(suggestionType, queryStr, null){}

        public override string ToString()
        {
            switch(suggestionType){

                case SuggestionType.Variable:
                    return $"Variable -> \"{queryStr}\"";

                case SuggestionType.Command:
                    return $"Command -> \"{queryStr}\"";

                case SuggestionType.Member:
                    return $"Member -> \"{queryType.Name}.{queryStr}\"";

                default:
                    return "No Suggestions";
            }
        }
    }

    public delegate bool QueryVariableType(string name, out Type type);

    /// <summary>
    /// use to analyze input string and provide suggestions type
    /// </summary>
    class InputBehaviourStateMachine{
        
        public readonly Stack<InputBehaviourState> stateStack;
        private Func<string, Type> getVariableType;
        private InputBehaviourState currentState;

        /// <summary> current suggestion </summary>
        public SuggestionQuery CurrentSuggestionQuery => currentState.GetSuggestion();

        public InputBehaviourStateMachine(Func<string, Type> getVariableType){

            this.getVariableType = getVariableType;
            stateStack = new Stack<InputBehaviourState>();
            currentState = new IBS_Ready(this);
        }
        public InputBehaviourState TryGetVariableMemberState(string variableName){

            var type = getVariableType(variableName);
            if( type == null ) return new IBS_Unknown(this);
            return new IBS_Member(this, type);
        }

        public void StepForward(char c){

            var nextState = currentState.StepForward(c);
            if( nextState != currentState ){
                stateStack.Push(currentState);
                currentState = nextState;
            }
        }
        public void StepBackward(){

            if( currentState.StepBackward() ){
                currentState = stateStack.Pop();
            }
        }
        public void Reset(){
            
            stateStack.Clear();
            currentState = new IBS_Ready(this);
        }
        public void Reset(string input){

            Reset();
            foreach( char c in input ){
                StepForward(c);
            }
        }

    }



    /// <summary> character automaton state </summary>
    abstract class InputBehaviourState{

        protected readonly InputBehaviourStateMachine stateMachine;

        public InputBehaviourState(InputBehaviourStateMachine stateMachine){
            this.stateMachine = stateMachine;
        }

        /// <summary>input new character and check if </summary>
        /// <param name="c">new input character</param>
        /// <returns>should change current state to nextState</returns>
        public abstract InputBehaviourState StepForward(char c);

        /// <summary>step backward, return to last state</summary>
        /// <returns>should change current state to last state</returns>
        public abstract bool StepBackward();

        /// <summary>get suggestion query</summary>
        /// <returns>current suggestion query</returns>
        public virtual SuggestionQuery GetSuggestion() => SuggestionQuery.None;

        protected IBS_Wait Wait() => new IBS_Wait(stateMachine);
        protected IBS_Unknown Unknown() => new IBS_Unknown(stateMachine);
        protected IBS_Variable Variable() => new IBS_Variable(stateMachine);
        protected InputBehaviourState VariableMember(string typeName) => stateMachine.TryGetVariableMemberState(typeName);
        protected InputBehaviourState Member(Type type, string name){
            MemberInfo member = type.GetDefaultMember(name);
            if( member == null ){
                return new IBS_Unknown(stateMachine);
            }
            switch(member.MemberType){
                case MemberTypes.Field:
                    return new IBS_Member(stateMachine, ((FieldInfo)member).FieldType);
                case MemberTypes.Property:
                    return new IBS_Member(stateMachine, ((PropertyInfo)member).PropertyType);
                default:
                    return new IBS_Unknown(stateMachine);
            }
        }
    }

    abstract class InputBehaviourStateWithInput : InputBehaviourState{

        public string CurrentInput{ get; protected set; } = string.Empty;
        public InputBehaviourStateWithInput(InputBehaviourStateMachine stateMachine, string input = ""):base(stateMachine){
            this.CurrentInput = input;
        }
    }




    class IBS_Ready : InputBehaviourStateWithInput
    {
        public IBS_Ready(InputBehaviourStateMachine stateMachine) : base(stateMachine){}
        public override InputBehaviourState StepForward(char c)
        {
            switch(c){
                case Lexer.VAR: return Variable();
                default: return Unknown();
            }
        }
        public override bool StepBackward() => false;
    }

    class IBS_Wait : InputBehaviourStateWithInput
    {
        public IBS_Wait(InputBehaviourStateMachine stateMachine, string input = ""):base(stateMachine, input){}
        public override InputBehaviourState StepForward(char c)
        {
            if( Lexer.IsWhiteSpace(c) ) return Wait();
            switch(c){
                case Lexer.VAR: return Variable();
                
                default: return Unknown();
            }
        }
        public override bool StepBackward() => true;
    }


    /// <summary>
    /// bad state only use to mark the input is bad, 
    /// and wait for user delete all and reinput
    /// </summary>
    class IBS_Unknown : InputBehaviourState{

        private int count;
        public IBS_Unknown(InputBehaviourStateMachine stateMachine) : base(stateMachine)
        {
            this.count = 0;
        }
        public override InputBehaviourState StepForward(char c)
        {
            count ++;
            return this;
        }
        public override bool StepBackward()
        {
            return --count < 0;
        }
    }

    class IBS_Variable : InputBehaviourStateWithInput
    {
        public IBS_Variable(InputBehaviourStateMachine stateMachine, string input = "") : base(stateMachine, input){}
        public override InputBehaviourState StepForward(char c)
        {
            if( CurrentInput.Length == 0 ){
                if( char.IsLetter(c) || c == Lexer.UNDERLINE ){
                    CurrentInput += c;
                    return this;
                }
                return Unknown();
            }

            if( char.IsLetterOrDigit(c) || c == Lexer.UNDERLINE ){

                CurrentInput += c;
                return this;
            }
            if( c == Lexer.DOT ){

                return VariableMember(CurrentInput);
            }
            return Unknown();
        }
        public override bool StepBackward()
        {
            if( CurrentInput.Length == 0 )return true;
            CurrentInput = CurrentInput.Substring(0, CurrentInput.Length - 1);
            return false;
        }
        public override SuggestionQuery GetSuggestion() => new(SuggestionType.Variable, CurrentInput);
    }

    class IBS_Member : InputBehaviourStateWithInput
    {
        private Type queryType;
        public IBS_Member(InputBehaviourStateMachine stateMachine, Type type) : base(stateMachine, string.Empty)
        {
            this.queryType = type;
        }

        public override bool StepBackward()
        {
            if( CurrentInput.Length == 0 )return true;
            CurrentInput = CurrentInput.Substring(0, CurrentInput.Length - 1);
            return false;
        }

        public override InputBehaviourState StepForward(char c)
        {
            if( CurrentInput.Length == 0 ){
                if( char.IsLetter(c) || c == Lexer.UNDERLINE ){
                    CurrentInput += c;
                    return this;
                }
                return Unknown();
            }

            if( char.IsLetterOrDigit(c) || c == Lexer.UNDERLINE ){

                CurrentInput += c;
                return this;
            }else if( c == Lexer.DOT ){

                return Member(queryType, CurrentInput);
            }
            return Unknown();
        }
        public override SuggestionQuery GetSuggestion()
        {
            return new(SuggestionType.Member, CurrentInput, queryType);
        }
    }


    /// <summary>
    /// lexer use to parse input realtime
    /// </summary>
    public class CharAutomaton{

        private InputBehaviourStateMachine stateMachine;
        private string lastInput = string.Empty;

        public CharAutomaton(Func<string, Type> getVariableType){
            stateMachine = new InputBehaviourStateMachine(getVariableType);
        }

        /// <summary>
        /// input current input text and get suggestion query
        /// </summary>
        public SuggestionQuery Input(string newInput){

            /* input string is empty */
            if( newInput == null || newInput.Length == 0 ){

                stateMachine.Reset();
                lastInput = string.Empty;
                return stateMachine.CurrentSuggestionQuery;
            }

            /* input string is equal to last input, it maybe nothing change 
            or an totally new input */
            if( newInput.Length == lastInput.Length ){
                if( newInput == lastInput ){
                    return stateMachine.CurrentSuggestionQuery;
                }
                Rehandle(newInput);
                return stateMachine.CurrentSuggestionQuery;
            }

            /* input some new characters */
            if( newInput.Length > lastInput.Length ){

                /* add some new characters */
                if(newInput.StartsWith(lastInput)){
                    foreach(char c in newInput.Substring(lastInput.Length)){
                        stateMachine.StepForward(c);
                    }
                    lastInput = newInput;
                    return stateMachine.CurrentSuggestionQuery;
                }
                
                /* maybe an totally new input */
                Rehandle(newInput);
                return stateMachine.CurrentSuggestionQuery;
            }

            /* delete some characaters */
            if( newInput.Length < lastInput.Length ){
                if(lastInput.StartsWith(newInput)){
                    foreach(char c in lastInput.Substring(newInput.Length)){
                        stateMachine.StepBackward();
                    }
                    lastInput = newInput;
                    return stateMachine.CurrentSuggestionQuery;
                }
                Rehandle(newInput);
                return stateMachine.CurrentSuggestionQuery;
            }

            /* code never run below */
            return stateMachine.CurrentSuggestionQuery;
        }
        void Rehandle(string input){

            lastInput = input;
            stateMachine.Reset(input);
        }
    }


    /// <summary>
    /// used to save suggestion information 
    /// </summary>
    public struct Suggestion{

        public readonly string primary;
        public readonly string description;

        public Suggestion(string primary, string description){

            this.primary = primary;
            this.description = description;
        }
        public Suggestion(string primary){
            
            this.primary = primary;
            this.description = string.Empty;
        }
        public override string ToString()
        {
            if(this.description.Length > 0){
                return $"{primary} :{description}";
            }
            return primary;
        }
    }
}