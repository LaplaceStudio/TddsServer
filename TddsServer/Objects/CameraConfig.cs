namespace TddsServer.Objects {
    public class CameraConfig {

        public string ChannelId { get; set; } = "0";
        public int ImageWidth { get; set; } = 960;
        public int ImageHeight { get; set; } = 960;
        public CameraStatus Status { get; set; }
        //public int Exposure { get; set; } = 5;
        //public int CaptureFrameRate { get; set; } = 30;
        //public bool UseGlobalConfig { get; set; } = true;
        public PixelFormatType PixelFormat { get; set; }

    }


    public enum CameraStatus {
        Disconnected = 0,
        Connected = 1,
        Running = 2
    }

    public enum PixelFormatType {
        UNKNOWN,
        RGB,
        RGBA,
        BGR,
        BGRA
    }
}
