# UnityCommandLineInterface

<a href="https://openupm.com/packages/com.redsaw.commandline/"><img src="https://img.shields.io/npm/v/com.redsaw.commandline?label=openupm&amp;registry_uri=https://package.openupm.com" /></a>

[Chinese Document](./README-ch.md)

## Summary

The project is an inner game command line console. it can run as a part of your game and provide an inner game debug and error log function. it just receives `UnityEngine.Debug.Log` method output so you have no need to change log point.

<div align=center>
<img src="./Res/屏幕截图 2024-01-04 053723.png" style="zoom:80%" />
</div>

<div align=center>
<img src="./Res/屏幕截图 2024-01-12 173800.png" style="zoom:80%" />
</div>

## Usage

### how to add custom commands

The project use Attribute and Reflection to define and collect commands, you can define your commands like this:

```c#
[Command]
static void MyCommand(){
    UnityEngine.Debug.Log("hello world");
}
```

then it could be recognized as a command, and you can input "*MyCommand*" to call it on command line console. there's no more configures or operations.

<div align=center>
<img src="./Res/usage-part-1.png" style="zoom:80%" />
</div>

you can set name, description, tag of command like this:

```c#

[Command("my_command")]
static void DefinedCommandName(){
    UnityEngine.Debug.Log("hello world");
}

[Command("with_desc", Desc = "add some descriptions here")]
static void AddSomeDescriptions(){
    UnityEngine.Debug.Log("hello world");
}

[Command("with_tag", Tag = "disable_for_user")]
static void CommandWithTag(){
    UnityEngine.Debug.Log("command is disabled for user");
}

[Command("with_args")]
static void Add(int a, int b){
    UnityEngine.Debug.Log(a + b);
}

```

Not all static methods could be recognized as command, the method's parameter list can only use the types defined below. 
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

Actually, these types are sufficient for use in most cases. But if you want some methods with other types of parameters to be recognized as well, you can register a parser function to the CommandSystem. for example, you have a command method with `Enum` type parameter like this:

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

this could make it easier to do some debug or testing work, for example, your debug method need a parameter like Player or Enemy, then you can convert it from string input by your custom parser function, looks like below:

```c#

public class GameEntity{/* some code here..*/}

[Command("handle_game_entity")]
static void DebugCommand(GameEntity entity){
    // do something to entity..
}

[CommandParameterParser(typeof(GameEntity))]
static bool GameEntityParser(string input, out object data){
    /* fake code, just for example */

    switch(input){
        case "A":
        case "B":
        case "C"
            data = gameEntities.GetByName(input);
            return true;
        default:
            data = null;
            return false;
    }
}

```

then you can input `handle_game_entity 'A'` to do something to the entity A.

## Custom the console 

### define custom Input

wait for editing

### custom ui theme

wait for editing

## Other

### Applicable Unity Versions

this project is highly decoupled with UnityEngine, so it can use in any version of UnityEngine.
you can define custom UI system for console, and this project provide default implementation for you can 
learn or optimized by yourself.

## TODO List

- [x] **Add Command Tag Defination v0.11** *@2024/01/06*
      <br> *you can add tag for your command so your can group them by tag, to hide or constraint some command usage*
- [x] **Add Command Query Cache v0.11** *@2024/01/06*
      <br> *command add query cache ability to store command query, use some space to bring more efficient query speed.*
- [x] **Receive Unity's Debug Information v0.11** *@2024/01/06*
      <br> *console supported to receive message from `UnityEngine.Debug.Log`, you can just use Unity's Debug Function, then the console would output the message as well*
- [ ] ~~**Refactoring Wrapper of `GameConsole` v0.12**~~
- [ ] ~~**Generate log file of console ouput v0.13**~~
      <br> Unity has its own log generator
- [ ] **Support to Filter console output v0.13**
- [ ] **Add More Console Renderer Features v0.2** 