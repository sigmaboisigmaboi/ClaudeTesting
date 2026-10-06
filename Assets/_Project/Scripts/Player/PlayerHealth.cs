using TheDeep.Combat;
using TheDeep.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TheDeep.Player
{
    // The player's hit points (P6). Taking damage flashes the screen red; at 0 HP (or on entering
    // a FallDeathZone) the player dies: controls stop, "YOU DIED" is shown, and a click reloads the
    // current area with full health. The world is NOT rolled back: what was destroyed stays destroyed.
    // Prototype display only, drawn with Unity's built-in OnGUI (no Canvas or UI package).
    [RequireComponent(typeof(CharacterController))]
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] float maxHealth = 100f;
        [Tooltip("Seconds after dying before a click retries.")]
        [SerializeField] float retryDelay = 1f;

        const float FlashDuration = 0.4f;

        Health health;
        FirstPersonController movement;
        InputAction attackAction;
        float flashUntil = -1f;
        float diedAt = -1f;
        GUIStyle hpStyle;
        GUIStyle bigStyle;
        GUIStyle smallStyle;

        public bool IsDead => health != null && health.IsDead;
        public float CurrentHealth => health != null ? health.Current : 0f;
        public float MaxHealth => health != null ? health.Max : maxHealth;

        void Awake()
        {
            health = new Health(maxHealth);
            movement = GetComponent<FirstPersonController>();
            attackAction = InputSystem.actions.FindAction("Player/Attack", throwIfNotFound: true);
        }

        // A hit from an enemy: damage, a red flash, and a fixed shove away from the attacker.
        public void TakeHit(float damage, Vector3 attackerPosition, float knockbackDistance, float knockbackDuration)
        {
            if (IsDead)
                return;

            float taken = health.TakeDamage(damage);
            if (taken > 0f)
            {
                flashUntil = Time.time + FlashDuration;
                Debug.Log($"Player hit for {taken:0} damage ({health.Current:0}/{health.Max:0} HP left).");
            }

            if (health.IsDead)
            {
                Die("was beaten down");
                return;
            }

            if (movement != null && knockbackDistance > 0f)
                movement.AddKnockback(CombatRules.KnockbackDisplacement(attackerPosition, transform.position, knockbackDistance, -transform.forward), knockbackDuration);
        }

        // Kills the player at once (0 HP or a lethal fall) and stops their controls.
        public void Die(string cause)
        {
            if (diedAt >= 0f)
                return;
            health.Kill();
            diedAt = Time.time;
            flashUntil = -1f;
            Debug.Log($"Player {cause}. YOU DIED.");

            // Switching these off also drops anything held (PlayerGrabThrow) and frees the cursor.
            SetEnabled<FirstPersonController>(false);
            SetEnabled<PlayerGrabThrow>(false);
            SetEnabled<PlayerPushRigidbodies>(false);
            SetEnabled<PlayerMelee>(false);
        }

        void SetEnabled<T>(bool on) where T : Behaviour
        {
            T component = GetComponent<T>();
            if (component != null)
                component.enabled = on;
        }

        void Update()
        {
            if (diedAt >= 0f && Time.time >= diedAt + retryDelay && attackAction.WasPressedThisFrame())
                Retry();
        }

        // Reloads the current area: full health, enemies back at their posts, world state unchanged.
        void Retry()
        {
            Debug.Log("Retrying: reloading the area.");
            AreaExit.TravelTo(SceneManager.GetActiveScene().name, "");
        }

        void OnGUI()
        {
            if (hpStyle == null)
            {
                hpStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
                bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
            }

            var screen = new Rect(0f, 0f, Screen.width, Screen.height);
            Color oldColor = GUI.color;

            if (IsDead)
            {
                GUI.color = new Color(0.35f, 0f, 0f, 0.75f);
                GUI.DrawTexture(screen, Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(0f, Screen.height * 0.5f - 60f, Screen.width, 80f), "YOU DIED", bigStyle);
                if (Time.time >= diedAt + retryDelay)
                    GUI.Label(new Rect(0f, Screen.height * 0.5f + 20f, Screen.width, 40f), "Click to try again", smallStyle);
                GUI.color = oldColor;
                return;
            }

            if (Time.time < flashUntil)
            {
                GUI.color = new Color(1f, 0f, 0f, 0.35f * (flashUntil - Time.time) / FlashDuration);
                GUI.DrawTexture(screen, Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
            GUI.Label(new Rect(20f, Screen.height - 50f, 300f, 40f), $"HP {Mathf.CeilToInt(health.Current)} / {health.Max:0}", hpStyle);
            GUI.color = oldColor;
        }
    }
}
