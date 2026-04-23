using SPTr.DeveloperConsole;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

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
        public class BindingInfo
        {
            public BindingInfo(Func<bool> binding, string cmdName, string cmdArguments)
            {
                this.binding = binding;
                this.cmdName = cmdName;
                this.cmdArguments = cmdArguments;
                this.isToggle = false;
                this._toggleCount = 0;
                this._toggleArguments = null;
            }

            public BindingInfo(Func<bool> binding, string cmdName, string[] toggleArguments)
            {
                this.binding = binding;
                this.cmdName = cmdName;
                this.cmdArguments = null;
                this.isToggle = true;
                this._toggleArguments = toggleArguments;
                this._toggleCount = 0;
            }

            public Func<bool> binding;
            public string cmdName;
            public string cmdArguments;

            public bool isToggle;
            private int _toggleCount;
            private string[] _toggleArguments;

            public string ConsumeToggleValue()
            {
                if (!this.isToggle || 
                    _toggleArguments == null || 
                    _toggleArguments.Length == 0)
                    return cmdArguments;

                return _toggleArguments[_toggleCount++ % _toggleArguments.Length];
            }
        }

        public static ConsoleCommand bind = new ConsoleCommand("bind",
            (arguments) =>
            {
                string replaceText = arguments.Replace("\"", "");
                string[] splitText = replaceText.Split(' ');
#if ENABLE_INPUT_SYSTEM
                bool isValidKey = Enum.TryParse(splitText[0], true, out Key currentKey);
#else
                bool isValidKey = Enum.TryParse(splitText[0], true, out KeyCode currentKey);
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
                    PrintErrorMSG("키 값이 올바르지 않습니다.");
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
            , "사용자 입력에 명령어를 할당합니다.  \n- bind <사용자 키> <콘솔명령어>");

        public static ConsoleCommand bindtoggle = new ConsoleCommand("bindtoggle", 
            (string arguments) =>
            {
                string[] splitText = Tokenize(arguments);

                // g, flag_cheats, 1 -> split[0], split[1], split[2]

#if ENABLE_INPUT_SYSTEM
                bool isValidKey = Enum.TryParse(splitText[0], true, out Key currentKey);
#else
                bool isValidKey = Enum.TryParse(splitText[0], true, out KeyCode currentKey);
#endif
                bool isNoCMD = splitText.Length == 1;
                if (isNoCMD)
                {
                    PrintErrorMSG("명령어를 명시하지 않았습니다. \n바인딩된 사용자 키의 명령어가 무엇인지 파악하려면 bind <사용자 키>를 입력하세요.");
                    return;
                }

                if (isValidKey)
                {
                    // CMD 정보 파악
                    bool isValidCMD = DevConsole.TryFindCommand(splitText[1], out var cmd);

                    if (!isValidCMD)
                    {
                        PrintErrorMSG($"[{splitText[1]}] - 알 수 없는 명령어 입니다.");
                        return;
                    }

                    // case 1 : 토글 값이 명시되지 않음
                    // -> bool 명령어 인가?
                    if (splitText.Length == 2 && 
                        cmd.Type == DevConObjType.isBool && 
                        cmd.TrackedValue != null &&
                        bool.TryParse(cmd.TrackedValue.Invoke() , out bool trackedResult))
                    {
                        string[] autoToggleValues = trackedResult
                            ? new[] { "false", "true" }
                            : new[] { "true", "false" };

                        if (_bindingDic.ContainsKey(currentKey))
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
                                    autoToggleValues
                                    )
                             );
                        return;
                    }
                    else if(splitText.Length == 2)
                    {
                        PrintErrorMSG($"이 종류의 명령어는 bindtoggle 시 2개 이상의 명시된 값이 필요합니다.");
                        return;
                    }

                    // case 2 : 올바른 형태로 되어 있음
                    // -> 토글 값 2개 이상인가?
                    if (splitText.Length >= 4)
                    {
                        if (_bindingDic.ContainsKey(currentKey))
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
                                    splitText[2..]
                                    )
                             );
                        return;
                    }
                    else if(splitText.Length == 3)
                    {
                        PrintErrorMSG("이 종류의 명령어는 bindtoggle 시 2개 이상의 명시된 값이 필요합니다.");
                        return;
                    }

                    PrintErrorMSG("올바르지 않은 인자 혹은 명령어입니다.");
                }
                else
                {
                    //fallback 아님, 강제 종료
                    PrintErrorMSG("키 값이 올바르지 않습니다.");
                    return;
                }

            },  "사용자 입력에 toggle 형식의 명령어를 할당합니다. \n- bindtoggle <사용자 키> <bool 콘솔명령어> \n- bindtoggle <사용자 키> <콘솔명령어> <값1> <값2> ... <값N>");


        public static void InvokeBinding()
        {
            foreach (var info in BindingList)
            {
                if (!info.binding.Invoke()) continue;

                if (DevConsole.TryFindCommand(info.cmdName, out ConsoleCommand cmd))
                {
                    string param = info.isToggle
                        ? info.ConsumeToggleValue()
                        : info.cmdArguments;

                    bool validExecute = DevConsole.ExecuteCommand(cmd, param, out string errorMSG);

                    if (!validExecute)
                        PrintErrorMSG(errorMSG);
                    else
                        OnExcuteBinding?.Invoke();
                }
                else
                {
                    PrintErrorMSG($"{info.cmdName}은(는) 존재하지 않는 명령어입니다");
                }
            }
        }

        private static readonly Regex _tokenizeRegex = new Regex(@"""([^""]*)""|(\S+)");

        private static string[] Tokenize(string arguments)
        {
            var matches = _tokenizeRegex.Matches(arguments);
            var result = new string[matches.Count];

            for (int i = 0; i < matches.Count; i++)
            {
                // 따옴표로 묶인 그룹 -> Group[1], 일반 토큰 -> Group[2]
                result[i] = matches[i].Groups[1].Success
                    ? matches[i].Groups[1].Value   
                    : matches[i].Groups[2].Value;  
            }

            return result;
        }

        private static void PrintErrorMSG(string msg)
        {
            Debug.Log($"<color={COLOR_ERROR}>{msg}</color>");
        }
    }
}
