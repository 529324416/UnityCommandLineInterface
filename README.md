# UnityCommandLineInterface

[Chinese Document](./README-ch.md)

The project is an inner game command line system, here are some screenshots of it.

<div align=center>
<img src="./Res/屏幕截图 2024-01-04 053723.png" style="zoom:80%" />
</div>

<div align=center>
<img src="./Res/屏幕截图 2024-01-04 045116.png" style="zoom:80%" />
</div>

It consists of three parts:

- 1.CommandSystem
- 2.Console and other components interfaces
- 3.Console Renderer's Unity implementation

These three parts are highly decoupled, it means the Console definitions and CommandSystem can be used in other projects individually, here are some instructions for usage.

## CommandSystem

### how to define custom commands

The project use Attribute and Reflection to define and collect commands, you can define your commands like this:

``````c#

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

``````

then you can use the command by inputting it into the console like this, there's no more configs or operations.

<div align=center>
<img src="./Res/屏幕截图 2024-01-04 064500.png" style="zoom:80%" />
</div>

### The parameters of Command methods

Not all static methods could be recognized as command, the method's parameter list can only use the types defined below:

```
Int
String
Float
Double
Bool
Char
Byte
Short
Long
UShort
UInt
ULong
Decimal
SByte
```
But if you want some methods with other types of parameters to be recognized as well, you can register a parser function to the CommandSystem. for example, you have a command method with `Enum` type parameter like this:

``````c#
[Command]
static void TestCommand(MyEnum value){
	// do something..
}
``````
In the default case, it won't be recognized as a command. so you can register a parser function with signature of this:

``````c#
bool ParseFunc(string args, out object value);
``````

The register way is to add an Attribute named `CommandParameterParser` to your function like this:


``````c#
[CommandParameterParser(typeof(MyEnum))]
static bool MyEnumParser(string value, out object data){

    if(System.Enum.TryParse<MyEnum>(value, out var result)){
        data = result;
        return true;
    }
    data = null;
    return false;
}
``````

Then you can use this command now.

<div align=center>
<img src="./Res/屏幕截图 2024-01-04 064808.png" style="zoom:80%" />
</div>

## Custom the console 

### define custom Input

wait for editing

### custom ui theme

wait for editing