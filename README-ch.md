# UnityCommandLineInterface

[英文文档](./README.md)

## 简介

这个项目是一个游戏内置的控制台项目，下面是项目的一些截图

<div align=center>
<img src="./Res/屏幕截图 2024-01-04 053723.png" style="zoom:80%" />
</div>

它由以下三个部分组成：

- **命令系统**
- **控制台行为逻辑与接口定义**
- **控制台渲染器实现**

这三个部分是高度解耦合的，也就是控制台的行为逻辑与命令系统可以自由的拆解到其他的项目中。以下是关于三个部分的使用文档。

## 如何使用

### 如何扩展自定义命令

项目通过特性和反射来定义命令，你可以通过定义静态函数来表示一个命令，如下所示：

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

[Command("command_with_args")]
static void CommandWithArgs(int a, int b){
    UnityEngine.Debug.Log(a + b);
}

[Command("command_with_tag", Tag = "disable_for_user")]
static void CommandWithTag(){
    UnityEngine.Debug.Log("command is disabled for user");
}

[Command("+8123*(&)  ..dsfm")]
static void WiredCommand(){
    
}

``````
之后你就可以通过命令行找到这个命令,之间不再需要额外的操作或者配置,如下图所示:

<div align=center>
<img src="./Res/屏幕截图 2024-01-04 064500.png" style="zoom:80%" />
</div>

### 命令函数的参数

不是所有的静态函数都可以被识别为命令，这需要目标函数的参数列表满足类型要求，它支持所有采用基础类型数据作为参数的静态函数，默认类型如下：

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

事实上这些类型已经足够使用了，但是如果你希望一些使用了额外参数的命令也可以被识别为命令的话，可以注册一个类型转换函数给命令系统，比如，你有一个使用了枚举类型参数的命令函数。

``````c#
[Command("test_command2")]
static void TestCommand(MyEnum value){
	// do something..
}
``````

由于`MyEnum`类型不属于默认支持的类型，所以默认情况下它不会被识别为命令，但是你可以通过注册一个解析函数使其可以被识别。解析函数的签名如下：

``````c#
bool ParseFunc(string args, out object value);
``````

注册方法是给你的解析函数挂载特性：`CommandParameterParser`

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

之后你就可以使用这个命令了，如下图所示：

<div align=center>
<img src="./Res/屏幕截图 2024-01-04 064808.png" style="zoom:80%" />
</div>

## 自定义命令行系统

### 自定义输入

等待文档的完善

### 自定义外观

等待文档的完善


## TODO List

- [x] **添加命令标签 v0.11** *@2024/01/06*
      <br> *现在你可以给命令添加标签，用于标签给命令分组用于实现一些特殊的限制，或者测试约束*
- [x] **添加命令查询缓存 v0.11** *@2024/01/06*
      <br> *现在查询命令会有最近20条查询记录的缓存，用一点空间换取了更高效的查询速度*
- [x] **支持接受Unity的Debug.Log输出 v0.11** *@2024/01/06*
      <br> *控制台提供了一个开关项用于选择是否监听UnityEngine.Debug.Log，打开时，该函数的输出结果会同时输出到控制台中，从而使你不用改变当前游戏的Debug命令*
- [ ] **重构控制台的封装结构 v0.12**
- [ ] **生成日志文件 v0.13**
- [ ] **输出过滤 v0.13**
- [ ] **添加更多的控制台渲染器行为逻辑** v0.2