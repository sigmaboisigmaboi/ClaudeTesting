using System.Collections.Generic;
using UnityEngine;

namespace TheDeep.Consequences
{
    // A data asset holding authored consequence rules, editable in the Inspector.
    // Create one with Assets > Create > The Deep > Consequence Rulebook.
    [CreateAssetMenu(fileName = "Rulebook", menuName = "The Deep/Consequence Rulebook")]
    public class ConsequenceRulebook : ScriptableObject
    {
        [SerializeField] List<ConsequenceRule> rules = new List<ConsequenceRule>();

        public IReadOnlyList<ConsequenceRule> Rules => rules;
    }
}
