using SPTr.DeveloperConsole;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SPTr.CMD
{
    public static class BindCMD
    {
        public const string COLOR_ERROR = "#F78181";
        public const string COLOR_INFO = "#F7BE81";
        public const string COLOR_VALUE = "#F5DA81";

        public static IReadOnlyCollection<BindingInfo> BindingList => _bindingDic.Values;

        public static Action OnExcuteBinding;

#if ENABLE_INPUT_SYSTEM
        private static Dictionary<Key ,BindingInfo> _bindingDic = new Dictionary<Key, BindingInfo>();
#else
        private static Dictionary<KeyCode ,BindingInfo> _bindingDic = new Dictionary<KeyCode, BindingInfo>();
#endif
        public struct BindingInfo
        {
            public BindingInfo(Func<bool> binding, string cmdName, string cmdArguments)
            {
                this.binding = binding;
                this.cmdName = cmdName;
                this.cmdArguments = cmdArguments;
            }

            public Func<bool> binding;
            public string cmdName;
            public string cmdArguments;

        }

        public static ConsoleCommand bind = new ConsoleCommand("bind",
            (string arguments) =>
            {
                string replaceText = arguments.Replace("\"", "");
                string[] splitText = replaceText.Split(' ');
#if ENABLE_INPUT_SYSTEM
                bool isValidKey = TryConvertStringToKey(splitText[0], out Key currentKey);
#else
                bool isValidKey = TryConvertStringToKey(splitText[0], out KeyCode currentKey);
#endif
                bool isCheckBinding = splitText.Length < 2 || (splitText[1] == string.Empty);
                bool isVoidAction = splitText.Length < 3;

                if (isValidKey && !isCheckBinding)
                {
                    if(_bindingDic.ContainsKey(currentKey))
                    {
                        _bindingDic.Remove(currentKey);
                    }
                    _bindingDic.Add
                         (
                             currentKey,
                             new BindingInfo(
#if ENABLE_INPUT_SYSTEM
                                 () => { return Keyboard.current[currentKey].wasPressedThisFrame; },
#else
                                 () => { return Input.GetKeyDown(currentKey); },
#endif
                                 splitText[1],
                                 isVoidAction ? "" : string.Join(' ', splitText[2..])
                                 )
                         );
                }
                else if (!isValidKey)
                {
                    PrintErrorMSG("키값이 올바르지 않습니다.");
                }
                else if (isCheckBinding)
                {
                    if (_bindingDic.ContainsKey(currentKey))
                    {
                        if (arguments.IndexOf('"') != -1)
                        {
                            _bindingDic.Remove(currentKey);
                            Debug.Log($"{currentKey} = \"{splitText[1]}\"");
                        }
                        else
                            Debug.Log($"{currentKey} = {_bindingDic[currentKey].cmdName} {_bindingDic[currentKey].cmdArguments}");
                        
                    }
                        
                    else
                        if(arguments.IndexOf('"') != -1)
                            Debug.Log($"{currentKey} = \"{splitText[1]}\"");
                        else
                            Debug.Log($"{currentKey}키에 할당된 명령어가 없습니다.");
                }
            }
            , "사용자 입력에 명령어를 할당합니다.  \n- bind <사용자 키>, <콘솔명령어>");

        public static void InvokeBinding()
        {
            foreach (var info in BindingList)
            {
                if (info.binding.Invoke())
                {
                    if (DevConsole.TryFindCommand(info.cmdName, out ConsoleCommand cmd))
                    {
                        bool validExcute = DevConsole.ExecuteCommand(cmd, info.cmdArguments, out string errorMSG);

                        if(!validExcute)
                        {
                            PrintErrorMSG(errorMSG);
                        }
                        else
                        {
                            OnExcuteBinding?.Invoke();
                        }
                    }
                    else
                    {
                        PrintErrorMSG($"{info.cmdName}은(는) 존재하지 않는 명령어입니다");
                    }
                }
            }
        }

#if ENABLE_INPUT_SYSTEM
        public static bool TryConvertStringToKey(string keyString,out Key key)
        {
            return Enum.TryParse(keyString, true, out key);
        }
#else
        public static bool TryConvertStringToKey(string keyString,out KeyCode key)
        {
            return Enum.TryParse(keyString, true, out key);
        }
#endif
        private static void PrintErrorMSG(string msg)
        {
            Debug.Log($"<color={COLOR_ERROR}>{msg}</color>");
        }

        //public static void AddTestCmd()
        //{
        //    _bindingList.Add(new BinidingInfo
        //    {
        //        binding = () => { return Input.GetKeyDown(KeyCode.G); },
        //        cmdName = "rb_create_box",
        //        cmdArguments = "1 1 1"
        //    });

        //    foreach(var info in BindingList)
        //    {
        //        Debug.Log($"{info.binding.Invoke()},{info.cmdName},{info.cmdArguments}");
        //    }
        //}
    }
}
