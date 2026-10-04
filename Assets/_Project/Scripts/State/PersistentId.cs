using UnityEngine;

namespace TheDeep.State
{
    // A stable, unique name for an object whose state is saved (e.g. "p2span.pillar_left").
    // Assigned by hand in the scene; it must never change once used, or old saves won't find it.
    // An EditMode test checks that every id in the build scenes is filled in and unique.
    [DisallowMultipleComponent]
    public class PersistentId : MonoBehaviour
    {
        [Tooltip("Unique across all scenes. Convention: <area>.<object>, e.g. p2span.wall_alcove")]
        [SerializeField] string id;

        public string Id => id;
    }
}
