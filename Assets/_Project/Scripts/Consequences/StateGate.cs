using TheDeep.State;
using UnityEngine;

namespace TheDeep.Consequences
{
    // Shows one set of objects when a condition on the world state is met, and another set when
    // it isn't (e.g. a guard standing at their post vs. blocking a doorway). Updates on load and
    // whenever the world state changes. Put it on a parent object, not on the objects it toggles.
    public class StateGate : MonoBehaviour
    {
        [SerializeField] StateCondition condition = new StateCondition();
        [SerializeField] GameObject[] showWhenMet = new GameObject[0];
        [SerializeField] GameObject[] showWhenNotMet = new GameObject[0];

        void OnEnable() => WorldSession.Changed += Refresh;
        void OnDisable() => WorldSession.Changed -= Refresh;
        void Start() => Refresh();

        void Refresh()
        {
            bool met = condition.IsMet(WorldSession.State);
            foreach (GameObject target in showWhenMet)
                if (target != null) target.SetActive(met);
            foreach (GameObject target in showWhenNotMet)
                if (target != null) target.SetActive(!met);
        }
    }
}
