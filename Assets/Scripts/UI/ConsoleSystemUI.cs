using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using SPTr.CustomCollections;
using SPTr.DeveloperConsole;
using System;
using Unity.VisualScripting;
using System.Collections;
using System.IO;
using SPTr.CMD;
using System.Text.RegularExpressions;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;



#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SPTr.UI
{
    public class ConsoleSystemUI : MonoBehaviour
    {
        public static ConsoleSystemUI instance;

        [Header("UI 요소")]
        public RectTransform ConsoleWindow;
        public InputField TextField;
        public InputField ConsoleLog;
        public Text SuggestionTexts;
        public Text CurrentSuggestionText;
        public Image SuggestionsPanel;
        public TrieTree SuggestionsTree;

        [Header("이벤트")]
        public UnityEvent OnWindowFocused;          // 콘솔 창이 활성화될 때 발생하는 이벤트
        public UnityEvent OnWindowUnfocused;        // 콘솔 창이 비활성화될 때 발생하는 이벤트

        private const int MAX_SUGGETIONS_COUNT = 15;

        private const string COLOR_ERROR = "#F78181";
        private const string COLOR_INFO = "#F7BE81";
        private const string COLOR_VALUE = "#F5DA81";

        private StringBuilder _logBuilder = new StringBuilder();
        private const int MAX_LOG_LENGTH = 15000;
        private bool _isLogDirty = false;

        private List<string> _lastInputText = new List<string>();
        private List<string> _suggestions = new List<string>();
        private StringBuilder _sb = new StringBuilder();

        private Action<ConsoleMessageType, List<string>> _printMSG;

        public static ConsoleCommand echo = new ConsoleCommand("echo",
            (string args) =>
            {
                Debug.Log(args);
            }, "콘솔에 문자열을 출력합니다.");

        public static ConsoleCommand help = new ConsoleCommand("help",
            (string args) =>
            {
                var split = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (split.Length > 1)
                {
                    Debug.Log($"<color={COLOR_ERROR}>인자는 하나만 입력해주세요</color>");
                    return;
                }

                var cmd = DevConsole.FindCommand(split[0]);
                if (cmd != null)
                {
                    if (cmd.Description == null || cmd.Description.Length <= 1)
                    {
                        Debug.Log($"<color={COLOR_ERROR}>명령어의 설명이 존재하지 않습니다.</color>");
                        return;
                    }

                    Debug.Log($"<color={COLOR_INFO}>{(cmd.Description[0] != '/' ? cmd.Description : cmd.Description[1..])}</color>");
                }
                else
                {
                    Debug.Log($"<color={COLOR_ERROR}>해당하는 이름의 콘솔 명령어가 존재하지 않습니다.</color>");
                }
            }, "콘솔 명령어의 설명을 출력합니다. \n- help <콘솔 명령어>");

        public static ConsoleCommand clear = new ConsoleCommand("clear", () =>
        {
            instance._logBuilder.Clear();
            instance.ConsoleLog.text = "";
            instance._lastInputText.Clear();
        }, "/콘솔 로그를 전부 지웁니다.");

        public void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(this);

                instance.SuggestionsTree = new TrieTree();

                DevConsole.OnCmdAdded += AddToSuggestionTree;
                DevConsole.OnCmdRemoved += RemoveToSuggestionTree;
                Application.logMessageReceived += HandleLog;

                DevConsole.AddAllCommandInAssembly();

                BindCMD.LoadBindingCfg();
                BindCMD.OnExcuteBinding += UpdateSuggestions;

                ExecCMD.ExecuteAutoExec();

                _printMSG = PrintConsoleMessage;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void Start()
        {
            ConsoleWindow.gameObject.SetActive(false);
        }

        public void Update()
        {
            // 콘솔 창의 활성상태를 보존합니다. 이후 포커스 이벤트(OnWindowFocused, OnWindowUnfocused)를 발생시킬 때 사용됩니다.
            bool lastActiveState = ConsoleWindow.gameObject.activeSelf;

            // 컴파일 분기 - 인풋 시스템과 레거시 인풋 매니저에 따라 입력 처리를 다르게 합니다.
#if ENABLE_INPUT_SYSTEM
            if (ConsoleWindow.gameObject.activeSelf && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                ExcuteConsoleText();
                TextField.onSubmit.Invoke("");
            }
            if (Keyboard.current.backquoteKey.wasPressedThisFrame)
            {
                ConsoleWindow.gameObject.SetActive(!ConsoleWindow.gameObject.activeSelf);
            }
            if (TextField.isFocused && CurrentSuggestionText.text != "" && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                TextField.text = CurrentSuggestionText.text;
                TextField.caretPosition = CurrentSuggestionText.text.Length;
                UpdateSuggestions();
            }
#else
            if (ConsoleWindow.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Return))
            {
                ExcuteConsoleText();
            }
            if(Input.GetKeyDown(KeyCode.BackQuote))
            {
                ConsoleWindow.gameObject.SetActive(!ConsoleWindow.gameObject.activeSelf);
            }
            if (TextField.isFocused && CurrentSuggestionText.text != "" && Input.GetKeyDown(KeyCode.Tab))
            {
                TextField.text = CurrentSuggestionText.text;
                TextField.caretPosition = CurrentSuggestionText.text.Length;
                UpdateSuggestions();
            }
#endif
            // 콘솔 창의 포커스 이벤트를 처리합니다. 활성 상태가 변경된 경우에만 이벤트를 발생시킵니다.
            if (ConsoleWindow.gameObject.activeSelf != lastActiveState)
            {
                if (ConsoleWindow.gameObject.activeSelf)
                    OnWindowFocused?.Invoke();
                else
                    OnWindowUnfocused?.Invoke();
            }
        }

        public void LateUpdate()
        {
            if (!TextField.isFocused)
                BindCMD.InvokeBinding();

            // 프레임 끝에서 로그 변경사항이 있다면 한 번만 UI에 반영합니다.
            if (_isLogDirty && ConsoleLog != null)
            {
                ConsoleLog.text = _logBuilder.ToString();
                _isLogDirty = false;
            }
        }

        public void AddToSuggestionTree(ConsoleCommand cmd)
        {
            SuggestionsTree.Add(cmd.Name);
        }

        public void RemoveToSuggestionTree(ConsoleCommand cmd)
        {
            SuggestionsTree.Remove(cmd.Name);
        }

        public void HandleLog(string logText, string stackTrace, LogType type)
        {
            // 줄바꿈 처리
            if (instance._logBuilder.Length > 0)
                instance._logBuilder.Append("\n");

            // 새 로그 텍스트 추가
            instance._logBuilder.Append(logText);

            // 전체 길이가 MAX_LOG_LENGTH를 초과하는지 검사
            if (instance._logBuilder.Length > MAX_LOG_LENGTH)
            {
                // 초과한 만큼의 문자 수 계산
                int overflowCount = instance._logBuilder.Length - MAX_LOG_LENGTH;

                // StringBuilder의 Remove 기능을 이용해 앞부분을 자름 (정확히 문자 수 기준)
                instance._logBuilder.Remove(0, overflowCount);
            }

            // UI 업데이트가 필요하다고 표시
            instance._isLogDirty = true;
        }

        public void SelectSuggestionFromIndex(int idx)
        {
            TextField.text = _suggestions[idx] + " ";
            UpdateSuggestions();

            TextField.ActivateInputField();

            StartCoroutine(MoveCaretToEnd());
        }

        public IEnumerator MoveCaretToEnd()
        {
            Color selectionColor = TextField.selectionColor;
            TextField.selectionColor = new Color { a = 0 };
            yield return null;

            TextField.caretPosition = TextField.text.Length;
            TextField.selectionAnchorPosition = TextField.caretPosition;
            TextField.selectionFocusPosition = TextField.caretPosition;
            TextField.ForceLabelUpdate();
            TextField.selectionColor = selectionColor;
        }

        public void UpdateSuggestions()
        {
            _sb.Clear();

            if (TextField.text == "" || !SuggestionsTree.TryLoadSuggestions(ref _suggestions, TextField.text))
            {
                if (SuggestionsPanel.enabled)
                    SuggestionsPanel.enabled = false;

                SuggestionTexts.text = "";
                CurrentSuggestionText.text = "";
                return;
            }

            if (!SuggestionsPanel.enabled)
                SuggestionsPanel.enabled = true;

            _suggestions.Sort();

            if (_suggestions.Count > 0)
            {
                for (int i = 0; i < _suggestions.Count; i++)
                {
                    if (i == MAX_SUGGETIONS_COUNT)
                    {
                        _sb.Append("...");
                        break;
                    }

                    _sb.Append(_suggestions[i]);
                    ConsoleCommand cmd = DevConsole.FindCommand(_suggestions[i]);

                    string trackVal = cmd.TrackedValue?.Invoke();

                    if (cmd.Type == DevConObjType.isBool)
                        trackVal = trackVal.ToLower();

                    _sb.Append($" {trackVal}");

                    if (i != _suggestions.Count - 1)
                        _sb.Append("\n");
                }
            }
            SuggestionTexts.text = _sb.ToString();
            CurrentSuggestionText.text = _suggestions[0];
        }

        public void ExcuteConsoleText()
        {
            if (string.IsNullOrEmpty(TextField.text))
                return;

            TextField.text = Regex.Replace(TextField.text, @"<[^>]+>", string.Empty);

            Debug.Log($"> {TextField.text}");

            if (DevConsole.ExecuteCommandLine(TextField.text, _printMSG))
                _lastInputText.Add(TextField.text);

            TextField.text = "";
        }

        private void PrintConsoleMessage(ConsoleMessageType type, List<string> msgs)
        {
            switch (type)
            {
                case ConsoleMessageType.UnknownCommand:
                    Debug.Log(msgs[0]);
                    break;
                case ConsoleMessageType.FullInformation:
                    string value = string.IsNullOrEmpty(msgs[1]) ? string.Empty : $" = {msgs[1]}";
                    string initValue = string.IsNullOrEmpty(msgs[2]) ? string.Empty : $" <color={COLOR_VALUE}>( init = {msgs[2]} )</color>";
                    string desc = string.IsNullOrEmpty(msgs[3]) ? string.Empty : $", <color={COLOR_INFO}>{msgs[3]}</color>";
                    Debug.Log($"<color=#9FF781>[{msgs[0]}{value}]</color>{initValue}{desc}");
                    break;
                case ConsoleMessageType.CompactInformation:
                    Debug.Log($"<color={COLOR_INFO}>{msgs[0]}</color>");
                    break;
                case ConsoleMessageType.FlagError:
                case ConsoleMessageType.ArgumentCountError:
                case ConsoleMessageType.ParameterError:
                    Debug.Log($"<color={COLOR_ERROR}>{msgs[0]}</color>");
                    break;
                /*
                case ConsoleMessageType.ExecuteError:
                    if ((DevConsole.CurrentFlags & ExecFlag.DEBUG) != 0)
                        Debug.Log($"<color={COLOR_ERROR}>{msgs[0]}</color>");
                    break;
                */
            }
        }

        private void OnDestroy()
        {
            DevConsole.OnCmdAdded -= AddToSuggestionTree;
            DevConsole.OnCmdRemoved -= RemoveToSuggestionTree;
            Application.logMessageReceived -= HandleLog;
            BindCMD.OnExcuteBinding -= UpdateSuggestions;
            BindCMD.SaveBindingCfg();
        }
    }
}