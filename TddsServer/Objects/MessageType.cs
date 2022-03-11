namespace TddsServer.Objects {
    public enum MessageType {
        Error = -1,
        Success = 0,
        UploaderOnline = 1,
        UploaderOffline = 2,
        DownloaderOnline = 3,
        DownloaderOffline = 4,

        #region TDDS and Console Message Type
        StartTDDS = 10,
        StopTDDS = 11,
        OpenChannel = 12,
        CloseChannel = 13,

        #endregion

        #region TDDS Service API Message Type
        ServiceOnline = 100,
        TddsOnline = 101,
        ConsoleOnline = 102,
        NoticeUploaderOnline = 103,
        NoticeDownloaderOnline = 104,
        GetChannelIds = 105,
        GetChannelImageFormat = 106,
        GetAllChannelsImageFormat = 107,
        GetCamerasStatus=108,


        #endregion

        #region Account
        HasAdminLoginInfo = 201,
        SetAdminLoginInfo,
        GetAllAccount,
        RigisterAccount,
        DestoryAccount,
        ResetAccount,
        ModifyAccount,
        UserLogIn,
        UserLogOut,
        #endregion
    }


}
