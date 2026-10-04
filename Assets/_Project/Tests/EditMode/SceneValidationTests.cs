using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TheDeep.Consequences;
using TheDeep.Destruction;
using TheDeep.NPC;
using TheDeep.State;
using TheDeep.World;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace TheDeep.Tests.EditMode
{
    // Guards hand-authored scene content: opens every scene in the build list and checks
    // persistent ids, destructible setup, area exits/spawn points, consequence runners, and NPCs.
    public class SceneValidationTests
    {
        // One thing read from a scene: which scene, which object, and the value we care about.
        class Found
        {
            public string Scene;
            public string Object;
            public string Value;
            public string Extra;
        }

        // Opens each enabled build scene (additively, closing it again if it wasn't already open)
        // and reads what we need from every T in it before the scene closes.
        static List<Found> Collect<T>(Func<T, Found> read) where T : Component
        {
            var result = new List<Found>();
            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                if (!buildScene.enabled)
                    continue;

                Scene scene = SceneManager.GetSceneByPath(buildScene.path);
                bool openedHere = !scene.isLoaded;
                if (openedHere)
                    scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Additive);

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (T component in root.GetComponentsInChildren<T>(true))
                    {
                        Found found = read(component);
                        found.Scene = scene.name;
                        found.Object = component.name;
                        result.Add(found);
                    }
                }

                if (openedHere)
                    EditorSceneManager.CloseScene(scene, true);
            }
            return result;
        }

        [Test]
        public void PersistentIds_AreFilledIn_AndUniqueAcrossAllBuildScenes()
        {
            List<Found> ids = Collect<PersistentId>(p => new Found { Value = p.Id });

            foreach (Found id in ids)
                Assert.IsFalse(string.IsNullOrWhiteSpace(id.Value), $"Empty PersistentId on '{id.Object}' in scene {id.Scene}");

            List<string> duplicates = ids.GroupBy(id => id.Value).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            Assert.IsEmpty(duplicates, "Duplicate PersistentIds: " + string.Join(", ", duplicates));
        }

        [Test]
        public void Destructibles_HaveCollider_PersistentId_AndAllThreeLooks()
        {
            List<Found> problems = Collect<Destructible>(d =>
            {
                var missing = new List<string>();
                if (d.GetComponent<Collider>() == null) missing.Add("collider on the root");
                if (d.GetComponent<PersistentId>() == null) missing.Add("PersistentId");
                if (d.IntactVisual == null) missing.Add("Intact");
                if (d.FracturedPieces == null) missing.Add("Fractured");
                else if (d.FracturedPieces.GetComponentsInChildren<Rigidbody>(true).Length == 0) missing.Add("pieces with Rigidbodies");
                if (d.RubbleVisual == null) missing.Add("Rubble");
                return new Found { Value = string.Join(", ", missing) };
            }).Where(f => f.Value.Length > 0).ToList();

            Assert.IsEmpty(problems, string.Join("\n", problems.Select(p => $"{p.Scene}/{p.Object} is missing: {p.Value}")));
        }

        [Test]
        public void AreaExits_PointAtBuildScenes_WithAMatchingSpawnPoint()
        {
            List<Found> exits = Collect<AreaExit>(e => new Found { Value = e.TargetScene, Extra = e.TargetSpawnId });
            List<Found> spawns = Collect<AreaSpawnPoint>(s => new Found { Value = s.SpawnId });
            List<string> buildSceneNames = EditorBuildSettings.scenes.Where(s => s.enabled)
                .Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path)).ToList();

            foreach (Found exit in exits)
            {
                Assert.Contains(exit.Value, buildSceneNames, $"AreaExit in {exit.Scene} targets '{exit.Value}', which isn't in the build scene list.");
                bool spawnExists = spawns.Any(s => s.Scene == exit.Value && s.Value == exit.Extra);
                Assert.IsTrue(spawnExists, $"AreaExit in {exit.Scene} uses spawn '{exit.Extra}', but {exit.Value} has no AreaSpawnPoint with that id.");
            }
        }

        [Test]
        public void ConsequenceRunners_HaveARulebook()
        {
            List<Found> missing = Collect<ConsequenceRunner>(r => new Found { Value = r.Rulebook != null ? "" : "missing" })
                .Where(f => f.Value.Length > 0).ToList();

            Assert.IsEmpty(missing, string.Join("\n", missing.Select(m => $"{m.Scene}/{m.Object} has no rulebook assigned")));
        }

        [Test]
        public void NpcAgents_HaveANavMeshAgent_KinematicBody_AndStops()
        {
            List<Found> problems = Collect<NpcAgent>(npc =>
            {
                var missing = new List<string>();
                if (npc.GetComponent<NavMeshAgent>() == null) missing.Add("NavMeshAgent");
                if (npc.GetComponent<Collider>() == null) missing.Add("collider");
                Rigidbody body = npc.GetComponent<Rigidbody>();
                if (body == null) missing.Add("Rigidbody");
                else if (!body.isKinematic) missing.Add("kinematic Rigidbody");
                if (npc.Stops.Any(stop => stop == null)) missing.Add("an empty stop slot");
                return new Found { Value = string.Join(", ", missing) };
            }).Where(f => f.Value.Length > 0).ToList();

            Assert.IsEmpty(problems, string.Join("\n", problems.Select(p => $"{p.Scene}/{p.Object} is missing: {p.Value}")));
        }

        [Test]
        public void NpcEjects_PointAtBuildScenes_WithAMatchingSpawnPoint()
        {
            List<Found> ejects = Collect<NpcAgent>(npc => new Found { Value = npc.EjectScene, Extra = npc.EjectSpawnId })
                .Where(f => !string.IsNullOrEmpty(f.Value)).ToList();
            List<Found> spawns = Collect<AreaSpawnPoint>(s => new Found { Value = s.SpawnId });

            foreach (Found eject in ejects)
            {
                bool spawnExists = spawns.Any(s => s.Scene == eject.Value && s.Value == eject.Extra);
                Assert.IsTrue(spawnExists, $"{eject.Scene}/{eject.Object} ejects to {eject.Value}/'{eject.Extra}', which doesn't exist in the build scenes.");
            }
        }

        // Fails until the NavMesh has been baked in the Unity Editor (select NavMesh > Bake).
        [Test]
        public void ScenesWithNpcs_HaveABakedNavMesh()
        {
            List<string> scenesWithNpcs = Collect<NpcAgent>(npc => new Found()).Select(f => f.Scene).Distinct().ToList();
            List<Found> surfaces = Collect<NavMeshSurface>(surface => new Found { Value = surface.navMeshData != null ? "baked" : "" });

            foreach (string scene in scenesWithNpcs)
            {
                Assert.IsTrue(surfaces.Any(s => s.Scene == scene), $"{scene} has NPCs but no NavMeshSurface.");
                Assert.IsTrue(surfaces.Any(s => s.Scene == scene && s.Value == "baked"),
                    $"{scene}'s NavMesh hasn't been baked yet: open the scene, select the NavMesh object, and press Bake.");
            }
        }
    }
}
