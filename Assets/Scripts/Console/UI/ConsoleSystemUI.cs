using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;
using SPTr.DeveloperConsole;
using SPTr.CMD;
using SPTr.CustomCollections;


#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SPTr.UI
{
    public class ConsoleSystemUI : MonoBehaviour
    {
        public static ConsoleSystemUI instance;

        [Header("UI")]
        [SerializeField] private UIDocument _uiDocument;

        // ── UIToolkit 요소 참조 ───────────────────────────────
        private VisualElement _window;
        private ScrollView _logScrollView;
        private Label _logTextLabel;
        private VisualElement _suggestionPanel;
        private VisualElement _handleSe;
        private TextField _commandInput;
        private Button _submitButton;
        private Button _closeButton;
        private Label _ghostSuggestion;   // 현재 입력한 prefix 뒤에 이어질 추천 suffix

        // ── 드래그 / 리사이즈 ────────────────────────────────
        private Vector2 _dragClickOffset;
        private Rect _resizeSnapshot;
        private bool _isDragging;
        private bool _isResizing;

        private const float MIN_WIDTH = 300f;
        private const float MIN_HEIGHT = 200f;

        // ── 로그 ─────────────────────────────────────────────
        private readonly StringBuilder _logBuilder = new StringBuilder();
        private bool _isLogDirty = false;
        private const int MAX_LOG_LEN = 15000;

        // ── 자동완성 ──────────────────────────────────────────
        private TrieTree _suggestionsTree = new TrieTree();
        private List<string> _suggestions = new List<string>();
        private const int MAX_SUGGESTIONS = 15;
        private bool _needsUpdate = false;

        // ── 기타 ─────────────────────────────────────────────
        private List<string> _lastInputText = new List<string>();

        private const string COLOR_ERROR = "#F78181";
        private const string COLOR_INFO = "#81C8FF";
        private const string COLOR_WARN = "#F0C080";

        IVisualElementScheduledItem cursorTask;

        // ── 내장 커맨드 ──────────────────────────────────────
        public static ConsoleCommand echo = new ConsoleCommand("echo",
            (string args) => Debug.Log(args), "콘솔에 문자열을 출력합니다.");

        public static ConsoleCommand clear = new ConsoleCommand("clear", () =>
        {
            instance._logBuilder.Clear();
            if (instance._logTextLabel != null)
                instance._logTextLabel.text = string.Empty;
        }, "/콘솔 로그를 전부 지웁니다.");

        public static ConsoleCommand help = new ConsoleCommand("help", (string args) =>
        {
            var split = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (split.Length > 1) { Debug.Log($"<color={COLOR_ERROR}>인자는 하나만 입력해주세요</color>"); return; }
            var cmd = DevConsole.FindCommand(split[0]);
            if (cmd != null)
                Debug.Log(cmd.Description?.Length > 1
                    ? $"<color={COLOR_INFO}>{(cmd.Description[0] != '/' ? cmd.Description : cmd.Description[1..])}</color>"
                    : $"<color={COLOR_ERROR}>명령어의 설명이 존재하지 않습니다.</color>");
            else
                Debug.Log($"<color={COLOR_ERROR}>해당하는 이름의 콘솔 명령어가 존재하지 않습니다.</color>");
        }, "콘솔 명령어의 설명을 출력합니다. \n- help <콘솔 명령어>");

        // ════════════════════════════════════════════════════
        // 생명주기
        // ════════════════════════════════════════════════════

        void Awake()
        {
            if (instance != null) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);

            DevConsole.OnCmdAdded += cmd => _suggestionsTree.Add(cmd.Name);
            DevConsole.OnCmdRemoved += cmd => _suggestionsTree.Remove(cmd.Name);
            Application.logMessageReceived += HandleLog;

            DevConsole.AddAllCommandInAssembly();
            BindCMD.LoadBindingCfg();
            BindCMD.OnExcuteBinding += UpdateSuggestions;
            ExecCMD.ExecuteAutoExec();
        }

        public void BlinkingCursor(TextField tf)
        {
            tf.RegisterCallback<FocusEvent>(evt =>
            {
                RestartBlink(tf);
            });

            tf.RegisterCallback<BlurEvent>(evt =>
            {
                cursorTask?.Pause();
                tf.RemoveFromClassList("transparentCursor");
            });

            // 처음 시작
            RestartBlink(tf);
        }

        void RestartBlink(TextField tf)
        {
            // 기존 스케줄 완전히 중지
            cursorTask?.Pause();

            // 시작 상태 통일 (항상 "보이는 상태"에서 시작)
            tf.RemoveFromClassList("transparentCursor");

            // 새 스케줄 등록
            cursorTask = tf.schedule.Execute(() =>
            {
                tf.EnableInClassList(
                    "transparentCursor",
                    !tf.ClassListContains("transparentCursor")
                );
            }).Every(380).StartingIn(380);
        }

        void SetupGhost()
        {
            var textInput = _commandInput.Q("unity-text-input");

            _ghostSuggestion.style.left = textInput.resolvedStyle.paddingLeft;
            _ghostSuggestion.style.top = 0;
            _ghostSuggestion.style.bottom = 0;
        }

        void OnEnable()
        {
            var root = _uiDocument.rootVisualElement;

            _window = root.Q("window");
            _logScrollView = root.Q<ScrollView>("log-list-area");
            _logTextLabel = root.Q<Label>("log-text-label");
#pragma warning disable 0618
            _logTextLabel.selection.selectionColor = new Color(108 / 255f, 166 / 255f, 198 / 255f, 140 / 255f);
#pragma warning restore 0618
            //_logTextLabel.selection.cursorColor = Color.red;
            //_logTextLabel = new Label();
            //_logTextLabel.AddToClassList("log-text-label");
            //_logTextLabel.enableRichText = true;
            //_logTextLabel.selection.isSelectable = true;
            //_logScrollView.contentContainer.Add(_logTextLabel);
            _suggestionPanel = root.Q("suggestion-panel");
            _suggestionPanel.style.display = DisplayStyle.None;
            _commandInput = root.Q<TextField>("command-input");
            _submitButton = root.Q<Button>("submit-btn");
            _closeButton = root.Q<Button>("close-btn");
            _handleSe = root.Q("handle-se");
            if (_handleSe == null)
                Debug.LogError("handle-se 못 찾음!");

            // 고스트를 #unity-text-input 안에 absolute 오버레이로 배치
            // flex 레이아웃에 참여하지 않으므로 입력란 폭을 깎지 않고,
            // textInput 좌표 기준이므로 left 계산이 정확함


            _ghostSuggestion = root.Q<Label>("ghost-suggestion");
            _ghostSuggestion.pickingMode = PickingMode.Ignore;
            _ghostSuggestion.style.display = DisplayStyle.None;
            _commandInput.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                SetupGhost();
            });

            // 이벤트 등록
            RegisterDrag();
            RegisterResize();
            RegisterInputEvents();

            // 리사이즈/이동 시 패널 위치 재계산
            _window.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                if (_suggestionPanel.style.display == DisplayStyle.Flex)
                    RepositionSuggestionPanel(_suggestions.Count);
            });

            BlinkingCursor(_commandInput);

            _closeButton.clicked += () => SetWindowActive(false);
            _submitButton.clicked += ExecuteConsoleText;

            _window.style.display = DisplayStyle.None;
        }

        void Update()
        {
            bool isActive = _window.resolvedStyle.display != DisplayStyle.None;

#if ENABLE_INPUT_SYSTEM
            if (isActive && Keyboard.current.enterKey.wasPressedThisFrame)
                ExecuteConsoleText();

            if (Keyboard.current.backquoteKey.wasPressedThisFrame)
                SetWindowActive(!isActive);

            // Tab 자동완성은 RegisterInputEvents의 KeyDownEvent 콜백에서 처리합니다.
#else
            if (isActive && Input.GetKeyDown(KeyCode.Return))
                ExecuteConsoleText();

            if (Input.GetKeyDown(KeyCode.BackQuote))
                SetWindowActive(!isActive);

            // Tab 자동완성은 RegisterInputEvents의 KeyDownEvent 콜백에서 처리합니다.
#endif
            if (_needsUpdate)
            {
                _needsUpdate = false;
                UpdateSuggestions();
            }
        }

        void LateUpdate()
        {
            // 인풋에 포커스 없을 때만 키 바인딩 실행
            if (_commandInput.focusController?.focusedElement != _commandInput)
                BindCMD.InvokeBinding();

            // 로그 변경사항 배치 반영 (프레임드랍 방지)
            if (_isLogDirty)
            {
                FlushLog();
                _isLogDirty = false;
            }
        }

        void OnDestroy()
        {
            DevConsole.OnCmdAdded -= cmd => _suggestionsTree.Add(cmd.Name);
            DevConsole.OnCmdRemoved -= cmd => _suggestionsTree.Remove(cmd.Name);
            Application.logMessageReceived -= HandleLog;
            BindCMD.OnExcuteBinding -= UpdateSuggestions;
            BindCMD.SaveBindingCfg();
        }

        // ════════════════════════════════════════════════════
        // 창 활성화
        // ════════════════════════════════════════════════════

        private void SetWindowActive(bool active)
        {
            _window.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ════════════════════════════════════════════════════
        // 드래그 이동 (타이틀바)
        // ════════════════════════════════════════════════════

        private void RegisterDrag()
        {
            var titleBar = _window.Q("title-bar");

            titleBar.RegisterCallback<PointerDownEvent>(e =>
            {
                _dragClickOffset = new Vector2(
                    e.position.x - _window.resolvedStyle.left,
                    e.position.y - _window.resolvedStyle.top
                );
                _isDragging = true;
                titleBar.CapturePointer(e.pointerId);
                _window.BringToFront();
                e.StopPropagation();
            });

            titleBar.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!_isDragging) return;
                var panel = _window.panel.visualTree;
                float panelW = panel.resolvedStyle.width;
                float panelH = panel.resolvedStyle.height;
                float w = _window.resolvedStyle.width;
                float h = _window.resolvedStyle.height;

                _window.style.left = Mathf.Clamp(e.position.x - _dragClickOffset.x, 0, panelW - w);
                _window.style.top = Mathf.Clamp(e.position.y - _dragClickOffset.y, 0, panelH - h);
            });

            titleBar.RegisterCallback<PointerUpEvent>(e =>
            {
                _isDragging = false;
                titleBar.ReleasePointer(e.pointerId);
            });
        }

        // ════════════════════════════════════════════════════
        // 리사이즈 (우측 하단 핸들만)
        // ════════════════════════════════════════════════════

        private void RegisterResize()
        {
            _handleSe.RegisterCallback<PointerDownEvent>(e =>
            {
                _resizeSnapshot = new Rect(
                    _window.resolvedStyle.left, _window.resolvedStyle.top,
                    _window.resolvedStyle.width, _window.resolvedStyle.height
                );
                _isResizing = true;
                _handleSe.CapturePointer(e.pointerId);
                _window.BringToFront();
                e.StopPropagation();
            });

            _handleSe.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!_isResizing) return;
                var panel = _window.panel.visualTree;
                float panelW = panel.resolvedStyle.width;
                float panelH = panel.resolvedStyle.height;
                float x = _resizeSnapshot.x;
                float y = _resizeSnapshot.y;

                float w = Mathf.Clamp(e.position.x - x, MIN_WIDTH, panelW - x);
                float h = Mathf.Clamp(e.position.y - y, MIN_HEIGHT, panelH - y);

                _window.style.width = w;
                _window.style.height = h;
            });

            _handleSe.RegisterCallback<PointerUpEvent>(e =>
            {
                _isResizing = false;
                _handleSe.ReleasePointer(e.pointerId);
            });
        }

        // ════════════════════════════════════════════════════
        // 인풋 이벤트
        // ════════════════════════════════════════════════════

        private void RegisterInputEvents()
        {
            _commandInput.RegisterValueChangedCallback(evt =>
            {
                _needsUpdate = true;
                RestartBlink(_commandInput);
            });

            _commandInput.RegisterCallback<FocusEvent>(_ =>
            {
                _needsUpdate = true;
            });

            _commandInput.RegisterCallback<BlurEvent>(_ =>
            {
                _suggestionPanel.style.display = DisplayStyle.None;
            });

            // Tab 키의 기본 포커스 이동을 막고, 추천 단어를 완성한 뒤 포커스를 유지합니다.
            _commandInput.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Tab)
                {
                    //evt.PreventDefault();
                    evt.StopPropagation();

                    if (_suggestions.Count > 0)
                    {
                        ApplyCurrentSuggestion();
                        // ApplyCurrentSuggestion은 value만 바꾸므로 명시적으로 포커스 복원
                        _commandInput.schedule.Execute(() =>
                        {
                            _commandInput.Focus();
                            int len = _commandInput.value.Length;
                            _commandInput.SelectRange(len, len);
                        }).ExecuteLater(0);
                    }
                }
            }, TrickleDown.TrickleDown); // TrickleDown으로 등록해야 UI Toolkit 내부 처리보다 먼저 실행됨
        }

        // ════════════════════════════════════════════════════
        // 자동완성
        // ════════════════════════════════════════════════════

        private void UpdateSuggestions()
        {
            _suggestions.Clear();
            _suggestionPanel.Clear();
            SetGhostSuggestion(string.Empty);

            string input = _commandInput.value;

            if (string.IsNullOrEmpty(input))
            {
                _suggestionPanel.style.display = DisplayStyle.None;
                SetGhostSuggestion(string.Empty);
                return;
            }

            bool result = _suggestionsTree.TryLoadSuggestions(ref _suggestions, input);

            if (!result)
            {
                _suggestionPanel.style.display = DisplayStyle.None;
                SetGhostSuggestion(string.Empty);
                return;
            }

            _suggestions.Sort();

            if (_suggestions.Count > 0)
                SetGhostSuggestion(_suggestions[0]);

            int count = Mathf.Min(_suggestions.Count, MAX_SUGGESTIONS);

            // 가장 긴 문자열 길이 기준으로 패널 너비 추산
            // (UI Toolkit은 런타임 텍스트 측정이 번거로우므로 문자 수 × 평균픽셀로 근사)
            const float CHAR_WIDTH_APPROX = 9.5f;  // font-size 14px 기준 (여유있게)
            const float ITEM_PADDING = 20f;          // padding-left + padding-right
            const float BORDER = 2f;

            int longestLen = 0;
            for (int i = 0; i < count; i++)
            {
                var cmd = DevConsole.FindCommand(_suggestions[i]);
                string trackVal = cmd?.TrackedValue?.Invoke() ?? "";
                if (cmd?.Type == DevConObjType.isBool) trackVal = trackVal.ToLower();
                string label = $"{_suggestions[i]} {trackVal}".TrimEnd();
                if (label.Length > longestLen) longestLen = label.Length;
            }

            float inputWidth = _commandInput.resolvedStyle.width;
            // resolvedStyle은 첫 프레임에 0일 수 있으므로 레이아웃 후 적용
            _suggestionPanel.schedule.Execute(() =>
            {
                float iw = _commandInput.resolvedStyle.width;
                if (iw <= 0) iw = inputWidth;
                float desiredW = longestLen * CHAR_WIDTH_APPROX + ITEM_PADDING + BORDER;
                float panelW = iw > 0 ? Mathf.Clamp(desiredW, 80f, iw) : Mathf.Max(desiredW, 80f);

                _suggestionPanel.style.width = panelW;

                RepositionSuggestionPanel(count);
            }).ExecuteLater(0);

            for (int i = 0; i < count; i++)
            {
                int idx = i;
                var cmd = DevConsole.FindCommand(_suggestions[i]);

                string trackVal = cmd?.TrackedValue?.Invoke() ?? "";

                if (cmd?.Type == DevConObjType.isBool) trackVal = trackVal.ToLower();

                var item = new Label($"{_suggestions[i]} {trackVal}".TrimEnd());
                item.AddToClassList("suggestion-item");

                item.RegisterCallback<PointerDownEvent>(e =>
                {
                    e.StopPropagation();
                    SelectSuggestion(idx);
                });
                item.RegisterCallback<MouseEnterEvent>(_ =>
                {
                    foreach (var child in _suggestionPanel.Children())
                        child.RemoveFromClassList("suggestion-item--active");
                    item.AddToClassList("suggestion-item--active");
                });
                item.RegisterCallback<MouseLeaveEvent>(_ =>
                {
                    item.RemoveFromClassList("suggestion-item--active");
                });

                _suggestionPanel.Add(item);
            }

            _suggestionPanel.style.display = DisplayStyle.Flex;
        }

        private void SetGhostSuggestion(string fullSuggestion)
        {
            if (_ghostSuggestion == null)
                return;

            string input = _commandInput.value;

            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(fullSuggestion))
            {
                _ghostSuggestion.style.display = DisplayStyle.None;
                return;
            }

            if (!fullSuggestion.StartsWith(input, StringComparison.OrdinalIgnoreCase))
            {
                _ghostSuggestion.style.display = DisplayStyle.None;
                return;
            }

            string prefix = fullSuggestion.Substring(0, input.Length);
            string suffix = fullSuggestion.Substring(input.Length);

            // 🔥 핵심: prefix는 투명 처리
            _ghostSuggestion.text =
                $"<color=#00000000>{prefix}</color><color=#D2D2D261>{suffix}</color>";

            _ghostSuggestion.style.display = DisplayStyle.Flex;

            UpdateGhostSuggestionLayout(); // 위치는 이제 단순화됨
        }

        private void UpdateGhostSuggestionLayout()
        {
            _ghostSuggestion.schedule.Execute(() =>
            {
                var textInput = _commandInput.Q("unity-text-input");
                if (textInput == null)
                    return;

                _ghostSuggestion.style.left = textInput.resolvedStyle.paddingLeft;
                _ghostSuggestion.style.right = textInput.resolvedStyle.paddingRight;

                _ghostSuggestion.style.top = 0;
                _ghostSuggestion.style.bottom = 0;
            }).ExecuteLater(0);
        }

        private void SelectSuggestion(int idx)
        {
            if (idx >= _suggestions.Count) return;
            _commandInput.value = _suggestions[idx] + " ";
            _commandInput.SelectRange(_commandInput.value.Length, _commandInput.value.Length);
            _commandInput.Focus();
            _needsUpdate = true;
        }

        private void RepositionSuggestionPanel(int itemCount)
        {
            var inputArea = _commandInput.parent;
            float windowH = _window.resolvedStyle.height;
            float inputAreaBottom = windowH - inputArea.layout.yMin;
            float panelH = itemCount * 26f + 4f + 2f;  // 아이템 26px × n + padding 4px + 테두리 2px
            const float GAP = 5f;

            // bottom 기준으로 input-area 바로 위에 밀착
            _suggestionPanel.style.bottom = inputAreaBottom - GAP;
            _suggestionPanel.style.top = StyleKeyword.Auto;
            _suggestionPanel.style.left = inputArea.layout.x + _commandInput.layout.x - 1;
        }

        private void ApplyCurrentSuggestion()
        {
            if (_suggestions.Count == 0) return;
            _commandInput.value = _suggestions[0] + " ";
            _commandInput.SelectRange(_commandInput.value.Length, _commandInput.value.Length);
            _needsUpdate = true;
        }

        // ════════════════════════════════════════════════════
        // 로그
        // ════════════════════════════════════════════════════

        public void HandleLog(string logText, string stackTrace, LogType type)
        {
            if (_logBuilder.Length > 0)
                _logBuilder.Append('\n');

            _logBuilder.Append(logText);

            if (_logBuilder.Length > MAX_LOG_LEN)
                _logBuilder.Remove(0, _logBuilder.Length - MAX_LOG_LEN);

            _isLogDirty = true;
        }

        // 로그를 Label 단위로 추가 (RichText + Ctrl+C 드래그 지원)
        private void FlushLog()
        {
            if (_logTextLabel == null)
                return;

            _logTextLabel.text = _logBuilder.ToString();

            _logScrollView.schedule.Execute(() =>
            {
                _logScrollView.scrollOffset = new Vector2(0, float.MaxValue);
            }).ExecuteLater(0);
        }

        // ════════════════════════════════════════════════════
        // 커맨드 실행
        // ════════════════════════════════════════════════════

        public void ExecuteConsoleText()
        {
            string text = _commandInput.value;
            if (string.IsNullOrEmpty(text)) return;

            text = Regex.Replace(text, @"<[^>]+>", string.Empty);
            Debug.Log($"> {text}");

            string[] split = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (split.Length == 0) return;

            var cmd = DevConsole.FindCommand(split[0]);
            if (cmd == null)
            {
                Debug.Log($"unknown command : {text}");
            }
            else
            {
                ProcessCMD(cmd, split[1..]);
                _lastInputText.Add(text);
            }

            _commandInput.value = "";
            UpdateSuggestions();
        }

        public void ProcessCMD(ConsoleCommand cmd, string[] args)
        {
            if (args.Length < 1)
            {
                if (cmd.Type != DevConObjType.isVoid)
                {
                    string desc = (cmd.Description != "" && cmd.Description[0] != '/') ? $", <color={COLOR_INFO}>{cmd.Description}</color>" : string.Empty;
                    string value = cmd.TrackedValue != null ? $" = {cmd.TrackedValue.Invoke()}" : string.Empty;
                    string initValue = (cmd.InitValue != null && cmd.InitValue != cmd.TrackedValue?.Invoke()) ? $" <color=#F5DA81>( init = {cmd.InitValue} )</color>" : string.Empty;

                    if (cmd.Type == DevConObjType.isBool) { value = value.ToLower(); initValue = initValue.ToLower(); }
                    Debug.Log($"<color=#9FF781>[{cmd.Name}{value}]</color>{initValue}{desc}");
                    return;
                }
                else if (cmd.Description != "" && cmd.Description[0] != '/')
                    Debug.Log($"<color={COLOR_INFO}>{cmd.Description}</color>");
                else if (cmd.Flag != ExecFlag.NONE && (cmd.Flag & DevConsole.CurrentFlags) == 0)
                { Debug.Log($"<color={COLOR_ERROR}>{cmd.Flag}플래그가 활성화 되어있지 않습니다.</color>"); return; }

                DevConsole.ExecuteCommand(cmd, string.Empty);
            }
            else if (args.Length > 1 && cmd.Type != DevConObjType.isString && cmd.Type != DevConObjType.isVoid)
                Debug.Log($"<color={COLOR_ERROR}>하나의 인자만 입력해야 합니다.</color>");
            else
            {
                if (cmd.Type == DevConObjType.isVoid && args[0] != "")
                { Debug.Log($"<color={COLOR_ERROR}>{cmd.Name}은 매개변수가 없는 명령어입니다.</color>"); return; }

                string cmdLine = cmd.Type == DevConObjType.isString ? string.Join(" ", args) : args[0];

                if (!DevConsole.ExecuteCommand(cmd, cmdLine)
                    && cmd.Flag != ExecFlag.NONE
                    && (cmd.Flag & DevConsole.CurrentFlags) == 0)
                    Debug.Log($"<color={COLOR_ERROR}>{cmd.Flag}플래그가 활성화 되어있지 않습니다.</color>");
            }
        }
    }
}