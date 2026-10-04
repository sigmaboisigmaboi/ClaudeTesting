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
            if (other.CompareTag("Player"))
                TravelTo(targetScene, targetSpawnId);
        }

        // Saves the world and moves the player to another area's spawn point. Used by exits and by
        // NPCs that throw the player out. Returns false if a transition is already under way.
        public static bool TravelTo(string scene, string spawnId)
        {
            if (transitionInProgress || string.IsNullOrEmpty(scene))
                return false;

            transitionInProgress = true;
            WorldSession.Save();
            PendingSpawnId = spawnId;
            Debug.Log($"Leaving to {scene} (spawn '{spawnId}'). World state saved.");
            SceneManager.LoadScene(scene);
            return true;
        }
    }
}
