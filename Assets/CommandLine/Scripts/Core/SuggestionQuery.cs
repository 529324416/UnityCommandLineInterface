using System;
using System.Collections.Generic;
using System.Reflection;

namespace RedSaw.CommandLineInterface
{

    public enum SuggestionType
    {

        /// <summary>no suggestion</summary>
        None,

        /// <summary>search property or local variable</summary>
        Variable,

        /// <summary>search member of a type</summary>
        Member,

        /// <summary>search command</summary>
        Command,
    }

    /// <summary> save current suggestion query information, provide to 
    /// current command system to get some suggestion back</summary>
    public readonly struct SuggestionQuery
    {

        public static readonly SuggestionQuery None =
            new(SuggestionType.None, string.Empty);

        public readonly SuggestionType suggestionType;
        public readonly string queryStr;
        public readonly Type queryType;

        public SuggestionQuery(SuggestionType suggestionType, string queryStr, Type queryType)
        {

            this.suggestionType = suggestionType;
            this.queryStr = queryStr;
            this.queryType = queryType;
        }
        public SuggestionQuery(SuggestionType suggestionType, string queryStr) :
            this(suggestionType, queryStr, null)
        { }

        public override string ToString()
        {
            return suggestionType switch
            {
                SuggestionType.Variable => $"Variable -> \"{queryStr}\"",
                SuggestionType.Command => $"Command -> \"{queryStr}\"",
                SuggestionType.Member => $"Member -> \"{queryType.Name}.{queryStr}\"",
                _ => "No Suggestions",
            };
        }
    }

    public delegate bool QueryVariableType(string name, out Type type);

    /// <summary>
    /// use to analyze input string and provide suggestions type
    /// </summary>
    class InputBehaviourStateMachine
    {

        public readonly Stack<InputBehaviourState> stateStack;
        private readonly Func<string, Type> getVariableType;
        private readonly Func<string, Type> getCallableType;
        private InputBehaviourState currentState;

        public string CurrentStatus
        {
            get
            {
                if (currentState == null) return "<No State>";
                return currentState.ToString();
            }
        }

        /// <summary> current suggestion </summary>
        public SuggestionQuery CurrentSuggestionQuery => currentState.GetSuggestion();

        public InputBehaviourStateMachine(Func<string, Type> getVariableType, Func<string, Type> getCallableType)
        {

            this.getVariableType = getVariableType;
            this.getCallableType = getCallableType;
            stateStack = new Stack<InputBehaviourState>();
            currentState = new IBS_Ready(this);
        }
        public InputBehaviourState TryGetVariableMemberState(string variableName)
        {

            var type = getVariableType(variableName);
            if (type == null) return new IBS_Unknown(this);
            return new IBS_Member(this, type);
        }
        public InputBehaviourState TryGetCallableMemberState(string callableName)
        {

            var type = getCallableType(callableName);
            if (type == null) return new IBS_Unknown(this);
            return new IBS_Member(this, type);
        }

        public void StepForward(char c)
        {

            var nextState = currentState.StepForward(c);
            if (nextState != currentState)
            {
                if (nextState.ShouldCollapse)
                {
                    stateStack.Pop();
                }
                stateStack.Push(currentState);
                currentState = nextState;
            }
        }
        public void StepBackward()
        {

            if (currentState.StepBackward())
            {
                currentState = stateStack.Pop();
            }
        }
        public void Reset()
        {

            stateStack.Clear();
            currentState = new IBS_Ready(this);
        }
        public void Reset(string input)
        {

            Reset();
            foreach (char c in input)
            {
                StepForward(c);
            }
        }

    }



    /// <summary> character automaton state </summary>
    abstract class InputBehaviourState
    {

        protected readonly InputBehaviourStateMachine stateMachine;
        public bool ShouldCollapse { get; protected set; } = false;

        /// <summary>
        /// mark if current state is after an assign operation
        /// </summary>
        public bool AfterAssign { get; protected set; } = false;

        public InputBehaviourState(InputBehaviourStateMachine stateMachine, bool afterAssign = false)
        {

            this.stateMachine = stateMachine;
            this.AfterAssign = afterAssign;
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

        protected IBS_Wait Wait() => new(stateMachine);
        protected IBS_Unknown Unknown() => new(stateMachine);
        protected IBS_Variable Variable() => new(stateMachine);
        protected InputBehaviourState VariableMember(string typeName) => stateMachine.TryGetVariableMemberState(typeName);
        protected InputBehaviourState CommandMember(string commandName) => stateMachine.TryGetCallableMemberState(commandName);
        protected InputBehaviourState Member(Type type, string name)
        {

            MemberInfo member = type.GetDefaultMember(name);
            if (member == null)
            {
                return new IBS_Unknown(stateMachine);
            }
            return member.MemberType switch
            {
                MemberTypes.Field => new IBS_Member(stateMachine, ((FieldInfo)member).FieldType),
                MemberTypes.Property => new IBS_Member(stateMachine, ((PropertyInfo)member).PropertyType),
                _ => new IBS_Unknown(stateMachine),
            };
        }
        protected IBS_Command Callable(char firstChar, bool shouldCollapse = false, bool afterAssign = false)
        {

            return new IBS_Command(stateMachine, firstChar, shouldCollapse, afterAssign);
        }
        public override string ToString() => "<State>";
    }

