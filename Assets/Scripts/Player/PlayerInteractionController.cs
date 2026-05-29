using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DeepSea.Core;
using DeepSea.Interaction;

namespace DeepSea.Player
{
    /// <summary>
    /// Senior-level Player Interaction Controller.
    /// Uses 3D trigger colliders to detect nearby IGatherable objects.
    /// Manages the gather timer, blocks player movement during gathering, and broadcasts events.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInteractionController : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [Tooltip("The tag assigned to gatherable trigger colliders.")]
        [SerializeField] private string gatherableTag = "Gatherable";

        // State Events
        public event Action<float, float> OnGatherProgressChanged; // (currentProgress, targetTime)
        public event Action<bool> OnGatherStateChanged; // (isGathering)

        // Dependencies
        private PlayerController playerController;
        private IInputProvider inputProvider;

        // Current Interactive candidates in range
        private readonly List<IGatherable> nearGatherables = new List<IGatherable>();
        private Coroutine gatherCoroutine;

        public bool IsGathering { get; private set; } = false;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            inputProvider = GetComponent<IInputProvider>();
        }

        private void Update()
        {
            if (IsGathering) return;

            // Handle Interaction button down
            if (inputProvider != null && inputProvider.IsInteractionPressed())
            {
                TryGatherNearest();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(gatherableTag) || other.GetComponent<IGatherable>() != null)
            {
                IGatherable gatherable = other.GetComponent<IGatherable>();
                if (gatherable != null && !nearGatherables.Contains(gatherable))
                {
                    nearGatherables.Add(gatherable);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            IGatherable gatherable = other.GetComponent<IGatherable>();
            if (gatherable != null && nearGatherables.Contains(gatherable))
            {
                nearGatherables.Remove(gatherable);
            }
        }

        private void TryGatherNearest()
        {
            if (nearGatherables.Count == 0) return;

            // Clean up destroyed references from list
            nearGatherables.RemoveAll(item => item == null || (item as MonoBehaviour) == null);
            if (nearGatherables.Count == 0) return;

            // Find closest gatherable (though standard distance is similar, this is a robust senior safety net)
            IGatherable nearest = null;
            float minDistance = float.MaxValue;
            Vector3 currentPos = transform.position;

            foreach (var item in nearGatherables)
            {
                MonoBehaviour mono = item as MonoBehaviour;
                if (mono != null)
                {
                    float dist = Vector3.SqrMagnitude(mono.transform.position - currentPos);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearest = item;
                    }
                }
            }

            if (nearest != null)
            {
                gatherCoroutine = StartCoroutine(GatheringRoutine(nearest));
            }
        }

        private IEnumerator GatheringRoutine(IGatherable target)
        {
            IsGathering = true;
            OnGatherStateChanged?.Invoke(true);

            // Block movement during gathering for realism and weight
            playerController.SetInputActive(false);

            float totalTime = target.GetGatherTime();
            float elapsedTime = 0f;

            while (elapsedTime < totalTime)
            {
                // Ensure target wasn't destroyed or player exited range midway
                if (target == null || (target as MonoBehaviour) == null)
                {
                    break;
                }

                elapsedTime += Time.deltaTime;
                OnGatherProgressChanged?.Invoke(elapsedTime, totalTime);
                yield return null;
            }

            // Finalize Gathering
            if (target != null && (target as MonoBehaviour) != null)
            {
                target.Gather(gameObject);
                nearGatherables.Remove(target);
            }

            // Unblock movement
            playerController.SetInputActive(true);
            IsGathering = false;
            OnGatherStateChanged?.Invoke(false);
            OnGatherProgressChanged?.Invoke(0f, totalTime);

            gatherCoroutine = null;
        }

        /// <summary>
        /// Cancels any active gathering process (e.g. if hit by an enemy).
        /// </summary>
        public void CancelGathering()
        {
            if (gatherCoroutine != null)
            {
                StopCoroutine(gatherCoroutine);
                gatherCoroutine = null;

                playerController.SetInputActive(true);
                IsGathering = false;
                OnGatherStateChanged?.Invoke(false);
                OnGatherProgressChanged?.Invoke(0f, 1f);
                Debug.Log("[PlayerInteractionController] Gathering cancelled!");
            }
        }
    }
}
