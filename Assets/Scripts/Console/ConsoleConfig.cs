using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace SPTr.DeveloperConsole
{
    /// <summary>
    /// DevConsole에서 사용할 수 있는 설정 파일에 대한 I/O Helper 입니다.
    /// </summary>
    class ConsoleConfig
    {
        public static readonly string CommonConfigPath = /* Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config"); */
            Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName, "Config");

        public const string ConfigExtension = ".cfg";

        // .cfg 읽기
        public static bool TryReadConfig(string fileName, out string[] commandLines)
        {
            commandLines = null;

            if (!TryGetConfigPath(fileName, out string path))
                return false;

            try
            {
                commandLines = File.ReadAllLines(path, Encoding.UTF8);
                return true;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        // .cfg 생성 / 수정
        public static bool TryWriteConfig(string fileName, string[] commandLines)
        {
            if (!TryGetConfigPath(fileName, out string path))
                return false;

            try
            {
                Directory.CreateDirectory(CommonConfigPath); // Config 폴더 없으면 생성
                File.WriteAllLines(path, commandLines, Encoding.UTF8);
                return true;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        // .cfg 파일 확인
        public static bool ExistsConfig(string fileName) => TryGetConfigPath(fileName, out string path) && File.Exists(path);

        public static string ToConfigFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return ConfigExtension;

            return fileName.EndsWith(ConfigExtension, StringComparison.OrdinalIgnoreCase)
                ? fileName
                : fileName + ConfigExtension;
        }

        private static bool TryGetConfigPath(string fileName, out string path)
        {
            path = string.Empty;

            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            // 호출자가 디렉터리 경로를 전달하지 못하게 한다.
            if (Path.IsPathRooted(fileName) ||
                fileName.Contains('/') ||
                fileName.Contains('\\'))
            {
                return false;
            }

            // 확장자는 메서드에서 일괄 처리한다.
            if (fileName.EndsWith(
                ConfigExtension,
                StringComparison.OrdinalIgnoreCase))
            {
                fileName = fileName[
                    ..^ConfigExtension.Length];
            }

            if (string.IsNullOrWhiteSpace(fileName) ||
                fileName is "." or ".." ||
                fileName.IndexOfAny(
                    Path.GetInvalidFileNameChars()) >= 0)
            {
                return false;
            }

            string root = Path.GetFullPath(CommonConfigPath);

            string candidate = Path.GetFullPath(
                Path.Combine(
                    root,
                    fileName + ConfigExtension));

            string normalizedRoot = root.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);

            string expectedPrefix =
                normalizedRoot + Path.DirectorySeparatorChar;

            StringComparison comparison =
                (Application.platform == RuntimePlatform.WindowsPlayer) || (Application.platform == RuntimePlatform.WindowsEditor)
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;

            if (!candidate.StartsWith(
                expectedPrefix,
                comparison))
            {
                return false;
            }

            path = candidate;
            return true;
        }
    }
}