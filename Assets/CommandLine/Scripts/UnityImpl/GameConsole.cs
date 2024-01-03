using UnityEngine;

namespace RedSaw.CommandLineInterface.UnityImpl{

    /// <summary>the final wrapper of CommandConsoleSystem build in Unity</summary>
    [RequireComponent(typeof(GameConsoleRenderer))]
    public class GameConsole : MonoBehaviour{

        static Console Instance { get; set; }

        void Awake(){
            var renderer = GetComponent<GameConsoleRenderer>();
            if(renderer == null){
                Debug.LogError("ConsoleRenderer has not found!");
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            Instance = new Console(renderer, new UserInput());
        }
        void Update() => Instance.Update();

        public static void Output(string msg) => Instance.Output(msg);
        public static void Output(string msg, string color) => Instance.Output(msg, color);
        public static void Output(object data){
            string _ = data == null ? "null" : data.ToString();
            Instance.Output(_);
        }
        public static void Output(object data, string color){
            string _ = data == null ? "null" : data.ToString();
            Instance.Output(_, color);
        }
        public static void Output(string[] msg) => Instance.Output(msg);
        public static void Output(string[] msg, string color) => Instance.Output(msg, color);
    }
}