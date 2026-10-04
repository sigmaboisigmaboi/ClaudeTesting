using TheDeep.State;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheDeep.World
{
    // A doorway to another area (scene). When the player walks into this trigger, the world state
    // is saved and the target scene is loaded; the player appears at the matching AreaSpawnPoint.
    // Needs a trigger collider on this GameObject. The target scene must be in the build scene list.
    public class AreaExit : MonoBehaviour
    {
        [Tooltip("Scene to load, by name (must be in File > Build Profiles > Scene List).")]
        [SerializeField] string targetScene = "";
        [Tooltip("Spawn point id in the target scene where the player should appear.")]
        [SerializeField] string targetSpawnId = "";

        // Which spawn point the next loaded scene should use. Empty = the player's own scene position.
        public static string PendingSpawnId { get; private set; } = "";

        static bool transitionInProgress;

        // Exposed for the scene-validation tests.
        public string TargetScene => targetScene;
        public string TargetSpawnId => targetSpawnId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetForNewPlaySession()
        {
            PendingSpawnId = "";
            transitionInProgress = false;
        }

        // Called by AreaSpawnPoint once the player has been placed.
        public static void ClearPendingSpawn()
        {
            PendingSpawnId = "";
            transitionInProgress = false;
        }

        // A freshly loaded scene can always be left again, even if no spawn point matched.
        void Start() => transitionInProgress = false;

        void OnTriggerEnter(Collider other)
        {
            if (transitionInProgress || !other.CompareTag("Player"))
                return;

            transitionInProgress = true;
            WorldSession.Save();
            PendingSpawnId = targetSpawnId;
            Debug.Log($"Leaving to {targetScene} (spawn '{targetSpawnId}'). World state saved.");
            SceneManager.LoadScene(targetScene);
        }
    }
}
