using SPTr.DeveloperConsole;
using System;

namespace SPTr.Demo
{
    public static class CamControlCMD
    {
        public static float cameraMoveSpeed = 5.0f;
        public static float cameraMoveSharpness = 6.0f;

        public static ConsoleCommand cam_movespd = new ConsoleCommand("cam_movespd", 
            (float value) => cameraMoveSpeed = value, 
            "카메라 이동 속도").SetTrackingValue(() => cameraMoveSpeed.ToString());

        public static ConsoleCommand cam_moveshp = new ConsoleCommand("cam_moveshp", 
            (float value) => cameraMoveSharpness = value, 
            "카메라 정지 모션속도").SetTrackingValue(() => cameraMoveSharpness.ToString());
    }
}
