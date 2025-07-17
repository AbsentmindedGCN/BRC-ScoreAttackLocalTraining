using Reptile;
using UnityEngine;
using CommonAPI;
using ScoreAttackGhostSystem;
using static ScoreAttack.GhostSaveData;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScoreAttack
{
    public static class SaveManager
    {
        private static readonly string ExportPath = Path.Combine(Application.persistentDataPath, "ScoreAttackSave.dat");

        public static string ExportSaveData()
        {
            using var file = File.Create(ExportPath);
            using var writer = new BinaryWriter(file);
            ScoreAttackSaveData.Instance.Write(writer);
            return ExportPath;
        }

        public static string ImportSaveData()
        {
            if (!File.Exists(ExportPath)) return null;

            using var file = File.OpenRead(ExportPath);
            using var reader = new BinaryReader(file);
            ScoreAttackSaveData.Instance.Read(reader);

            Core.Instance.SaveManager.SaveCurrentSaveSlot();
            return ExportPath;
        }
    }
}