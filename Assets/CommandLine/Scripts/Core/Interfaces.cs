using System;
using System.Collections.Generic;

/* 
    use interface between the real implemetations to decoupling the whole system's components
*/

namespace RedSaw.CommandLineInterface{

    /// <summary>
    /// define the user input of CommandConsole, cause Unity has two different 
    /// input solutions.
    /// </summary>
    public interface IConsoleInput{

        /// <summary>push up to get history input or change selection of alternative options</summary>
        bool MoveUp{ get; }

        /// <summary>push down to get history input or change selection of alternative options</summary>
        bool MoveDown{ get; }

        /// <summary>push ctrl+c or other keys to focus on console</summary>
        bool Focus{ get; }

        /// <summary>push esc or other keys to quit focus on console</summary>
        bool QuitFocus{ get; }

        /// <summary>push F1 or other keys to show or hide console</summary>
        bool ShowOrHide{ get; }
    }

    /// <summary>define the interface of console renderer</summary>
    public interface IConsoleRenderer{

        bool IsVisible{ get; set; }

        #region InputField

            /// <summary>check if input field is focused</summary>
            bool IsInputFieldFocus{ get; }

            /// <summary>input text</summary>
            string InputText{ get; set; }

            /// <summary>focus on input field</summary>
            void Focus();

            /// <summary>activate input field</summary>
            void ActivateInput();

            /// <summary>clear input field content</summary>
            void ClearInput();

            /// <summary>quit focus on input field</summary>
            void QuitFocus();

            /// <summary>add listener on input field</summary>
            void BindOnTextChanged(Action<string> callback);

            /// <summary>add listener on input field</summary>
            void BindOnSubmit(Action<string> callback);

            /// <summary>set cursor position</summary>
            void SetInputCursorPosition(int pos);

            /// <summary>make scrollbar position at last position</summary>
            void MoveScrollBarToEnd();

            void MoveCursorToEnd(){
                if(InputText != null)SetInputCursorPosition(InputText.Length);
            }

        #endregion

        #region OutputPanel

            /// <summary>output panel capacity</summary>
            int OutputPanelCapacity{ get; set; }

            /// <summary>output message</summary>
            void Output(string msg);

            /// <summary>output message</summary>
            void Output(string[] msg);

            /// <summary>output message with given color</summary>
            void Output(string msg, string color = "#ffffff");

            /// <summary>output message</summary>
            void Output(string[] msg, string color = "#ffffff");

            /// <summary>clear current console</summary>
            void Clear();

        #endregion

        #region AlternativeOptions

            /// <summary>is current alternative panel showing now</summary>
            bool IsAlternativeOptionsActive{ get; set; }

            /// <summary>
            /// render current alternative options
            /// </summary>
            List<string> AlternativeOptions{ set; }

            /// <summary>
            /// highlight this alternative options index
            /// </summary>
            int AlternativeOptionsIndex{ set; }
        #endregion
    }

    /// <summary>define the interface of command system</summary>
    public interface ICommandSystem{

        /// <summary>get all command infos</summary>
        IEnumerable<string> CommandInfos{ get; }

        /// <summary>execute command</summary>
        /// <param name="command">command string</param>
        /// <returns>if command executed</returns>
        bool Execute(string command);

        /// <summary>execute command, but don't output any error informations</summary>
        /// <param name="command">command string</param>
        /// <returns>if command executed</returns>
        bool ExecuteSlience(string command);

        /// <summary>query commands</summary>
        /// <param name="command">command string</param>
        /// <param name="count">count of alternative commands</param>
        /// <param name="scoreFunc">score function</param>
        /// <returns>command infos</returns>
        List<(string, string)> Query(string command, int count);

        /// <summary>set output function</summary>
        /// <param name="outputFunc">output function</param>
        void SetOutputFunc(Action<string> outputFunc);

        /// <summary>set output error function</summary>
        /// <param name="outputFunc">output function</param>
        void SetOutputErrFunc(Action<string> outputFunc);

        void UseDefualtCommand();
    }
}