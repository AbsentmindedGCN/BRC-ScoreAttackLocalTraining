using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommonAPI;
using CommonAPI.Phone;
using Reptile;
using UnityEngine;
namespace ScoreAttackGhostSystem
{
    public class CrewBoomSupport
    {
        public static Guid GetGUID(int character) {
            var characterDatabase = GetTypeByName("CrewBoomAPI.CrewBoomAPIDatabase");
            Dictionary<int, Guid> guids = characterDatabase.GetField("_userCharacters", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as Dictionary<int, Guid>;
            return guids[character];
        }

        public static int GetCharacter(Guid guid) {
            var characterDatabase = GetTypeByName("CrewBoomAPI.CrewBoomAPIDatabase");
            Dictionary<int, Guid> guids = characterDatabase.GetField("_userCharacters", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as Dictionary<int, Guid>;
            Dictionary<Guid, int> characters = guids.ToDictionary(x => x.Value, x => x.Key);
            return characters[guid];
        }

        public static Type GetTypeByName(string name) // from BombRushMP.Common.ReflectionUtility
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Reverse())
            {
                var tt = assembly.GetType(name);
                if (tt != null)
                {
                    return tt;
                }
            }
            return null;
        }
    }
}