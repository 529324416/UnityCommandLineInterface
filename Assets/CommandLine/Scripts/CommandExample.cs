using RedSaw.CommandLineInterface;

public static class CommandExample
{
    [Command]
    static void MyCommand(){
        UnityEngine.Debug.Log("hello world");
    }
}
