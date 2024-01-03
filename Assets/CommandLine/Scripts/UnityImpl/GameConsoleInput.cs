using UnityEngine;

namespace RedSaw.CommandLineInterface.UnityImpl{

    public class UserInput : IConsoleInput
    {
        public bool Focus => Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C);
        public bool MoveUp => Input.GetKeyDown(KeyCode.UpArrow);
        public bool MoveDown => Input.GetKeyDown(KeyCode.DownArrow);
        public bool QuitFocus => Input.GetKeyDown(KeyCode.Escape);
        public bool ShowOrHide => Input.GetKeyDown(KeyCode.F1);
    }
}