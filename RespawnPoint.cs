using Reptile;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace ScoreAttack
{
    public class RespawnPoint
    {
        public Vector3 Position = Vector3.zero;
        public Quaternion Rotation = Quaternion.identity;
        public bool Gear = false;

        public RespawnPoint(Vector3 position, Quaternion rotation, bool gear)
        {
            Position = position;
            Rotation = rotation;
            Gear = gear;
        }

        /*
        public void ApplyToPlayer(Player player)
        {
            WorldHandler.instance.PlacePlayerAt(player, Position, Rotation);
            player.SwitchToEquippedMovestyle(Gear);
        }
        */

        /*
        public void ApplyToPlayer(Player player)
        {
            // Uses coroutine to ensure this happens outside of the physics step, to fix "walk in place" visual bug
            player.StartCoroutine(RespawnRoutine(player));
        }

        private IEnumerator RespawnRoutine(Player player)
        {
            yield return new WaitForEndOfFrame(); // Wait for the end of the current frame
            player.CompletelyStop();
            WorldHandler.instance.PlaceCurrentPlayerAt(Position, Rotation, true);
            //player.SwitchToEquippedMovestyle(Gear);
            if (Gear)
            {
                player.SwitchToEquippedMovestyle(true);
            }
            else
            {
                player.SwitchToEquippedMovestyle(false);
            }
            player.SetRotation(Rotation);

            yield return null;
        }
        */

        public void ApplyToPlayer(Player player)
        {
            // In BRC, ghosts are AI players. We only want to run this for the human.
            if (player == null || player.isAI)
            {
                return;
            }

            player.StartCoroutine(RespawnRoutine(player));
        }

        private IEnumerator RespawnRoutine(Player player)
        {
            yield return new WaitForEndOfFrame();

            // Final safety check to ensure the player wasn't destroyed mid-frame
            if (player == null) yield break;

            player.CompletelyStop();

            // Teleport the actual player
            WorldHandler.instance.PlaceCurrentPlayerAt(Position, Rotation, true);

            if (Gear)
            {
                player.SwitchToEquippedMovestyle(true);
            }
            else
            {
                player.SwitchToEquippedMovestyle(false);
            }

            player.SetRotation(Rotation);

            yield return null;
        }
    }
}
