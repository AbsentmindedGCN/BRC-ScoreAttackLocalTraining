using Reptile;
using System.IO;
using UnityEngine;

namespace ScoreAttack
{
    public static class SaveManager
    {
        private static readonly string SavePath = Path.Combine(Application.persistentDataPath, "ScoreAttackSave.dat");

        public static string ExportSaveData()
        {
            using var file = File.Create(SavePath);
            using var writer = new BinaryWriter(file);
            ScoreAttackSaveData.Instance.Write(writer);
            return SavePath;
        }

        public static string ImportSaveData()
        {
            if (!File.Exists(SavePath)) return null;

            using var file = File.OpenRead(SavePath);
            using var reader = new BinaryReader(file);
            ScoreAttackSaveData.Instance.Read(reader);

            Core.Instance.SaveManager.SaveCurrentSaveSlot();
            return SavePath;
        }
    }
}
