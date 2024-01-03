using RedSaw.CommandLineInterface;

public static class CommandDef{
    
    [Command("test_command", Desc = "just for testing")]
	static void MyCommand(){
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

    [Command("test_command2")]
    static void MyCommand2(MyEnum value){
        UnityEngine.Debug.Log(value.ToString());
    }
}

public enum MyEnum{
    A,
    B,
    C
}
