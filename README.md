# Developer Console System
 
 유니티 개발자용 콘솔 시스템

[![Video Label](http://img.youtube.com/vi/CdtEuDo4UVM/0.jpg)](https://www.youtube.com/watch?v=CdtEuDo4UVM)

## 개요
유니티 엔진에서 사용할 수 있는 콘솔 시스템입니다

Prefab폴더에 있는 콘솔 시스템 프리펩을 프로젝트 씬에 드래그 앤 드롭으로 쉽게 활성화할 수 있습니다.

~키로 콘솔창을 활성/비활성화시킬 수 있습니다.

또한 사용자는 C# 정적 클래스를 만들어 콘솔 시스템에서 사용할 명령어를 등록시킬 수 있습니다. 

해당하는 예제는 깃허브 프로젝트 및 배포된 유니티패키지 내부에 Demo/Scripts 폴더에서 확인하실 수 있습니다.

## 일반 사용법
```
[ConCmd("cv_test")]
static float TestValue;

[ConCmd("cmd_test")]
static void TestCommand(bool isTrue) {  }
```
다음과 같이 'ConCmd' 어트리뷰트를 기본타입(bool,int,float,string) '정적' 멤버변수와 반환값이 없고 매개변수가 없는 혹은 매개변수가 기본타입 하나인 '정적' 멤버함수에 붙이면 자동으로 콘솔 시스템에 명령어로서 저장됩니다.
단 콘솔 시스템에서 불리게 될 스네이크 케이스 형식의 이름은 중복이 있어선 안됩니다.

ConCmd 어트리뷰트에는 이외에도 다양한 설정법이 있습니다.


1. 설명(Description)
```
[ConCmd("cmd_test", "/설명 감추기")]
static void TestCommand(bool isTrue) {  }
```
콘솔 시스템에는 명령어에 인자를 넣지않고 실행시키면 해당 명령어가 무엇을하는 명령어인지 소개하는 설명이 출력됩니다.
단 '/'기호를 맨 앞에 넣은 설명은 출력되지 않습니다. '/'기호가 붙은 설명이 있는 명령어는 'help' 명령어의 인자로 넣어서 실행하면 비로소 출력됩니다.

2. 실행플래그 (ExecFlag)
```
[ConCmd("cmd_test", "/설명 감추기", ExecFlag.CHEAT)]
static void TestCommand(bool isTrue) {  }
```
콘솔 시스템 내부에는 상태플래그가 존재합니다. 이는 DevConsole.cs에 하드코딩되어 있으며 개발자가 상태를 직접 수정할 수 있습니다.
이 상태플래그를 통해 콘솔 시스템은 명령어에 붙은 실행플래그를 읽고 실행시킬 수 있냐 없냐를 판단합니다.
이런 명령어의 실행플래그는 ConCmd 세번째 인자에서 설정이 가능합니다.

3. 추적값 (TrakingValue)
```
static bool TestBoolean = true;

[ConCmd("cmd_test", "/설명 감추기", ExecFlag.CHEAT, "TestBoolean")]
static void TestCommand(bool isTrue) {  }
```
명령어에 TrackingValue를 지정하면 콘솔시스템의 자동완성과 로그출력에서 명령어가 추적하고 있는 값을 출력시킬 수 있습니다.
이런 명령어의 추적값은 ConCmd 네번째 인자에서 설정이 가능합니다. 
추적하고픈 정적 멤버변수의 이름을 네번째 인자에 작성하면 됩니다.

## 고급 사용법
ConCmd 어트리뷰트를 이용한 명령어 등록은 간편하지만 리플렉션을 통해 메타데이터 참조 함수를 자동생성하여 등록하는 방식이기에 실제 함수나 변수만큼 빠르게 동작하지 않습니다.
성능이 중요한 때에는 다음과 같은 방식으로 리플렉션을 사용하지 않고 직접 명령어를 콘솔 시스템에 등록시킬 수 있습니다.
```
static bool TestBoolean = true;

static ConsoleCommand cmd_test = new ConsoleCommand("cmd_test", (bool value) => TestBoolean = value, "/설명 감추기").SetTrackingValue(() => TestBoolean.ToString());
```
