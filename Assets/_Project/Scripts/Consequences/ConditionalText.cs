using System;
using System.Collections.Generic;
using TheDeep.State;
using UnityEngine;

namespace TheDeep.Consequences
{
    // Floating world-space text that reacts to the world state: NPC barks, signs, boards.
    // Shows the FIRST entry whose condition is met (put the most specific entries first and an
    // entry with an empty condition last as the default). With a show distance, the text only
    // appears when the player is close (barks); with 0 it is always shown (signs).
    // Prototype display: Unity's built-in TextMesh, created at runtime. A real UI/dialogue
    // system can replace the display later without changing the authored entries.
    public class ConditionalText : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public StateCondition condition = new StateCondition();
            [TextArea] public string text = "";
        }

        [Tooltip("Optional name shown above the text, e.g. 'Concord Guard'.")]
        [SerializeField] string speaker = "";
        [SerializeField] List<Entry> entries = new List<Entry>();

        [Header("Display")]
        [Tooltip("0 = always visible (signs). Otherwise only visible when the player is this close, in meters (barks).")]
        [SerializeField] float showWithinDistance = 0f;
        [Tooltip("Height of the text above this object's position, in meters.")]
        [SerializeField] float heightOffset = 0f;
        [SerializeField] float characterSize = 0.035f;
        [SerializeField] Color color = Color.white;

        const float ProximityCheckInterval = 0.2f;

        TextMesh textMesh;
        MeshRenderer textRenderer;
        Transform player;
        string currentText;
        bool currentlyVisible;
        bool loggedCurrentText;
        float nextProximityCheck;

        // Exposed for tests and debugging.
        public IReadOnlyList<Entry> Entries => entries;

        // The text of the first entry whose condition is met, or "" if none.
        public static string SelectText(IReadOnlyList<Entry> entries, WorldState state)
        {
            if (entries == null)
                return "";
            foreach (Entry entry in entries)
                if (entry != null && (entry.condition == null || entry.condition.IsMet(state)))
                    return entry.text ?? "";
            return "";
        }

        void Awake()
        {
            var textObject = new GameObject("Text");
            textObject.transform.SetParent(transform, false);
            textObject.transform.localPosition = new Vector3(0f, heightOffset, 0f);
            textMesh = textObject.AddComponent<TextMesh>();
            textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textMesh.fontSize = 48;
            textMesh.characterSize = characterSize;
            textMesh.anchor = TextAnchor.LowerCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = color;
            textRenderer = textObject.GetComponent<MeshRenderer>();
            if (textRenderer == null)
                textRenderer = textObject.AddComponent<MeshRenderer>();
            textRenderer.sharedMaterial = textMesh.font.material;
        }

        void OnEnable()
        {
            WorldSession.Changed += Refresh;
            Refresh(); // the state may have changed while this was hidden (e.g. by a StateGate)
        }

        void OnDisable() => WorldSession.Changed -= Refresh;

        void Start()
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            player = playerObject != null ? playerObject.transform : null;
            Refresh();
        }

        void Refresh()
        {
            string selected = SelectText(entries, WorldSession.State);
            if (selected != currentText)
            {
                currentText = selected;
                loggedCurrentText = false;
                textMesh.text = string.IsNullOrEmpty(speaker) ? selected : $"[{speaker}]\n{selected}";
            }
            UpdateVisibility();
        }

        void Update()
        {
            if (showWithinDistance > 0f && Time.time >= nextProximityCheck)
            {
                nextProximityCheck = Time.time + ProximityCheckInterval;
                UpdateVisibility();
            }
        }

        void LateUpdate()
        {
            // Always face the camera so text is readable from any side.
            Camera viewer = Camera.main;
            if (viewer != null && currentlyVisible)
                textMesh.transform.rotation = Quaternion.LookRotation(textMesh.transform.position - viewer.transform.position);
        }

        void UpdateVisibility()
        {
            bool inRange = showWithinDistance <= 0f ||
                           (player != null && Vector3.Distance(player.position, transform.position) <= showWithinDistance);
            currentlyVisible = inRange && !string.IsNullOrEmpty(currentText);
            textRenderer.enabled = currentlyVisible;

            // Log each line once when it is first seen, so reactions are visible in the Console too.
            if (currentlyVisible && !loggedCurrentText)
            {
                loggedCurrentText = true;
                Debug.Log($"[{(string.IsNullOrEmpty(speaker) ? name : speaker)}] {currentText.Replace("\n", " ")}");
            }
        }
    }
}