    abstract class InputBehaviourStateWithInput : InputBehaviourState
    {

        public string CurrentInput { get; protected set; } = string.Empty;
        public InputBehaviourStateWithInput(
            InputBehaviourStateMachine stateMachine,
            string input = "",
            bool afterAssign = false
        ) : base(stateMachine, afterAssign)
        {
            this.CurrentInput = input;
        }
        public override string ToString() => $"<State: {CurrentInput}>";
    }




    class IBS_Ready : InputBehaviourStateWithInput
    {
        public IBS_Ready(InputBehaviourStateMachine stateMachine) : base(stateMachine) { }
        public override InputBehaviourState StepForward(char c)
        {
            switch (c)
            {
                case Lexer.VAR:
                    return Variable();

                case Lexer.DOUBLE_QUOTE:
                case Lexer.SINGLE_QUOTE:
                    return new IBS_String(stateMachine, c);

                case Lexer.UNDERLINE:
                    return Callable(c, shouldCollapse: true, AfterAssign);

                default:
                    if (Lexer.IsWhiteSpace(c)) return Wait();
                    if (c == Lexer.UNDERLINE || char.IsLetter(c)) return Callable(c);
                    return Unknown();
            }

        }
        public override bool StepBackward() => false;
        public override string ToString() => "<Ready>";
    }

    class IBS_Wait : InputBehaviourStateWithInput
    {

        public IBS_Wait(InputBehaviourStateMachine stateMachine, string input = "") : base(stateMachine, input)
        {
        }
        public override InputBehaviourState StepForward(char c)
        {

            switch (c)
            {
                case Lexer.VAR:
                    return Variable();

                case Lexer.DOUBLE_QUOTE:
                case Lexer.SINGLE_QUOTE:
                    return new IBS_String(stateMachine, c);

                default:
                    if (Lexer.IsWhiteSpace(c)) return Wait();
                    if (c == Lexer.ASSIGN) return Wait();
                    if (char.IsLetter(c) || c == Lexer.UNDERLINE) return Callable(c, shouldCollapse: true, AfterAssign);
                    return Unknown();
            }
        }

        public override bool StepBackward() => true;
        public override string ToString() => $"<Wait>";
    }


    class IBS_Command : InputBehaviourStateWithInput
    {

        public IBS_Command(
            InputBehaviourStateMachine stateMachine,
            char firstChar,
            bool shouldCollapse = false,
            bool afterAssign = false) : base(stateMachine, firstChar.ToString(), afterAssign)
        {

            CurrentInput = firstChar.ToString();
            this.ShouldCollapse = shouldCollapse;
        }
        public override InputBehaviourState StepForward(char c)
        {
            if (Lexer.IsWhiteSpace(c)) return Wait();
            if (c == Lexer.DOT) return CommandMember(CurrentInput);
            if (c == Lexer.UNDERLINE || char.IsLetterOrDigit(c))
            {
                CurrentInput += c;
                return this;
            }
            return Unknown();
        }
        public override bool StepBackward()
        {
            if (CurrentInput.Length == 0) return true;
            CurrentInput = CurrentInput[..^1];
            return false;
        }
        public override SuggestionQuery GetSuggestion() => new(SuggestionType.Command, CurrentInput);
        public override string ToString() => $"<Command: {CurrentInput}>";
    }


    /// <summary>
    /// bad state only use to mark the input is bad, 
    /// and wait for user delete all and reinput
    /// </summary>
    class IBS_Unknown : InputBehaviourState
    {

        private int count;
        public IBS_Unknown(InputBehaviourStateMachine stateMachine) : base(stateMachine)
        {
            this.count = 0;
        }
        public override InputBehaviourState StepForward(char c)
        {
            count++;
            return this;
        }
        public override bool StepBackward()
        {
            return --count < 0;
        }
        public override string ToString() => "<Unknown>";
    }

