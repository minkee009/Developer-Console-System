using SPTr.DeveloperConsole;
using System;
using UnityEngine;

namespace SPTr.CMD
{
    public static class ExecCMD
    {
        public const string COLOR_ERROR = "#F78181";

        private const string AUTOEXEC_CFG_NAME = "autoexec";

        public static ConsoleCommand exec = new ConsoleCommand("exec",
            (string fileName) =>
            {
                ExecuteCfg(fileName);
            }, "Config 폴더 안의 cfg 파일을 불러와 명령어를 순서대로 실행합니다. \n- exec <파일명>");

        /// <summary>
        /// cfg 파일을 읽어 안의 모든 명령어를 순서대로 실행합니다.
        /// </summary>
        public static void ExecuteCfg(string fileName)
        {
            if (!ConsoleConfig.TryReadConfig(fileName, out string[] lines))
            {
                Debug.Log($"<color={COLOR_ERROR}>[{fileName}.cfg] 파일을 찾을 수 없습니다.</color>");
                return;
            }

            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                    continue;

                int firstSpace = line.IndexOf(' ');
                string cmdName = firstSpace < 0 ? line : line[..firstSpace];
                string arguments = firstSpace < 0 ? string.Empty : line[(firstSpace + 1)..].Trim();

                if (!DevConsole.TryFindCommand(cmdName, out var cmd))
                {
                    Debug.Log($"<color={COLOR_ERROR}>unknown command : {cmdName}</color>");
                    continue;
                }

                DevConsole.ExecuteCommand(cmd, arguments);
            }
        }

        /// <summary>
        /// autoexec.cfg를 찾아 실행합니다.
        /// </summary>
        public static void ExecuteAutoExec()
        {
            if (!ConsoleConfig.ExistsConfig(AUTOEXEC_CFG_NAME))
                ConsoleConfig.TryWriteConfig(AUTOEXEC_CFG_NAME, Array.Empty<string>());

            ExecuteCfg(AUTOEXEC_CFG_NAME);
        }
    }
}