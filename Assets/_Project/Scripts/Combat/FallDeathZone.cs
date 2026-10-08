using TheDeep.Player;
using UnityEngine;

namespace TheDeep.Combat
{
    // A lethal area (e.g. the Span's chasm): the player dies the moment they enter it.
    // No fall damage: crossing into the zone is the death. Only the player is affected.
    // Needs a trigger collider on this GameObject.
    [RequireComponent(typeof(Collider))]
    public class FallDeathZone : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            PlayerHealth player = other.GetComponent<PlayerHealth>();
            if (player != null)
                player.Die("fell into the chasm");
        }
    }
}
