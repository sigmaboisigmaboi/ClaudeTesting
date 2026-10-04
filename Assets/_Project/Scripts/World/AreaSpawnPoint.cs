using UnityEngine;

namespace TheDeep.World
{
    // Where the player appears when arriving from another area. The spawn point's position and
    // facing (Y rotation) are used. If no transition is pending, the player keeps its scene position.
    public class AreaSpawnPoint : MonoBehaviour
    {
        [Tooltip("Matched against the AreaExit's Target Spawn Id in the other scene.")]
        [SerializeField] string spawnId = "";

        public string SpawnId => spawnId;

        void Start()
        {
            if (string.IsNullOrEmpty(AreaExit.PendingSpawnId) || AreaExit.PendingSpawnId != spawnId)
                return;

            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                // A CharacterController overrides direct position changes, so switch it off while moving.
                CharacterController controller = player.GetComponent<CharacterController>();
                if (controller != null)
                    controller.enabled = false;

                player.transform.SetPositionAndRotation(transform.position, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));

                if (controller != null)
                    controller.enabled = true;
            }

            AreaExit.ClearPendingSpawn();
        }
    }
}
