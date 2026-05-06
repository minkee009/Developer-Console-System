# Developer Console System
 
 유니티 개발자용 콘솔 시스템

[![Video Label](http://img.youtube.com/vi/7vg73NF0uWw/0.jpg)](https://www.youtube.com/watch?v=7vg73NF0uWw)

## 개요
유니티 엔진에서 사용할 수 있는 콘솔 시스템입니다

Prefab폴더에 있는 콘솔 시스템 프리펩을 프로젝트 씬에 드래그 앤 드롭으로 쉽게 활성화할 수 있습니다.

~키로 콘솔창을 활성/비활성화시킬 수 있습니다.

또한 사용자는 C# 정적 클래스를 만들어 콘솔 시스템에서 사용할 명령어를 등록시킬 수 있습니다. 

해당하는 예제는 깃허브 프로젝트 및 배포된 유니티패키지 내부에 Demo/Scripts 폴더에서 확인하실 수 있습니다.

## 일반 사용법
```csharp
[ConCmd("cv_test")]
static float TestValue;

[ConCmd("cmd_test")]
static void TestCommand(bool isTrue) {  }
```
다음과 같이 'ConCmd' 어트리뷰트를 기본타입(bool,int,float,string) '정적' 멤버변수와 반환값이 없고 매개변수가 없는 혹은 매개변수가 기본타입 하나인 '정적' 멤버함수에 붙이면 자동으로 콘솔 시스템에 명령어로서 저장됩니다.
단 콘솔 시스템에서 불리게 될 스네이크 케이스 형식의 이름은 중복이 있어선 안됩니다.

ConCmd 어트리뷰트에는 이외에도 다양한 설정법이 있습니다.


1. 설명(Description)
```csharp
[ConCmd("cmd_test", "/설명 감추기")]
static void TestCommand(bool isTrue) {  }
```
콘솔 시스템에는 명령어에 인자를 넣지않고 실행시키면 해당 명령어가 무엇을하는 명령어인지 소개하는 설명이 출력됩니다.
단 '/'기호를 맨 앞에 넣은 설명은 출력되지 않습니다. '/'기호가 붙은 설명이 있는 명령어는 'help' 명령어의 인자로 넣어서 실행하면 비로소 출력됩니다.

2. 실행플래그 (ExecFlag)
```csharp
[ConCmd("cmd_test", "/설명 감추기", ExecFlag.CHEAT)]
static void TestCommand(bool isTrue) {  }
```
콘솔 시스템 내부에는 상태플래그가 존재합니다. 이는 DevConsole.cs에 하드코딩되어 있으며 개발자가 상태를 직접 수정할 수 있습니다.
이 상태플래그를 통해 콘솔 시스템은 명령어에 붙은 실행플래그를 읽고 실행시킬 수 있냐 없냐를 판단합니다.
이런 명령어의 실행플래그는 ConCmd 세번째 인자에서 설정이 가능합니다.

3. 추적값 (TrakingValue)
```csharp
static bool TestBoolean = true;

[ConCmd("cmd_test", "/설명 감추기", ExecFlag.CHEAT, "TestBoolean")]
static void TestCommand(bool isTrue) {  }
```
명령어에 TrackingValue를 지정하면 콘솔시스템의 자동완성과 로그출력에서 명령어가 추적하고 있는 값을 출력시킬 수 있습니다.
이런 명령어의 추적값은 ConCmd 네번째 인자에서 설정이 가능합니다. 
추적하고픈 정적 멤버변수의 이름을 네번째 인자에 작성하면 됩니다.

## 고급 사용법
ConCmd 어트리뷰트를 이용한 명령어 등록은 간편하지만 리플렉션을 통해 메타데이터 참조함수를 캡처하여 등록하는 방식이기에 일반적으로 만든 함수보다 동작이 매우 느릴 수 있습니다.
따라서 실행할 명령어의 성능이 중요한 때에는 다음과 같은 방식으로 리플렉션을 사용하지 않고 직접 명령어를 콘솔 시스템에 등록시킬 수 있습니다.
```csharp
static bool TestBoolean = true;

static ConsoleCommand cmd_test = new ConsoleCommand("cmd_test", (bool value) => TestBoolean = value, "/설명 감추기").SetTrackingValue(() => TestBoolean.ToString());
```

## 내장 명령어
콘솔 시스템에는 기본적으로 사용할 수 있는 명령어가 내장되어 있습니다.

### 1. Echo
 > echo <문자열>
- 원하는 문자열을 출력합니다.

### 2. Help
 > help <명령어>
- 명령어의 설명을 출력합니다.

### 3. Clear
 > clear
- 콘솔로그를 전부 지웁니다.

### 4. Info - Execflags
 > info_execflags
- 콘솔시스템에 사용되는 플래그의 정보를 출력합니다.

### 5. Flag - Cheats
 > flag_cheats <true/false>
- 콘솔시스템의 치트플래그를 활성/비활성화 합니다.

### 6. Bind
 > bind <키> <명령어 + 인자>
- 지정한 키에 명령어를 할당합니다.
- 키에 할당된 명령어를 해제시키려면 bind <키> ""를 입력해야 합니다. ex) bind g ""

### 7. BindToggle
 > bindtoggle <키> <Tracking값이 있는 Boolean 명령어>

 > bindtoggle <키> <명령어> <인자1> <인자2> ... <인자N>
- 지정한 키에 토글이 가능한 명령어를 할당합니다.
- 적용할 인자가 복잡한 형식(띄어쓰기가 있는)인 경우 인자를 큰 따옴표 ""로 묶어 입력해야 합니다. ex) bindtoggle g echo "hello, world!" "test log!!"

