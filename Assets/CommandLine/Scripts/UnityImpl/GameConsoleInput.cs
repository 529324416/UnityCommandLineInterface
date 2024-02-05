using UnityEngine;

namespace RedSaw.CommandLineInterface.UnityImpl
{

    /// <summary>default implementation of IConsoleInput with legacy Input</summary>
    public class UserInput : IConsoleInput
    {
        /// <summary>push ctrl+c or other keys to focus on console</summary>
        public bool Focus => Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C);

        /// <summary>push up to get history input or change selection of alternative options</summary>
        public bool MoveUp => Input.GetKeyDown(KeyCode.UpArrow);

        /// <summary>push down to get history input or change selection of alternative options</summary>
        public bool MoveDown => Input.GetKeyDown(KeyCode.DownArrow);

        /// <summary>push esc or other keys to quit focus on console</summary>
        public bool QuitFocus => Input.GetKeyDown(KeyCode.Escape);

        /// <summary>push F1 or other keys to show or hide console</summary>
        public bool ShowOrHide => Input.GetKeyDown(KeyCode.F1);
    }
}