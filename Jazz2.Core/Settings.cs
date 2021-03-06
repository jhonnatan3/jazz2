﻿namespace Jazz2
{
    public class Settings
    {
        public enum ResizeMode
        {
            None,
            HQ2x,
            xBRZ3,
            xBRZ4,
            CRT
        }

#if __ANDROID__
        public static ResizeMode Resize = ResizeMode.HQ2x;
        public const float MusicVolume = 0.7f;
        public const float SfxVolume = 0.3f;
#else
        public static ResizeMode Resize = ResizeMode.xBRZ3;
        public const float MusicVolume = 0.5f;
        public const float SfxVolume = 0.36f;
#endif
    }
}