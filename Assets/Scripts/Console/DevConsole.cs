using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DeveloperConsole
{

    [Flags]
    public enum ExecFlag
    {
        NONE = 0,
        CHEAT = 1,
        DEBUG = 2,
        CUSTOM = 4, //���ϴ� ��� �̸��� �����ؼ� ���
    }

    public enum DevConObjType
    {
        isVoid = 0,
        isBool = 1,
        isInt = 2,
        isFloat = 3,
        isString = 4,
    }

    public interface IDevConObj
    {
        public DevConObjType Type { get; }
        public string Name { get; }
        public string Description { get; }

        /// <summary>
        /// ���� �÷��״� �ϳ��� �ʱ�ȭ���Ѿ� �մϴ�.
        /// ex) flag = ExecFlag.CHEAT (O) , flag = ExecFlag.CHEAT | ExecFlag.CUSTOM (X)
        /// </summary>
        public ExecFlag Flag { get; }
    }

    public class ConsoleCommand : IDevConObj
    {
        #region Basic Info

        public DevConObjType Type => _type;
        public string Name => _name;
        public string Description => _description;
        public ExecFlag Flag => _flag;
        public string InitValue => _initValue;

        private DevConObjType _type;
        private string _name;
        private string _description;
        private ExecFlag _flag;
        private bool _firstInvoke = true;
        private string _initValue = null;

        #endregion

        #region Delegate

        public Action VoidAction { get; set; }
        public Action<bool> BoolAction { get; set; }
        public Action<int> IntAction { get; set; }
        public Action<float> FloatAction { get; set; }
        public Action<string> StringAction { get; set; }

        public Func<string> TrackedValue { get; private set; }

        #endregion

        #region Constructor

        public ConsoleCommand(string name, Action action, string description = "", ExecFlag execFlag = ExecFlag.NONE)
        {
            _type = DevConObjType.isVoid;
            _name = name;
            _description = description;
            _flag = execFlag;
            VoidAction += CheckIsFirstInvoke;
            VoidAction += action;
        }

        public ConsoleCommand(string name, Action<bool> action, string description = "", ExecFlag execFlag = ExecFlag.NONE)
        {
            _type = DevConObjType.isBool;
            _name = name;
            _description = description;
            _flag = execFlag;
            BoolAction += (value) => CheckIsFirstInvoke();
            BoolAction += action;
        }

        public ConsoleCommand(string name, Action<int> action, string description = "", ExecFlag execFlag = ExecFlag.NONE)
        {
            _type = DevConObjType.isInt;
            _name = name;
            _description = description;
            _flag = execFlag;
            IntAction += (value) => CheckIsFirstInvoke();
            IntAction += action;
        }

        public ConsoleCommand(string name, Action<float> action, string description = "", ExecFlag execFlag = ExecFlag.NONE)
        {
            _type = DevConObjType.isFloat;
            _name = name;
            _description = description;
            _flag = execFlag;
            FloatAction += (value) => CheckIsFirstInvoke();
            FloatAction += action;
        }

        public ConsoleCommand(string name, Action<string> action, string description = "", ExecFlag execFlag = ExecFlag.NONE)
        {
            _type = DevConObjType.isString;
            _name = name;
            _description = description;
            _flag = execFlag;
            StringAction += (value) => CheckIsFirstInvoke();
            StringAction += action;
        }

        public ConsoleCommand SetTrackingValue(Func<string> trakingValue = null)
        {
            if(_type == DevConObjType.isVoid)
            {
                Console.WriteLine("�Ű������� ���� �ָܼ�ɾ �������� ������ �� �����ϴ�.");
                return this;
            }

            TrackedValue = trakingValue;
            return this;
        }

        #endregion

        /// <summary>
        /// ��ɾ ���� ��ŵ�ϴ�. (�Ϲ� ����ڿ�)
        /// </summary>
        /// <param name="param"></param>
        public void Invoke(object param)
        {
            switch (_type)
            {
                case DevConObjType.isVoid:
                    VoidAction.Invoke();
                    break;
                case DevConObjType.isBool:
                    BoolAction.Invoke((bool)param);
                    break;
                case DevConObjType.isInt:
                    IntAction.Invoke((int)param);
                    break;
                case DevConObjType.isFloat:
                    FloatAction.Invoke((float)param);
                    break;
                case DevConObjType.isString:
                    StringAction.Invoke((string)param);
                    break;
            }
        }

        private void CheckIsFirstInvoke()
        {
            if (_firstInvoke)
            {
                _firstInvoke = false;
                _initValue = TrackedValue?.Invoke().ToString();
            }
        }
    }

    /// <summary>
    /// ���� ��� ������ ���� ��� �Լ��� �ܼ� ��ɾ�� ����ϴ�. �̸��� ������ũ ���̽��� �ۼ��ؾ� �մϴ� -> "cv_player_speed"
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field)]
    public class ConCmd : Attribute
    {
        public string name;
        public string description;
        public ExecFlag execflag;
        public string trackingValue;

        public ConCmd(string name, string description = "", ExecFlag execflag = ExecFlag.NONE, string trackingValue = null)
        {
            this.name = name;
            this.description = description;
            this.execflag = execflag;
            this.trackingValue = trackingValue;
        }
    }

    public static class DevConsole
    {
        public static Action<ConsoleCommand> OnCmdAdded;
        public static Action<ConsoleCommand> OnCmdRemoved;
        public static Action<ConsoleCommand> OnCmdExecuted;

        public static IReadOnlyDictionary<string, ConsoleCommand> consoleCmds => _consoleCmds;
        private static Dictionary<string, ConsoleCommand> _consoleCmds = new Dictionary<string, ConsoleCommand>();

        #region Console Command Management 
        public static void AddCommand(ConsoleCommand obj)
        {
            try
            {
                _consoleCmds.Add(obj.Name, obj);
            }
            catch (ArgumentException e)
            {
                System.Console.WriteLine($"{e} : �ָܼ�ɾ �̹� �����մϴ�.");
                return;
            }

            OnCmdAdded?.Invoke(obj);
        }

        public static void RemoveCommand(ConsoleCommand obj)
        {
            try
            {
                _consoleCmds.Remove(obj.Name);
            }
            catch (KeyNotFoundException e)
            {
                System.Console.WriteLine($"{e} : �ָܼ�ɾ �������� �ʽ��ϴ�.");
                return;
            }

            OnCmdRemoved?.Invoke(obj);
        }

        /// <summary>
        /// ���� ������� ��� ConsoleCommand�� DevConsole�� �߰��մϴ�. useConCmd �Ű������� ��Ʈ����Ʈ ��ɾ�(ConCmd) �߰� ���θ� �����մϴ�. (���÷��� ���)
        /// </summary>
        public static void AddAllCommandInAssembly(bool useConCmd = true)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type t in assembly.GetTypes())
                {
                    FieldInfo[] fields = t.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

                    foreach (FieldInfo f in fields)
                    {
                        if (f.FieldType == typeof(ConsoleCommand) && !consoleCmds.ContainsKey(f.Name))
                        {
                            AddCommand((ConsoleCommand)f.GetValue(null));
                        }

                        else if (useConCmd && Attribute.GetCustomAttribute(f, typeof(ConCmd)) is ConCmd cmdInfo && !consoleCmds.ContainsKey(cmdInfo.name))
                        {
                            AddCommandWithConCmd(f, cmdInfo);
                        }
                    }

                    if (!useConCmd)
                        continue;

                    MethodInfo[] methods = t.GetMethods(BindingFlags.NonPublic| BindingFlags.Public | BindingFlags.Static);

                    foreach(MethodInfo m in methods)
                    {
                        if (Attribute.GetCustomAttribute(m, typeof(ConCmd)) is ConCmd cmdInfo && !consoleCmds.ContainsKey(cmdInfo.name))
                        {
                            AddCommandWithConCmd(m, cmdInfo);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ConCmd�� �ָܼ�ɾ�� �ٲپ� DevConsole�� �߰��մϴ�. (���÷��� ���)
        /// </summary>
        /// <param name="member"></param>
        /// <param name="cmdInfo"></param>
        private static void AddCommandWithConCmd(MemberInfo member, ConCmd cmdInfo)
        {
            bool validCommand = true;
            ConsoleCommand command = null;
            string typeError = "[����] : �������� �ʴ� ������ �ܼ� ��ɾ�� �����߽��ϴ�";

            if (member.MemberType == MemberTypes.Field)
            {
                FieldInfo field = (FieldInfo)member;
                switch (Type.GetTypeCode(field.FieldType))
                {
                    case TypeCode.Boolean:
                        command = new ConsoleCommand(cmdInfo.name, (bool value) => field.SetValue(null, value), cmdInfo.description, cmdInfo.execflag);
                        command.SetTrackingValue(() => field.GetValue(null).ToString());
                        break;
                    case TypeCode.Int32:
                        command = new ConsoleCommand(cmdInfo.name, (int value) => field.SetValue(null, value), cmdInfo.description, cmdInfo.execflag);
                        command.SetTrackingValue(() => field.GetValue(null).ToString());
                        break;
                    case TypeCode.Single:
                        command = new ConsoleCommand(cmdInfo.name, (float value) => field.SetValue(null, value), cmdInfo.description, cmdInfo.execflag);
                        command.SetTrackingValue(() => field.GetValue(null).ToString());
                        break;
                    case TypeCode.String:
                        command = new ConsoleCommand(cmdInfo.name, (string value) => field.SetValue(null, value), cmdInfo.description, cmdInfo.execflag);
                        command.SetTrackingValue(() => field.GetValue(null).ToString());
                        break;
                    default:
                        validCommand = false;
                        Console.WriteLine(typeError);
                        break;
                }
            }
            else if (member.MemberType == MemberTypes.Method)
            {
                MethodInfo method = (MethodInfo)member;

                ParameterInfo[] param = method.GetParameters();

                if (param.Length > 1)
                {
                    return;
                }

                else if(param.Length == 0)
                {
                    command = new ConsoleCommand(cmdInfo.name, () => method.Invoke(null, null), cmdInfo.description, cmdInfo.execflag);
                }
                else
                {
                    switch (Type.GetTypeCode(param[0]?.ParameterType))
                    {
                        case TypeCode.Boolean:
                            command = new ConsoleCommand(cmdInfo.name, (bool value) => method.Invoke(null, new object[] { value }), cmdInfo.description, cmdInfo.execflag);
                            break;
                        case TypeCode.Int32:
                            command = new ConsoleCommand(cmdInfo.name, (int value) => method.Invoke(null, new object[] { value }), cmdInfo.description, cmdInfo.execflag);
                            break;
                        case TypeCode.Single:
                            command = new ConsoleCommand(cmdInfo.name, (float value) => method.Invoke(null, new object[] { value }), cmdInfo.description, cmdInfo.execflag);
                            break;
                        case TypeCode.String:
                            command = new ConsoleCommand(cmdInfo.name, (string value) => method.Invoke(null, new object[] { value }), cmdInfo.description, cmdInfo.execflag);
                            break;
                        default:
                            validCommand = false;
                            Console.WriteLine(typeError);
                            break;
                    }
                }

                if(validCommand 
                    && cmdInfo.trackingValue != null 
                    && cmdInfo.trackingValue != string.Empty)
                {
                    Type classInfo = member.DeclaringType;

                    foreach(var field in classInfo.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static))
                    {
                        if (field.Name == cmdInfo.trackingValue)
                            command.SetTrackingValue(() => field.GetValue(null).ToString());
                    }
                }
            }
            else
                validCommand = false;

            if (validCommand)
                AddCommand(command);
        }
        #endregion

        #region Using Console Command
        public static ConsoleCommand FindCommand(string name)
        {
            ConsoleCommand cmd;

            try
            {
                cmd = _consoleCmds[name];
            }
            catch (KeyNotFoundException e)
            {
                System.Console.WriteLine($"{e} : �ָܼ�ɾ �������� �ʽ��ϴ�.");
                return null;
            }

            return cmd;
        }
        public static bool TryFindCommand(string name, out ConsoleCommand cmd)
        {
            cmd = FindCommand(name);

            return cmd != null;
        }
        public static bool ExcuteCCmd(ConsoleCommand cmd, string param)
        {
            if (cmd.Flag != ExecFlag.NONE && (cmd.Flag & _currentFlags) == 0)
            {
                System.Console.WriteLine($"{cmd.Flag} �÷��װ� �������� �ʽ��ϴ�.");
                return false;
            }

            bool boolParam = false;
            int intParam = 0;
            float floatParam = 0.0f;

            string errorMSG = null;
            switch (cmd.Type)
            {
                case DevConObjType.isVoid:
                    cmd.VoidAction.Invoke();
                    break;
                case DevConObjType.isBool:
                    if (bool.TryParse(param, out boolParam))
                    {
                        cmd.BoolAction.Invoke(boolParam);
                    }
                    else if (int.TryParse(param, out intParam))
                    {
                        cmd.BoolAction.Invoke(intParam > 0);
                    }
                    else
                    {
                        errorMSG = "���ڸ� 0,1 Ȥ�� true,false�� ������ּ���";
                    }
                    break;
                case DevConObjType.isInt:
                    if (int.TryParse(param, out intParam))
                    {
                        cmd.IntAction.Invoke(intParam);
                    }
                    else
                    {
                        errorMSG = "���ڿ� ���ڸ� ������ּ���";
                    }
                    break;
                case DevConObjType.isFloat:
                    if (float.TryParse(param, out floatParam))
                    {
                        cmd.FloatAction.Invoke(floatParam);
                    }
                    else
                    {
                        errorMSG = "���ڿ� ���ڸ� ������ּ���";
                    }
                    break;
                case DevConObjType.isString:
                    if (param != "")
                    {
                        cmd.StringAction.Invoke(param);
                    }
                    else
                    {
                        errorMSG = "���ڸ� �Է����ּ���";
                    }
                    break;
            }
            if (errorMSG != null)
            {
                System.Console.WriteLine($"��ɾ ������� �ʾҽ��ϴ�. {errorMSG}");
                return false;
            }
            else
            {
                OnCmdExecuted?.Invoke(cmd);
                return true;
            }
        }
        #endregion

        #region ExecFlag

        public static ExecFlag CurrentFlags => _currentFlags;

        private static ExecFlag _currentFlags = ExecFlag.NONE | ExecFlag.DEBUG;

        private static ConsoleCommand info_execflags = new ConsoleCommand("info_execflags", () => { },
            $"���� �÷��� ���� :: \n {ExecFlag.NONE} : �÷��� ����, �÷��� ���� �ֺܼ����� �ܼ�Ŀ�ǵ带 ���� �з��� �⺻ �÷��� \n {ExecFlag.CHEAT} : ġƮ�� �÷��� \n {ExecFlag.DEBUG} : ����� ������ �÷��� \n {ExecFlag.CUSTOM} : ������ Ŀ���� �÷���");

        private static ConsoleCommand flag_cheats = new ConsoleCommand("flag_cheats",
        (bool value) =>
        {
            if (value)
                _currentFlags |= ExecFlag.CHEAT;
            else
                _currentFlags &= ~ExecFlag.CHEAT;
        },
        $"{ExecFlag.CHEAT}�÷����� ���� �����մϴ�.").SetTrackingValue(
            () => ((_currentFlags & ExecFlag.CHEAT) != 0).ToString());

        #endregion
    }



}