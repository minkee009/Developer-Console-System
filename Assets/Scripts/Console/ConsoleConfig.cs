using System;
using System.IO;
using System.Text;

namespace SPTr.DeveloperConsole
{
    /// <summary>
    /// DevConsole에서 사용할 수 있는 설정 파일에 대한 I/O Helper 입니다.
    /// </summary>
    class ConsoleConfig
    {
        public static readonly string CommonConfigPath = /* Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config"); */
#if UNITY_EDITOR  // Unity 엔진에 맞게 수정함
            Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName, "Config");
#else
            Path.Combine(Application.persistentDataPath, "Config");
#endif

        public const string ConfigExtension = ".cfg";

        // .cfg 읽기
        public static bool TryReadConfig(string fileName, out string[] commandLines)
        {
            commandLines = null;
            string path = Path.Combine(CommonConfigPath, fileName + ConfigExtension);

            if (!File.Exists(path))
                return false;

            try
            {
                commandLines = File.ReadAllLines(path, Encoding.UTF8);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        // .cfg 생성 / 수정
        public static bool TryWriteConfig(string fileName, string[] commandLines)
        {
            string path = Path.Combine(CommonConfigPath, fileName + ConfigExtension);
            try
            {
                Directory.CreateDirectory(CommonConfigPath); // Config 폴더 없으면 생성
                File.WriteAllLines(path, commandLines, Encoding.UTF8);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        // .cfg 파일 확인
        public static bool ExistsConfig(string fileName) => File.Exists(Path.Combine(CommonConfigPath, fileName + ConfigExtension));
    }
}