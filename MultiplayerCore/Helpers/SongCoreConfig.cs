using System.Reflection;

namespace MultiplayerCore.Helpers
{
    /// <summary>
    /// Helper for reading SongCore config data.
    /// </summary>
    public static class SongCoreConfig
    {
        private static object? _songCoreConfig = null;

        private static object? TryGetInstance()
        {
            if (_songCoreConfig == null)
            {
                _songCoreConfig = typeof(SongCore.Plugin)
                    .GetProperty("Configuration", BindingFlags.NonPublic | BindingFlags.Static)
                    ?.GetValue(null);
            }

            return _songCoreConfig;
        }

        public static object? TryGetValue(string key)
        {
            var configObject = TryGetInstance();
            if (configObject == null)
                return null;
            
            var configProp = configObject.GetType().GetProperty(key);
            if (configProp == null)
                return null;

            return configProp.GetValue(configObject);
        }

        public static object? TrySetValue(string key, object value)
        {
            var configObject = TryGetInstance();
            
            if (configObject == null)
                return null;
            
            var configProp = configObject.GetType().GetProperty(key);
            
            if (configProp == null)
                return null;

            configProp.SetValue(configObject, value);
            return true;
        }

        public static bool TryGetBool(string key)
        {
            var value = TryGetValue(key);
            if (value == null)
                return false;
            
            return (bool) value;
        }

        public static bool TrySetBool(string key)
        {
            var value = TryGetValue(key);
            if (value == null)
                return false;
            
            return (bool) value;
        }

        public static bool CustomSongNoteColors
        {
            get => TryGetBool("CustomSongNoteColors");
            set => TrySetValue("CustomSongNoteColors", value);
        }

        public static bool CustomSongObstacleColors
        {
            get => TryGetBool("CustomSongObstacleColors");
            set => TrySetValue("CustomSongObstacleColors", value);
        }

        public static bool CustomSongEnvironmentColors
        {
            get => TryGetBool("CustomSongEnvironmentColors");
            set => TrySetValue("CustomSongEnvironmentColors", value);
        }

        public static bool AnyCustomSongColors
        {
            get => CustomSongNoteColors || CustomSongObstacleColors || CustomSongEnvironmentColors;
            set
            {
                if (value)
                {
                    CustomSongNoteColors = true;
                    CustomSongObstacleColors = true;
                    CustomSongEnvironmentColors = true;
                }
                else
                {
                    CustomSongNoteColors = false;
                    CustomSongObstacleColors = false;
                    CustomSongEnvironmentColors = false;
                }
            }
        }
    }
}