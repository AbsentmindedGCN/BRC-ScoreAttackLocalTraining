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

        private static readonly string ImportGhostPath = Path.Combine(GhostSaveData.Instance.GetSaveLocation(), "imported_ghosts");
        private static readonly string ExportGhostPath = Path.Combine(GhostSaveData.Instance.GetSaveLocation(), "exported_ghosts");

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

        /* public static string ExportPBGhosts() 
        {
            foreach (var ghostPair in GhostSaveData.Instance.BestGhostsByStage)
            {
                foreach (var ghostTimePair in ghostPair.Value.GhostByTimeLimit) 
                {
                    Stage stage = (Stage)ghostPair.Key;
                    float timeLimit = (float)ghostTimePair.Key;
                    string exportLocation = ExportSinglePBGhost(stage, timeLimit);
                }
            }
            return ExportGhostPath;
        }

        public static string ExportPBGhostsForStage(Stage stage) {
            foreach (var ghostTimePair in GhostSaveData.Instance.GetOrCreateGhostData(stage).GhostByTimeLimit)
            {
                string exportLocation = ExportSinglePBGhost(stage, (float)ghostTimePair.Key);
            }
            return ExportGhostPath;
        }

        public static string ExportSinglePBGhost(Stage stage, float timeLimit)
        {
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);
            
            string stageName = Reptile.Core.Instance.Localizer.GetStageName(stage);
            float pbScore = ScoreAttackSaveData.Instance.PersonalBestByStage[stage].GetPersonalBest(timeLimit);

            var filename = stageName + " " + ((int)timeLimit).ToString() + "s (" + ((int)pbScore).ToString() + ")" + ".gsa";
            filename = filename.Trim();

            Ghost ghost = GhostSaveData.Instance.GetOrCreateGhostData(stage).GetGhost(timeLimit);
            GhostSaveData.Instance.WriteExportedGhost(writer, ghost, stage, timeLimit, ghost.Score);
            writer.Flush();

            var data = ms.ToArray();
            CustomStorage.Instance.WriteFile(data, filename);

            writer.Dispose();
            ms.Dispose();

            return Path.Combine(ExportGhostPath, filename);
        }

        public static ExportedGhostData ImportSingleGhost(string filePath) {
            if (!File.Exists(filePath)) return null;

            using var file = File.OpenRead(filePath);
            using var reader = new BinaryReader(file);
            return GhostSaveData.Instance.ReadExportedGhost(reader);
        }

        public static ExportedGhostData[] ImportGhosts() {
            List<ExportedGhostData> importedGhosts = new List<ExportedGhostData>(); 

            foreach (String file in Directory.GetFiles(ImportGhostPath, "*.gsa", SearchOption.AllDirectories))
            {
                ExportedGhostData data = ImportSingleGhost(file);
                if (data != null) importedGhosts.Add(data);
            }
            
            return importedGhosts.ToArray(); 
        } */
    }
}
