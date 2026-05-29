using UnityEngine;

namespace DeepSea.Data
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "DeepSea/EnemyData", order = 3)]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string enemyName = "Deep Sea Anglerfish";
        
        [Tooltip("The visual sprite of the enemy.")]
        public Sprite sprite;

        [Header("Stats")]
        [Tooltip("Horizontal tracking speed.")]
        public float moveSpeed = 4.0f;

        [Tooltip("Vertical (Z) transition speed when changing layers to chase player.")]
        public float depthTransitionSpeed = 2.0f;

        [Tooltip("Damage dealt to player's oxygen upon collision.")]
        public float attackDamage = 15.0f;

        [Tooltip("Radius within which the enemy detects the player.")]
        public float detectionRadius = 10.0f;

        [Header("Spawn Settings")]
        [Tooltip("Minimum depth (Z unit) where this enemy can spawn.")]
        public float minSpawnDepth = 30f;

        [Tooltip("Maximum depth (Z unit) where this enemy can spawn.")]
        public float maxSpawnDepth = 200f;
    }
}
