namespace TddsServer.General {
    public class Logger {

        private static FileStream LogStream;
        private static StreamWriter Writer;
        private static string DateFormat = "yy-MM-dd HH:mm:ss.fff";


        private static async Task<bool> Init() {
            try {
                if (LogStream == null) {
                    LogStream = new FileStream($"TDDS-{DateTime.Now:yyMMdd-HHmmss}.txt", FileMode.CreateNew);
                }
                if (Writer == null) {
                    Writer = new StreamWriter(LogStream);
                }
                await Log(LogType.Info, "Start logging.");
                return true;
            } catch (Exception exp) {
                Console.WriteLine("Initialize logger failed. " + exp.Message);
                return false;
            }
        }

        /// <summary>
        /// Log something to log file.
        /// </summary>
        /// <param name="type">Type of this log message.</param>
        /// <param name="text">Log message.</param>
        /// <returns>Foramtted log message string.</returns>
        public static async Task<string> Log(LogType type, string text) {
            if (LogStream == null || Writer == null) {
                if (!await Init()) return "";
            }
            string content = $"[{type.ToString().Substring(0, 1).ToUpper()}]{DateTime.Now.ToString(DateFormat)}>>{text}";
            if (type == LogType.Debug) {
                // No neccessary to write debug message into log file.
                Console.WriteLine(content);
            } else {
                Writer?.WriteLine(content + Environment.NewLine);
            }
            Writer?.FlushAsync();
            return content;
        }

        public static async void Finish() {
            try {
                await Log(LogType.Info, "Finish Logging.");
                await Writer.FlushAsync();
                await LogStream.FlushAsync();
            } catch (Exception exp) {
                Console.WriteLine(exp.Message);
            }
        }
    }

    public enum LogType {
        Info,
        Error,
        Debug,
        Warning,
    }
}
