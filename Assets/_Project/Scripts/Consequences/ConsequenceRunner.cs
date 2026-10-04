using TheDeep.State;
using UnityEngine;

namespace TheDeep.Consequences
{
    // Put one in each area scene that uses consequences. On load it registers the rulebook with
    // the shared WorldSession and runs the rules once (the "world tick" on area transition),
    // before other scripts' Start, so reactors see the up-to-date state.
    [DefaultExecutionOrder(-100)]
    public class ConsequenceRunner : MonoBehaviour
    {
        [SerializeField] ConsequenceRulebook rulebook;

        // Exposed for the scene-validation tests.
        public ConsequenceRulebook Rulebook => rulebook;

        void Awake()
        {
            WorldSession.UseRules(rulebook);
            WorldSession.RunWorldTick();
        }
    }
}