    class IBS_Variable : InputBehaviourStateWithInput
    {
        public IBS_Variable(InputBehaviourStateMachine stateMachine, string input = "") : base(stateMachine, input) { }
        public override InputBehaviourState StepForward(char c)
        {
            if (CurrentInput.Length == 0)
            {
                if (char.IsLetter(c) || c == Lexer.UNDERLINE)
                {
                    CurrentInput += c;
                    return this;
                }
                return Unknown();
            }

            if (char.IsLetterOrDigit(c) || c == Lexer.UNDERLINE)
            {

                CurrentInput += c;
                return this;
            }
            if (c == Lexer.DOT)
            {

                return VariableMember(CurrentInput);
            }
            return Unknown();
        }
        public override bool StepBackward()
        {
            if (CurrentInput.Length == 0) return true;
            CurrentInput = CurrentInput[..^1];
            return false;
        }
        public override SuggestionQuery GetSuggestion() => new(SuggestionType.Variable, CurrentInput);
        public override string ToString() => $"<Variable: {CurrentInput}>";
    }

    class IBS_String : InputBehaviourStateWithInput
    {

        private readonly char quote;
        public IBS_String(InputBehaviourStateMachine stateMachine, char quote, string input = "") : base(stateMachine, input)
        {
            this.quote = quote;
        }
        public override InputBehaviourState StepForward(char c)
        {
            if (c == quote) return Wait();
            CurrentInput += c;
            return this;
        }
        public override bool StepBackward()
        {
            if (CurrentInput.Length == 0) return true;
            CurrentInput = CurrentInput[..^1];
            return false;
        }
        public override string ToString() => $"<String: {CurrentInput}>";
    }

    class IBS_Member : InputBehaviourStateWithInput
    {
        private readonly Type queryType;
        public IBS_Member(InputBehaviourStateMachine stateMachine, Type type) : base(stateMachine, string.Empty)
        {
            queryType = type;
        }

        public override bool StepBackward()
        {
            if (CurrentInput.Length == 0) return true;
            CurrentInput = CurrentInput[..^1];
            return false;
        }

        public override InputBehaviourState StepForward(char c)
        {
            if (CurrentInput.Length == 0)
            {
                if (char.IsLetter(c) || c == Lexer.UNDERLINE)
                {
                    CurrentInput += c;
                    return this;
                }
                return Unknown();
            }
            if (char.IsLetterOrDigit(c) || c == Lexer.UNDERLINE)
            {

                CurrentInput += c;
                return this;
            }
            else if (c == Lexer.DOT)
            {

                return Member(queryType, CurrentInput);
            }
            return Callable(c);
        }
        public override SuggestionQuery GetSuggestion()
        {
            return new(SuggestionType.Member, CurrentInput, queryType);
        }
        public override string ToString() => $"<Member: {CurrentInput}>";
    }




    /// <summary>
    /// lexer use to parse input realtime
    /// </summary>
    public class CharAutomaton
    {

        private readonly InputBehaviourStateMachine stateMachine;
        private string lastInput = string.Empty;
        public string CurrentStatus => stateMachine.CurrentStatus;

        public CharAutomaton(
            Func<string, Type> getVariableType,
            Func<string, Type> getCallableType
        )
        {
            stateMachine = new InputBehaviourStateMachine(getVariableType, getCallableType);
        }

        /// <summary>
        /// input current input text and get suggestion query
        /// </summary>
        public SuggestionQuery Input(string newInput)
        {

            /* input string is empty */
            if (newInput == null || newInput.Length == 0)
            {

                stateMachine.Reset();
                lastInput = string.Empty;
                return SuggestionQuery.None;
            }

            /* input string is equal to last input, it maybe nothing change 
            or an totally new input */
            if (newInput.Length == lastInput.Length)
            {
                if (newInput == lastInput)
                {
                    return stateMachine.CurrentSuggestionQuery;
                }
                Rehandle(newInput);
                return stateMachine.CurrentSuggestionQuery;
            }

            /* input some new characters */
            if (newInput.Length > lastInput.Length)
            {

                /* add some new characters */
                if (newInput.StartsWith(lastInput))
                {
                    foreach (char c in newInput[lastInput.Length..])
                    {
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
            if (newInput.Length < lastInput.Length)
            {
                if (lastInput.StartsWith(newInput))
                {
                    foreach (char _ in lastInput[newInput.Length..])
                    {
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
        void Rehandle(string input)
        {

            lastInput = input;
            stateMachine.Reset(input);
        }
    }


    /// <summary>
    /// used to save suggestion information 
    /// </summary>
    public readonly struct Suggestion
    {

        public readonly string primary;
        public readonly string description;

        public Suggestion(string primary, string description)
        {

            this.primary = primary;
            this.description = description;
        }
        public Suggestion(string primary)
        {

            this.primary = primary;
            this.description = string.Empty;
        }
        public override string ToString()
        {
            if (this.description.Length > 0)
            {
                return $"{primary} :{description}";
            }
            return primary;
        }
    }
}