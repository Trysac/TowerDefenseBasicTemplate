using SaucerSiege.Core;
using UnityEngine;

namespace SaucerSiege.Game
{
    /// <summary>Keeps each level's record in PlayerPrefs (the registry on Windows, IndexedDB on the web).</summary>
    public sealed class PlayerPrefsRecordStore : IRecordStore
    {
        public const string Prefix = "SaucerSiege.Level";

        public LevelRecord Load(int level) => new LevelRecord(
            PlayerPrefs.GetInt($"{Prefix}{level}.Best", 0),
            PlayerPrefs.GetInt($"{Prefix}{level}.Stars", 0),
            PlayerPrefs.GetInt($"{Prefix}{level}.Cleared", 0) == 1);

        public void Save(int level, LevelRecord record)
        {
            PlayerPrefs.SetInt($"{Prefix}{level}.Best", record.BestScore);
            PlayerPrefs.SetInt($"{Prefix}{level}.Stars", record.Stars);
            PlayerPrefs.SetInt($"{Prefix}{level}.Cleared", record.Cleared ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