### 8. Unbind
 > unbind <키>

 > unbind all
 - 지정한 키의 bind 명령어를 해제합니다.
 - all을 입력시 모든 bind를 해제합니다.

### 9. Exec
 > exec <콘솔 설정파일 이름>
- .cfg의 내용을 전부 적용합니다. 에디터의 경우 프로젝트 최상단 경로에서 /Config, 빌드된 게임의 경우 게임내부 최상단 경로에서 /Config 폴더 내의 .cfg파일을 찾아 적용합니다.

## Console Config (.cfg)
개발자용 콘솔 시스템에는 콘솔 명령어를 순차적으로 적용할 수 있는 설정 파일을 만들 수 있습니다.

예) user.cfg
```
flag_cheats true
bind f echo "hello, world!"
bind r clear
bindtoggle t flag_cheats
```
예시문과 같이 개행으로 명령어를 구분시키며, 위에서부터 아래로 순차 적용시킵니다.

이러한 .cfg 파일은 Exec 명령어의 설명과 같이 에디터와 빌드된 게임 폴더 내부의 최상단 경로에서 /Config 내에 위치시키면 인식됩니다.

또한 유저가 직접 .cfg를 만들지 않더라도 개발자용 콘솔에는 기본적으로 아래와 같은 .cfg를 생성하고 적용시킵니다.

1. binding.cfg
- Bind 명령어를 실행한 경우 해당 실행 내용은 어플리케이션 종료 이후 전부 binding.cfg로 저장됩니다. 이후 [에디터 씬 플레이 / 어플리케이션이 구동]될 때 마다 한번씩 자동으로 적용됩니다.

2. autoexec.cfg
- 에디터든 게임이든 콘솔 시스템 UI가 최소 한번 이상 활성화된 경우 + autoexec.cfg가 없는 경우 자동생성됩니다. 이후 [에디터 씬 플레이 / 어플리케이션이 구동]될 때 마다 한번씩 자동으로 적용됩니다.

구동 시 해당 파일들의 적용 순서는 나열된 순서와 같습니다. ( binding.cfg -> autoexec.cfg )
