using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TheDeep.Consequences;
using UnityEditor;

namespace TheDeep.Tests.EditMode
{
    // Guards hand-authored consequence data: every rulebook asset in the project must have
    // rules with unique ids, a real condition, and at least one effect.
    public class RulebookValidationTests
    {
        static List<ConsequenceRulebook> AllRulebooks() =>
            AssetDatabase.FindAssets("t:ConsequenceRulebook")
                .Select(guid => AssetDatabase.LoadAssetAtPath<ConsequenceRulebook>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(book => book != null)
                .ToList();

        [Test]
        public void AtLeastOneRulebookExists()
        {
            Assert.IsNotEmpty(AllRulebooks(), "No ConsequenceRulebook assets found in the project.");
        }

        [Test]
        public void Rules_HaveUniqueIds_ConditionsAndEffects()
        {
            var problems = new List<string>();
            var seenIds = new HashSet<string>();

            foreach (ConsequenceRulebook book in AllRulebooks())
            {
                foreach (ConsequenceRule rule in book.Rules)
                {
                    if (rule == null || string.IsNullOrWhiteSpace(rule.id))
                    {
                        problems.Add($"{book.name}: a rule has no id");
                        continue;
                    }
                    if (!seenIds.Add(rule.id))
                        problems.Add($"{book.name}: duplicate rule id '{rule.id}'");
                    if (rule.condition == null || rule.condition.IsEmpty)
                        problems.Add($"{book.name}/{rule.id}: empty condition (it would fire immediately)");
                    if (!rule.HasEffects)
                        problems.Add($"{book.name}/{rule.id}: no effects");
                }
            }

            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
