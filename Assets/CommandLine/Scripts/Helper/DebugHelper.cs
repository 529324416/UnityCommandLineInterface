using System;

namespace RedSaw.CommandLineInterface{

    /// <summary>
    /// mark one field or property is debug info, when we use CommandHelper.Debug(), we can get all debug info
    /// include parent object's debug info
    /// </summary>
    [AttributeUsage(AttributeTargets.Field|AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class DebugInfoAttribute: Attribute{

        public string Key{ get; protected set; }
        public string Color{ get; set; }

        public DebugInfoAttribute(string key){
            
            this.Key = key;
            this.Color = "#fffde3";
        }
        public DebugInfoAttribute(){

            this.Key = string.Empty;
            this.Color = "#fffde3";
        }
    }

    /// <summary>
    /// mark one element is container of debug info
    /// </summary>
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
    public class DebugObjectAttribute: Attribute{}
}