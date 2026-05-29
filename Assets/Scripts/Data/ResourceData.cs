using UnityEngine;

namespace DeepSea.Data
{
    [CreateAssetMenu(fileName = "ResourceData", menuName = "DeepSea/ResourceData", order = 2)]
    public class ResourceData : ScriptableObject
    {
        [Header("Identity")]
        public string resourceName = "Common Seaweed";
        
        [Tooltip("The visual sprite of the resource.")]
        public Sprite sprite;

        [Header("Economic & Physical Value")]
        [Tooltip("Gold value earned when selling this resource.")]
        public int goldValue = 10;

        [Tooltip("Weight occupied in the inventory net (망사리).")]
        public float weight = 1.0f;

        [Header("Spawn Settings")]
        [Tooltip("Minimum depth (Z unit) where this resource can spawn.")]
        public float minSpawnDepth = 0f;

        [Tooltip("Maximum depth (Z unit) where this resource can spawn.")]
        public float maxSpawnDepth = 150f;

        [Header("Interaction Settings")]
        [Tooltip("Time in seconds required to gather this resource.")]
        public float gatherTime = 1.0f;
    }
}
