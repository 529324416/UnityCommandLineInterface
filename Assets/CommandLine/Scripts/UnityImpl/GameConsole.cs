using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedSaw.CommandLineInterface.UnityImpl{

    /// <summary>the final wrapper of CommandConsoleSystem build in Unity</summary>
    [RequireComponent(typeof(GameConsoleRenderer))]
    public class GameConsole : MonoBehaviour{

        static Console Instance { get; set; }

        [Command("help", Desc = "show all commands and descriptions")]
        static void CommandHelp(){
                
            foreach(var command in Instance.TotalCommandInfos){
                Instance.Output(command, "green");
            }
        }

        [Command("clear", Desc = "clear output panel")]
        static void CommandClear(){

            Instance.ClearOutputPanel();
        }

        [Command("print", Desc = "print something on the console")]
        static void CommandPrint(string value){

            Instance.Output(value);
        }

        [Command("show_logo", Desc = "show redsaw logo")]
        static void OutputLogo(){

            /*
                _____          _  _____                 _____ _             _ _       
                |  __ \        | |/ ____|               / ____| |           | (_)      
                | |__) |___  __| | (___   __ ___      _| (___ | |_ _   _  __| |_  ___  
                |  _  // _ \/ _` |\___ \ / _` \ \ /\ / /\___ \| __| | | |/ _` | |/ _ \ 
                | | \ \  __/ (_| |____) | (_| |\ V  V / ____) | |_| |_| | (_| | | (_) |
                |_|  \_\___|\__,_|_____/ \__,_| \_/\_/ |_____/ \__|\__,_|\__,_|_|\___/ 
            */

            string[] logs = new string[]{
                " _____          _  _____                 _____ _             _ _        ",
                "|  __ \\        | |/ ____|               / ____| |           | (_)      ",
                "| |__) |___  __| | (___   __ ___      _| (___ | |_ _   _  __| |_  ___  ",
                "|  _  // _ \\/ _` |\\___ \\ / _` \\ \\ /\\ / /\\___ \\| __| | | |/ _` | |/ _ \\ ",
                "| | \\ \\  __/ (_| |____) | (_| |\\ V  V / ____) | |_| |_| | (_| | | (_) |",
                "|_|  \\_\\___|\\__,_|_____/ \\__,_| \\_/\\_/ |_____/ \\__|\\__,_|\\__,_|_|\\___/ "
            };
            Instance.Output(logs, "#df426e");
        }


        public class UserInput : IConsoleInput
        {
            public bool MoveUp => Input.GetKeyDown(KeyCode.UpArrow);
            public bool MoveDown => Input.GetKeyDown(KeyCode.DownArrow);
            public bool Submit => Input.GetKeyDown(KeyCode.A);
            public bool Focus => Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C);
            public bool QuitFocus => Input.GetKeyDown(KeyCode.Escape);
        }

        public class CommandSystem : ICommandSystem
        {

            public IEnumerable<string> CommandInfos{
                get{
                    foreach(var command in CommandLineInterface.CommandSystem.TotalCommands){
                        yield return command.ToString();
                    }
                }
            }

            public bool Execute(string command) => CommandLineInterface.CommandSystem.Execute(command);
            public List<(string, string)> Query(string input, int count)
            {
                var commands = CommandLineInterface.CommandSystem.QueryCommands(input, count, CLIUtils.SimpleFilter);
                var result = new List<(string, string)>();
                foreach(var command in commands){
                    result.Add((command.Name, command.Description));
                }
                return result;
            }

            public void SetOutputFunc(Action<string> outputFunc) => CommandLineInterface.CommandSystem.SetOutputFunc(outputFunc);
        }

        void Awake(){

            var userInput = new UserInput();
            var renderer = GetComponent<GameConsoleRenderer>();
            Instance = new Console(new CommandSystem(), renderer, userInput);
            OutputLogo();
        }

        void Update(){
            Instance.Update();
        }
    }
}