namespace TddsServer.Objects {
    public class CameraConfig {

        public string ChannelId { get; set; } = "0";
        public bool IsConnected { get; set; } = false;
        public bool IsWorking { get; set; } = false;
        public int ImageWidth { get; set; } = 960;
        public int ImageHeight { get; set; } = 960;
        public int Exposure { get; set; } = 5;
        public int CaptureFrameRate { get; set; } = 30;
        public bool UseGlobalConfig { get; set; } = true;
        public PixelFormatType PixelFormat { get; set; }

    }

    public enum PixelFormatType {
        UNKNOWN,
        RGB,
        RGBA,
        BGR,
        BGRA
    }
}
