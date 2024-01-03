using RedSaw.CommandLineInterface;

public static class CommandDef{
    
    [Command]
    static void MyCommand(){
        UnityEngine.Debug.Log("hello world");
    }

    [Command("test_command")]
    static void DefinedCommandName(){
        UnityEngine.Debug.Log("hello world");
    }

    [Command("test_command2", Desc = "add some descriptions here")]
    static void AddSomeDescriptions(){
        UnityEngine.Debug.Log("hello world");
    }

    [CommandParameterParser(typeof(MyEnum))]
    static bool MyEnumParser(string value, out object data){

        if(System.Enum.TryParse<MyEnum>(value, out var result)){
            data = result;
            return true;
        }
        data = null;
        return false;
    }

    [Command]
    static void TestCommand(MyEnum value){
        UnityEngine.Debug.Log(value.ToString());
    }
}

public enum MyEnum{
    A,
    B,
    C
}
