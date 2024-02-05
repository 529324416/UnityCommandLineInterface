using UnityEngine;
using RedSaw.CommandLineInterface;

public class MyClass{

    [DebugInfo("health", Color = "#ff0000")]
    public int health = 100;

    [DebugInfo("name")]
    public string name = "hello world";

    public void Do(){
        Debug.Log("do something");
    }
}

public static class CommandExample
{
    [CommandProperty("myObj")]
    public static MyClass myObject = new MyClass();


    [Command("print")]
    static void Print(object value){
        if( value == null ){
            Debug.Log("null");
            return;
        }
        Debug.Log(value);
    }

    [Command("add")]
    static int Add(int a, int b = 1){
        return a + b;
    }


    [Command]
    static void printType(object value){
        if( value == null){
            Debug.Log(typeof(void));
            return;
        }
        Debug.Log(value.GetType());
    }

    [CommandValueParser(typeof(Vector2), Alias = "pos2")]
    public static bool TryParseVector2(string input, out object data){

        string[] result= input.Trim(new char[]{'(', ')'}).Split(',');
        if(result.Length == 2){
            if(float.TryParse(result[0], out float x) && float.TryParse(result[1], out float y)){

                data = new Vector2(x, y);
                return true;
            }
        }
        data = default;
        return false;
    }

    [CommandValueParser(typeof(Vector3), Alias = "v3")]
    public static bool TryParseVector3(string input, out object data){

        switch(input){
            case "up":
                data = Vector3.up;
                return true;
            case "down":
                data = Vector3.down;
                return true;
            case "left":
                data = Vector3.left;
                return true;
            case "right":
                data = Vector3.right;
                return true;
            case "forward":
                data = Vector3.forward;
                return true;
            case "back":
                data = Vector3.back;
                return true;
        }

        string[] result= input.Trim(new char[]{'(', ')'}).Split(',');
        if(result.Length == 3){
            if( float.TryParse(result[0], out float x) && 
                float.TryParse(result[1], out float y) && 
                float.TryParse(result[2], out float z)){

                data = new Vector3(x, y, z);
                return true;
            }
        }
        data = default;
        return false;
    }

}
