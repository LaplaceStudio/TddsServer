namespace TddsServer.Objects {
    public class ChannelImageFormat {
        public int ChannelId { get; set; }
        public uint ImageWidth { get; set; }
        public uint ImageHeight { get; set; }
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
