using System.Diagnostics;

namespace dy.net.utils
{
    /// <summary>
    /// Whisper 语音转文字助手
    /// </summary>
    public class WhisperHelper
    {
        /// <summary>
        /// 将音频转为文字
        /// </summary>
        /// <param name="audioPath">音频文件路径</param>
        /// <param name="outputDir">输出目录（默认与音频同目录）</param>
        /// <param name="model">模型大小：tiny, base, small, medium, large</param>
        /// <param name="language">语言：zh（中文）, en（英文）等</param>
        /// <returns>转录文本文件路径</returns>
        public async Task<string> TranscribeAsync(string audioPath, string outputDir = null, string model = "base", string language = "zh")
        {
            if (!File.Exists(audioPath))
            {
                Serilog.Log.Error($"音频文件不存在: {audioPath}");
                return null;
            }

            outputDir ??= Path.GetDirectoryName(audioPath);
            var baseName = Path.GetFileNameWithoutExtension(audioPath);

            try
            {
                // whisper audio.mp3 --model base --language zh --output_dir ./ --initial_prompt "以下是普通话的句子。"
                // initial_prompt 用于引导输出简体中文
                var arguments = $"\"{audioPath}\" --model {model} --language {language} --output_dir \"{outputDir}\" --output_format txt --initial_prompt \"以下是普通话的句子。\"";
                
                Serilog.Log.Debug($"开始转录: whisper {arguments}");

                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "whisper",
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                
                // 读取输出用于日志
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();
                
                var output = await outputTask;
                var error = await errorTask;

                var txtPath = Path.Combine(outputDir, $"{baseName}.txt");
                
                if (process.ExitCode == 0 && File.Exists(txtPath))
                {
                    Serilog.Log.Debug($"转录成功: {txtPath}");
                    return txtPath;
                }
                else
                {
                    Serilog.Log.Error($"转录失败: {error}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"转录异常: {ex.Message}");
                return null;
            }
        }
    }
}
