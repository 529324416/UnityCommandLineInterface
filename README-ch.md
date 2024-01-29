# UnityCommandLineInterface

[![openupm](https://img.shields.io/npm/v/com.redsaw.commandline?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.redsaw.commandline/)

[English Document](./README.md)

## 简介

这个项目是一个游戏内置的控制台项目,主要用于执行一些简短的命令或者函数,或者用于设置某些数值等

<div align=center>
<img src="./Res/screen-shot.png" style="zoom:80%" />
</div>

[![IMAGE ALT TEXT HERE](./Res/usage-part-1.png)](https://youtu.be/ejtAfCKEf8A)

## 特性：
- 使用简单，无需任何额外配置或者复杂学习流程
- 模块间高度解耦合，其内部组件可以单独使用
- 轻量级，无任何依赖
- 支持输入文本提示
- 支持自定义
- 全版本支持


## 如何使用

### 1.如何添加自定义命令

该项目使用特性和反射来定义和收集命令，你可以通过给函数挂载`Command`特性来定义你自己命令

```c#
[Command]
static void MyCommand(){
    UnityEngine.Debug.Log("hello world");
}
```

这样这个函数就会被识别为一个命令，并且你可以输入 *`MyCommand`* 来执行该函数，之间不需要任何多余的配置或者操作

<div align=center>
<img src="./Res/usage-part-1.png" style="zoom:80%" />
</div>

你可以设置命令的名称，描述和标签

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

### 2.如何添加自定义变量

控制台变量指的是可以通过`@`进行访问的静态变量，要注册一个控制台变量，你可以给目标静态属性或者静态字段挂载一个`CommandProperty`特性，它就会被识别为一个控制台变量

```C#
[CommandProperty]
public static Player player;
```

之后你可以在控制台直接对其进行访问，可访问的元素不仅是其变量本身，还包括了所有它内部的字段、属性和方法。如下所示:

```
@player.Jump()
@player.health = 100
@player.tag = "something"
```

并且其他需要`Player`类型参数的命令也可以直接对其进行访问
```
some_command @player
```

当我们对一个变量进行赋值的时候，如果该变量不存在于你定义的默认变量中，那么该系统会临时创建一个本地的变量去存储你设定的值，如下所示

```
@pos = @player.pos
print @pos          // 读取player的pos字段并存储于@pos中
@player.pos = @pos  // 重设位置
```

### 3.如何添加值解析器

在测试过程中我们肯定会需要很多值的设置，但是该系统并不是一个完整的编程语言，所以它无法定义完整的值输入，比如`@player.pos = xxx`这样的命令，我们假设`pos`是`UnityEngine.Vector3`类型，那么该如何将给定的字符串或者任何输入处理为对应的类型呢？

此时我们需要注册一个值解析器

```C#
[CommandValueParser(typeof(Vector3), Alias = "v3")]
public static bool ParseVector3(string input, out object data){
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
```
你需要指定将其解析为目标类型`Vector3`，并且可以选择为其设置一个别名`v3`，当然，具体的逻辑完全可以自己决定，比如你希望把`"up"`解析为`Vector3.Up`的话，可以做一个直接的判断
```C#
switch(input){
    case "up":
        data = Vector3.up;
        return true;
    /* ... */
}
```
此后当系统遇到需要`Vector3`类型的地方，它可能会尝试利用你定义的解析器来解析目标字符串
```
@player.pos = "1.0, 1.0, 1.0"
```

根据你的解析器的具体逻辑，你的赋值内容是可变的

```
@player.pos = '1. 1. 1.'
@player.pos = 'up'
@player.pos = 'pt_revival'
```

### 关于更多用法

当然由于它可以直接访问一个对象的子元素，所以它在某些情况下处理一些问题可能会变得有歧义，所以我们来看一些比较奇怪的用法：

*等待文档更新*



## 其他

### 适用的Unity版本

Unity 2018.03+

这个项目与Unity是高度解耦合的，所以它几乎可以用于所有版本的Unity，你可以为它定义自己的UI来使用，这个项目提供了默认的Unity实现，你可以学习或者自己改造。

### 特性支持：基础运算

该系统是为了能够进行方便的debug工作而设计的，所以它的定位并不是一个完整的编程语言，所以它现在还不支持基础的运算，比如数学运算，或者逻辑运算等。如果对该类特性有需求的话，可以在github提出，如果的确有必要的话，我会实现的。