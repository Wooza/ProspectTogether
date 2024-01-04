using System;
using System.IO;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Foundation.Extensions
{
    public static class ApiExtensions
    {
        public static string GetWorldId(this ICoreAPI api) => api?.World?.SavegameIdentifier.ToString();

        /// <summary>
        /// These data files are per world 
        /// </summary>
        public static TData LoadOrCreateDataFile<TData>(this ICoreAPI api, string filename) where TData : new()
        {
            var path = GetDataPath(api, filename);
            try
            {
                MigrateOldDataIfExists(filename, api);

                if (File.Exists(path))
                {
                    var content = File.ReadAllText(path);
                    return JsonUtil.FromString<TData>(content);
                }
            }
            catch (Exception e)
            {
                if (File.Exists(path))
                {
                    api.World.Logger.LogRaw(EnumLogType.Error, $"Failed loading file '{path}' with error '{e}'!");
                    throw;
                }
                else
                {
                    api.World.Logger.LogRaw(EnumLogType.Error, $"Failed loading file '{path}' with error '{e}'. Will initialize new one...");
                }
            }

            var newData = new TData();
            SaveDataFile(api, filename, newData);

            return newData;
        }

        /// <summary>
        /// These data files are per world 
        /// </summary>
        public static void SaveDataFile<TData>(this ICoreAPI api, string filename, TData data) where TData : new()
        {
            var path = GetDataPath(api, filename);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var content = JsonUtil.ToString(data);
                File.WriteAllText(path, content);
            }
            catch (Exception e)
            {
                api.World.Logger.LogRaw(EnumLogType.Error, $"Failed saving file '{path}' with error '{e}'!");
            }
        }

        private static string GetDataPath(ICoreAPI api, string filename)
        {
            return Path.Combine(GamePaths.DataPath, "ModData", GetWorldId(api), filename);
        }

        /// <summary>
        /// I used to use Seed instead of SavegameIdentifier, so unfortunately it is necessary to also check the old path and move the old data over.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="api"></param>
        private static void MigrateOldDataIfExists(string filename, ICoreAPI api)
        {
            var oldPath = Path.Combine(GamePaths.DataPath, "ModData", api.World.Seed.ToString(), filename);
            if (!File.Exists(oldPath))
                return;

            var newPath = GetDataPath(api, filename);
            Directory.CreateDirectory(Path.GetDirectoryName(newPath));
            File.Move(oldPath, newPath);
            if (!Directory.EnumerateFileSystemEntries(Path.GetDirectoryName(oldPath)).Any())
                Directory.Delete(Path.GetDirectoryName(oldPath));
        }

        /// <summary>
        /// There also used to be a spelling error in an old argument, so have to migrate that one as well.
        /// </summary>
        /// <param name="oldFilePath"></param>
        /// <param name="newFileName"></param>
        /// <param name="api"></param>
        public static void MigrateOldDataIfExists(string oldFilePath, string newFileName, ICoreAPI api)
        {
            if (!File.Exists(oldFilePath))
                return;

            var newPath = GetDataPath(api, newFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(newPath));
            File.Move(oldFilePath, newPath);
            if (!Directory.EnumerateFileSystemEntries(Path.GetDirectoryName(oldFilePath)).Any())
                Directory.Delete(Path.GetDirectoryName(oldFilePath));
        }
    }
}
