namespace TddsServer.General {
    public class Logger {

        private static FileStream LogStream;
        private static StreamWriter Writer;
        private static string DateFormat = "yy-MM-dd HH:mm:ss.fff";


        private static async Task<bool> Init() {
            try {
                if (LogStream == null) {
                    string dir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                    if (!Directory.Exists(dir)) {
                        Directory.CreateDirectory(dir);
                        Console.WriteLine($"Created logs directory:{dir}");
                    }
                    string path = Path.Combine(dir, $"TDDS-{DateTime.Now:yyMMdd-HHmmss}.txt");
                    LogStream = new FileStream(path, FileMode.CreateNew);
                    Console.WriteLine($"Created log file:{path}");
                }
                if (Writer == null) {
                    Writer = new StreamWriter(LogStream);
                }
                
                await Log(LogType.Info, "Start logging.");
                Console.WriteLine("Logger started.");
                return true;
            } catch (Exception exp) {
                Console.WriteLine("Initialize logger failed. " + exp.Message);
                return false;
            }
        }

        /// <summary>
        /// Log something to log file. Log string will not be wrote to console except type is Debug.
        /// </summary>
        /// <param name="type">Type of this log message.</param>
        /// <param name="text">Log message.</param>
        /// <returns>Foramtted log message string.</returns>
        public static async Task<string> GetLog(LogType type, string text) {
            if (LogStream == null || Writer == null) {
                if (!await Init()) return "";
            }
            string content = $"[{type.ToString().Substring(0, 1).ToUpper()}]{DateTime.Now.ToString(DateFormat)}>>{text}";
            if (type == LogType.Debug) {
                // No neccessary to write debug message into log file.
                Console.WriteLine(content);
            } else {
                Writer?.WriteLine(content);
            }
            Writer?.FlushAsync();
            return content;
        }

        /// <summary>
        /// Log something to log file and console.
        /// </summary>
        /// <param name="type">Type of this log message.</param>
        /// <param name="text">Log message.</param>
        /// <returns>Foramtted log message string.</returns>
        public static async Task Log(LogType type, string text) {

            if (LogStream == null || Writer == null) {
                if (!await Init()) {
                    Console.WriteLine("Init logger failed.");
                    return;
                }
            }
            string content = $"[{type.ToString().Substring(0, 1).ToUpper()}]{DateTime.Now.ToString(DateFormat)}>>{text}";
            Console.WriteLine(content);
            Writer?.WriteLine(content);
            Writer?.Flush();
        }

        public static async void Finish() {
            try {
                await Log(LogType.Info, "Finish Logging.");
                Writer.Flush();
                LogStream.Flush();
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
