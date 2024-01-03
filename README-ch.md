# UnityCommandLineInterface

[英文文档](./README.md)

这个项目是一个游戏内置的控制台项目，下面是项目的一些截图

<div align=center>
<img src="./Res/屏幕截图 2024-01-04 053723.png" style="zoom:80%" />
</div>

<div align=center>
<img src="./Res/屏幕截图 2024-01-04 045116.png" style="zoom:80%" />
</div>

它由以下三个部分组成：

- **命令系统**
- **控制台行为逻辑与接口定义**
- **控制台渲染器实现**

这三个部分是高度解耦合的，也就是控制台的行为逻辑与命令系统可以自由的拆解到其他的项目中。以下是关于三个部分的使用文档。

## 命令系统

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

如果你希望一些使用了额外参数的命令也可以被识别为命令的话，可以注册一个类型转换函数给命令系统，比如，你有一个使用了枚举类型参数的命令函数。

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