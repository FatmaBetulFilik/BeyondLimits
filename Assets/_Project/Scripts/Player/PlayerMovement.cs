using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BeyondLimits.Player
{
    /// <summary>
    /// Prototype movement for the networked player.
    /// Movement is owner-authoritative (see ADR 0002): the owner moves its own
    /// player and NetworkTransform syncs the position to everyone else.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : NetworkBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;

        private CharacterController controller;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            // Only the owner reads input and moves this player.
            // Other copies are moved by NetworkTransform.
            if (!IsOwner) return;

            Vector3 input = ReadMoveInput();
            controller.SimpleMove(input * moveSpeed);
        }

        // Reads WASD directly for the prototype.
        // Later: move to an Input Actions asset.
        private static Vector3 ReadMoveInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return Vector3.zero;

            Vector3 input = Vector3.zero;
            if (keyboard.wKey.isPressed) input.z += 1f;
            if (keyboard.sKey.isPressed) input.z -= 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            return input.normalized;
        }
    }
}
